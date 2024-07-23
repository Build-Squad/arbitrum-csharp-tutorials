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
    public static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        Console.WriteLine("Deposit Eth via Arbitrum SDK");

        // Read values from appsettings.json
        var walletPrivateKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1RpcUrl = configuration["DevelopmentSettings:L1RPC"];
        var l2RpcUrl = configuration["DevelopmentSettings:L2RPC"];


        // Set up L1 / L2 wallets connected to providers
        var l1wallet = new Account("0xdf57089febbacf7ba0bc227dafbffa9fc08a93fdc68e1e42411a14efcf23656e");
        var l2wallet = new Account("0xdf57089febbacf7ba0bc227dafbffa9fc08a93fdc68e1e42411a14efcf23656e");

        var l1Provider = new Web3(l1wallet, l1RpcUrl);
        var l2Provider = new Web3(l2wallet, l2RpcUrl);

        var senderAddress = l2wallet.Address;

        var l1Signer = new SignerOrProvider(l1wallet, l1Provider);

        // Set the amount to be deposited in L2 (in wei)
        var ethToL2DepositAmount = UnitConversion.Convert.ToWei(0.0005m);

        // Add the default local network configuration to the SDK
        // to allow this script to run on a local node
        var network = AddDefaultLocalNetwork();
        AddCustomNetwork(network.l1Network, network.l2Network);

        // Use l2Network to create an Arbitrum SDK EthBridger instance
        // We'll use EthBridger for its convenience methods around transferring ETH to L2
        //var l2Network = await GetL2Network(l2Provider);
        var l2Network = network.l2Network;
        var ethBridger = new EthBridger(l2Network);

        // Get the initial balance of the sender wallet 
        var l2WalletInitialEthBalance = await l2Provider.Eth.GetBalance.SendRequestAsync(l2wallet.Address);
        Console.WriteLine($"Initial L2 wallet Balance: {Web3.Convert.FromWei(l2WalletInitialEthBalance)} ETH");

        // Transfer ether from L1 to L2
        // This convenience method automatically queries for the retryable's max submission cost and forwards the appropriate amount to L2
        var depositTx = await ethBridger.Deposit(new EthDepositParams { L1Signer = l1Signer, Amount = ethToL2DepositAmount });

        Console.WriteLine("Now we wait for L2 side of the transaction to be executed ⏳");
        //await depositTx.WaitForL2(l2Provider);

        // Display transaction receipt details
        Console.WriteLine($"Transaction Hash: {depositTx.TransactionHash}");
        Console.WriteLine($"Transaction was mined in block: {depositTx.BlockNumber}");
        Console.WriteLine($"Transaction status: {(depositTx.Status.Value == 1 ? "Success" : "Failed")}");
        Console.WriteLine($"Gas used: {depositTx.GasUsed}");
        Console.WriteLine($"Cumulative gas used: {depositTx.CumulativeGasUsed}");

        // Get the final balance of the sender wallet on L1
        var l2WalletUpdatedEthBalance = await l1Provider.Eth.GetBalance.SendRequestAsync(l2wallet.Address);
        Console.WriteLine($"L2 wallet Balance now is: {Web3.Convert.FromWei(l2WalletUpdatedEthBalance)} ETH");

        Console.WriteLine($"Your L2 ETH balance is updated from {Web3.Convert.FromWei(l2WalletInitialEthBalance)} to {Web3.Convert.FromWei(l2WalletUpdatedEthBalance)}");
        Console.WriteLine($"Amount Transferred: {Web3.Convert.FromWei(ethToL2DepositAmount)} ETH");

    }
}
