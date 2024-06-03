using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Contracts;
using Nethereum.Util;
using Microsoft.Extensions.Configuration;
using static Arbitrum.DataEntities.NetworkUtils;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using System.Security.Principal;
using Arbitrum.AssetBridgerModule;
using Nethereum.HdWallet;
using Arbitrum.DataEntities;
using Arbitrum.Message;
using static Arbitrum.Utils.Lib;
using Nethereum.Hex.HexTypes;
using SharedSettings;
using Nethereum.Model;
using Nethereum.RPC.Accounts;
using static Arbitrum.Message.L1ToL2MessageUtils;

public class Program
{

    public static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];

        // Set up L1 / L2 wallets connected to providers
        var walletPrivateKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1RpcUrl = configuration["DevelopmentSettings:L1RPC"];
        var l2RpcUrl = configuration["DevelopmentSettings:L2RPC"];

        var account1 = new Account(walletPrivateKey);
        var account2 = new Account(walletPrivateKey);

        var l1Web3 = new Web3(account1, l1RpcUrl);
        var l2Web3 = new Web3(account2, l2RpcUrl);

        var l1Signer = new SignerOrProvider(account1, l1Web3);
        var l2Signer = new SignerOrProvider(account2, l2Web3);

        await Console.Out.WriteLineAsync("Cross-chain Greeter");

        // Add default local network configuration
        AddDefaultLocalNetwork();

        // Get L2 network and EthBridger instance
        var l2Network = await GetL2Network(l2Web3);
        var ethBridger = new EthBridger(l2Network);
        var inboxAddress = ethBridger.L2Network.EthBridge.Inbox;

        // Deploy L1 Greeter contract
        var l1GreeterAddress = await DeployContract(l1Signer, l1Web3, "GreeterL1", "Hello world in L1", inboxAddress);
        Console.WriteLine($"L1 Greeter deployed to {l1GreeterAddress}");

        // Deploy L2 Greeter contract
        var l2GreeterAddress = await DeployContract(l2Signer, l2Web3, "GreeterL2", "Hello world in L2");
        Console.WriteLine($"L2 Greeter deployed to {l2GreeterAddress}");

        // Update L1 Greeter with L2 Greeter address
        var l1Greeter = l1Web3.Eth.GetContract(LoadAbi("GreeterL1"), l1GreeterAddress);
        var updateL1Tx = await l1Greeter.GetFunction("updateL2Target").SendTransactionAsync(account.Address, l2GreeterAddress);
        await WaitForTransaction(l1Web3, updateL1Tx);

        // Update L2 Greeter with L1 Greeter address
        var l2Greeter = l2Web3.Eth.GetContract(LoadAbi("GreeterL2"), l2GreeterAddress);
        var updateL2Tx = await l2Greeter.GetFunction("updateL1Target").SendTransactionAsync(account.Address, l1GreeterAddress);
        await WaitForTransaction(l2Web3, updateL2Tx);

        Console.WriteLine("Counterpart contract addresses set in both greeters");

        // Get current L2 greeting
        var currentL2Greeting = await l2Greeter.GetFunction("greet").CallAsync<string>();
        Console.WriteLine($"Current L2 greeting: \"{currentL2Greeting}\"");

        // Update greeting from L1 to L2
        var newGreeting = "Greeting from far, far away";
        var l1ToL2MessageGasEstimator = new L1ToL2MessageGasEstimator(l2Web3);

        // Calculate calldata for setGreeting function
        var iface = new Nethereum.ABI.FunctionEncoding.FunctionCallEncoder();
        var calldata = iface.EncodeRequest("setGreeting", new object[] { newGreeting });

        // Estimate gas parameters
        var gasParams = await l1ToL2MessageGasEstimator.EstimateAll(
            new L1ToL2MessageGasEstimateParams
            {
                From = l1GreeterAddress,
                To = l2GreeterAddress,
                L2CallValue = 0,
                ExcessFeeRefundAddress = account.Address,
                CallValueRefundAddress = account.Address,
                Data = calldata
            },
            await GetBaseFee(l1Web3),
            l1Web3
        );

        var gasPriceBid = await l2Web3.Eth.GasPrice.SendRequestAsync();
        var setGreetingTx = await l1Greeter.GetFunction("setGreetingInL2").SendTransactionAsync(account.Address, gasParams.MaxSubmissionCost, gasParams.GasLimit, gasPriceBid, new HexBigInteger(gasParams.Deposit));
        var setGreetingRec = await WaitForTransaction(l1Web3, setGreetingTx);

        Console.WriteLine($"Greeting txn confirmed on L1! {setGreetingRec.TransactionHash}");

        // Check L1 to L2 message status
        var l1TxReceipt = new L1TransactionReceipt(setGreetingRec);
        var messages = await l1TxReceipt.GetL1ToL2Messages(account);
        var message = messages[0];
        Console.WriteLine("Waiting for the L2 execution of the transaction. This may take up to 10-15 minutes ⏰");
        var messageResult = await message.WaitForStatusAsync();
        if (messageResult.Status == L1ToL2MessageStatus.Redeemed)
        {
            Console.WriteLine($"L2 retryable ticket is executed {messageResult.L2TxReceipt.TransactionHash}");
        }

        else
        {
            Console.WriteLine($"L2 retryable ticket failed with status {L1ToL2MessageStatus.GetName(messageResult.Status)}");
        }

        // Get updated L2 greeting
        var newGreetingL2 = await l2Greeter.GetFunction("greet").CallAsync<string>();
        Console.WriteLine($"Updated L2 greeting: \"{newGreetingL2}\"");
    }
    private static async Task<TransactionReceipt> WaitForTransaction(Web3 web3, string transactionHash)
    {
        TransactionReceipt receipt = null;
        while (receipt == null)
        {
            receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            await Task.Delay(1000);
        }
        return receipt;
    }
    private static async Task<string> DeployContract(SignerOrProvider signer, Web3 web3, string contractName, string greeting, string inboxAddress = null)
    {
        var abi = LoadAbi(contractName);
        var bytecode = LoadBytecode(contractName);
        var deployment = new ContractDeploymentMessage
        {
            Greeting = greeting,
            Inbox = inboxAddress
        };
        var deploymentHandler = web3.Eth.GetContractDeploymentHandler<ContractDeploymentMessage>();
        var transactionReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deployment);
        return transactionReceipt.ContractAddress;
    }
    private static string LoadAbi(string contractName)
    {
        // Load the ABI from a file or resource
        // For simplicity, this example assumes the ABI is a hardcoded string
        return contractName == "GreeterL1" ? "GreeterL1_ABI" : "GreeterL2_ABI";
    }

    private static string LoadBytecode(string contractName)
    {
        // Load the bytecode from a file or resource
        // For simplicity, this example assumes the bytecode is a hardcoded string
        return contractName == "GreeterL1" ? "GreeterL1_Bytecode" : "GreeterL2_Bytecode";
    }
}