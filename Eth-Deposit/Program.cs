using System;
using Arbitrum.AssetBridgerModule;
using Arbitrum.DataEntities;
using Microsoft.Extensions.Configuration;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using static Arbitrum.DataEntities.NetworkUtils;
using Nethereum.JsonRpc.Client;
using SharedSettings;

class Program
{
    static void Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        Console.WriteLine("Deposit Eth via Arbitrum SDK");

        // Read values from appsettings.json
        var walletPrivateKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1RpcUrl = configuration["DevelopmentSettings:L1RPC"];
        var l2RpcUrl = configuration["DevelopmentSettings:L2RPC"];

        // Set up L1 / L2 wallets connected to providers
        var l1Provider = new Web3(l1RpcUrl);
        var l2Provider = new Web3(l2RpcUrl);

        var account = new Account(walletPrivateKey);
        var senderAddress = account.Address;

        //account.TransactionManager.Client = l1Client;
        var l1Signer = new SignerOrProvider(account, l1Provider);

        // Set the amount to be deposited in L2 (in wei)
        var ethToL2DepositAmount = UnitConversion.Convert.ToWei(0.0001m);

        // Add the default local network configuration to the SDK
        // to allow this script to run on a local node
        AddDefaultLocalNetwork();

        // Use l2Network to create an Arbitrum SDK EthBridger instance
        // We'll use EthBridger for its convenience methods around transferring ETH to L2
        var l2Network = GetL2Network(l2Provider).Result;
        var ethBridger = new EthBridger(l2Network);
        var receiverAddress = l2Network?.EthBridge?.Inbox;

        // Get the initial balance of the sender wallet 
        var senderL1Balance = l1Provider.Eth.GetBalance.SendRequestAsync(account.Address).Result;
        Console.WriteLine($"Sender L1 Balance: {Web3.Convert.FromWei(senderL1Balance)} ETH");

        // Get the initial balance of the receiver wallet
        var receiverL1Balance = l1Provider.Eth.GetBalance.SendRequestAsync(receiverAddress).Result;
        Console.WriteLine($"Receiver L1 Balance: {Web3.Convert.FromWei(receiverL1Balance)} ETH");

        // Transfer ether from L1 to L2
        // This convenience method automatically queries for the retryable's max submission cost and forwards the appropriate amount to L2
        var depositTx = ethBridger.Deposit(new EthDepositParams { L1Signer = l1Signer, Amount = ethToL2DepositAmount }).Result;

        // Display transaction receipt details
        Console.WriteLine($"Transaction Hash: {depositTx.TransactionHash}");
        Console.WriteLine($"Transaction was mined in block: {depositTx.BlockNumber}");
        Console.WriteLine($"Transaction status: {(depositTx.Status.Value == 1 ? "Success" : "Failed")}");
        Console.WriteLine($"Gas used: {depositTx.GasUsed}");
        Console.WriteLine($"Cumulative gas used: {depositTx.CumulativeGasUsed}");

        // Get the final balance of the sender wallet on L1
        var senderL1BalanceFinal = l1Provider.Eth.GetBalance.SendRequestAsync(account.Address).Result;
        Console.WriteLine($"Sender L1 Balance now is: {Web3.Convert.FromWei(senderL1BalanceFinal)} ETH");

        // Get the final balance of the receiver wallet on L1
        var receiverL1BalanceFinal = l1Provider.Eth.GetBalance.SendRequestAsync(receiverAddress).Result;
        Console.WriteLine($"Receiver L1 Balance now is: {Web3.Convert.FromWei(receiverL1BalanceFinal)} ETH");

        Console.WriteLine($"Your L2 ETH balance is updated from {receiverL1Balance.ToString()} to {receiverL1BalanceFinal.ToString()}");
    }
}
