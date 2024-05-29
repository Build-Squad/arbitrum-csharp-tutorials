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
using Arbitrum.Message.Tests.Integration;
using Arbitrum.Message;
using static Arbitrum.Utils.Lib;
using Nethereum.Hex.HexTypes;
using SharedSettings;

public class Program
{

    public static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];

        Console.WriteLine("Deposit Eth via Arbitrum SDK");

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

        // Initialize the Arbitrum SDK (equivalent) - this is a placeholder
        var l2Network = await GetL2NetworkAsync(l2Web3);
        var ethBridger = new EthBridger(l2Network);
        var inboxAddress = ethBridger.L2Network.EthBridge.Inbox;

        // Deploy contracts
        var l1GreeterAddress = await DeployContractAsync(l1Signer, "GreeterL1", "Hello world in L1");
        var l2GreeterAddress = await DeployContractAsync(l2Signer, "GreeterL2", "Hello world in L2");

        // Update contract addresses
        await UpdateGreeterTargetsAsync(l1Signer, l1GreeterAddress, l2GreeterAddress);
        await UpdateGreeterTargetsAsync(l2Signer, l2GreeterAddress, l1GreeterAddress);

        // Log the L2 greeting string
        var l2Greeter = l2Web3.Eth.GetContract("ABI", l2GreeterAddress);
        var currentL2Greeting = await l2Greeter.GetFunction("greet").CallAsync<string>();
        Console.WriteLine($"Current L2 greeting: \"{currentL2Greeting}\"");

        // Update greeting from L1 to L2
        var newGreeting = "Greeting from far, far away";
        var calldata = EncodeFunctionCall("setGreeting", newGreeting);

        // Estimate gas parameters
        var gasParams = await EstimateGasParamsAsync(l1Signer, l2Signer, l1GreeterAddress, l2GreeterAddress, calldata);

        // Send greeting to L2
        await SetGreetingInL2Async(l1Web3, l1GreeterAddress, newGreeting, gasParams);

        // Wait for L2 execution and check status
        await WaitForL2ExecutionAsync(l1Signer, l2Signer, l1GreeterAddress);

        // Verify updated greeting on L2
        currentL2Greeting = await l2Greeter.GetFunction("greet").CallAsync<string>();
        Console.WriteLine($"Updated L2 greeting: \"{currentL2Greeting}\"");
    }

    private static async Task<string> DeployContractAsync(SignerOrProvider l1Signer, string contractName, params object[] constructorParams)
    {
        var abi = "ABI for the contract"; // Replace with actual ABI
        var bytecode = "Bytecode for the contract"; // Replace with actual bytecode

        var contractDeployment = new ContractDeploymentMessage(bytecode);

        var transactionHash = await l1Signer.Provider.Eth.DeployContract.SendRequestAsync(bytecode, l1Signer.Account.Address);
        var receipt = await l1Signer.Provider.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

        return receipt.ContractAddress;
    }

    private static async Task UpdateGreeterTargetsAsync(SignerOrProvider l1Signer, string greeterAddress, string targetAddress)
    {
        var contract = l1Signer.Provider.Eth.GetContract("ABI for Greeter", greeterAddress);
        var updateTargetFunction = contract.GetFunction("updateTarget");

        var transactionHash = await updateTargetFunction.SendTransactionAsync(targetAddress);
        await l1Signer.Provider.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
    }

    private static string EncodeFunctionCall(string functionName, params object[] functionParams)
    {
        var abi = $"function {functionName}(string _greeting)";
        var contract = new Contract(null, abi, null);
        var function = contract.GetFunction(functionName);

        return function.GetData(functionParams);
    }

    private static async Task<L1ToL2MessageGasParams> EstimateGasParamsAsync(SignerOrProvider l1Web3, SignerOrProvider l2Web3, string l1Address, string l2Address, string calldata)
    {
        var estimator = new L1ToL2MessageGasEstimator(l2Web3.Provider);
        var baseFee = await GetBaseFee(l1Web3.Provider);

        var gasParams = await estimator.EstimateAll(
        new L1ToL2MessageNoGasParams
        {
            From = l1Address,
            To = l2Address,
            L2CallValue = 0,
            ExcessFeeRefundAddress = l2Web3.Account.Address,
            CallValueRefundAddress = l2Web3.Account.Address,
            Data = calldata.HexToByteArray()
        },
        baseFee, 
        l1Web3.Provider,
        new GasOverrides
        {
            GasLimit = new PercentIncreaseWithMin { Base = null, Min = new BigInteger(10000), PercentIncrease = new BigInteger(30)},
            MaxSubmissionFee = new PercentIncreaseType { Base = null, PercentIncrease = new BigInteger(30)},
            MaxFeePerGas = new PercentIncreaseType {Base = null, PercentIncrease = new BigInteger(39) }
        });

        return gasParams;
    }

    private static async Task SetGreetingInL2Async(Web3 web3, string greeterAddress, string newGreeting, L1ToL2MessageGasParams gasParams)
    {
        var contract = web3.Eth.GetContract("ABI for GreeterL1", greeterAddress);
        var setGreetingFunction = contract.GetFunction("setGreetingInL2");

        var transactionHash = await setGreetingFunction.SendTransactionAsync(newGreeting, gasParams.MaxSubmissionCost, gasParams.GasLimit, gasParams.MaxFeePerGas, gasParams.Deposit);
        var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

        Console.WriteLine($"Greeting txn confirmed on L1! 🙌 {receipt.TransactionHash}");
    }

    private static async Task WaitForL2ExecutionAsync(SignerOrProvider l1Web3, SignerOrProvider l2Web3, string l1GreeterAddress)
    {
        var receipt = await l1Web3.Provider.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(l1GreeterAddress);
        var l1TxReceipt = new L1TransactionReceipt(receipt);

        var messages = await l1TxReceipt.GetL1ToL2Messages(l2Web3);
        var message = messages.FirstOrDefault();

        Console.WriteLine("Waiting for the L2 execution of the transaction. This may take up to 10-15 minutes ⏰");

        var messageResult = await message.WaitForStatus();
        var status = messageResult.Status;

        if (status == L1ToL2MessageUtils.L1ToL2MessageStatus.REDEEMED)
        {
            Console.WriteLine($"L2 retryable ticket is executed 🥳 {messageResult.L2TxReceipt.TransactionHash}");
        }
        else
        {
            Console.WriteLine($"L2 retryable ticket is failed with status {status}");
        }
    }
}