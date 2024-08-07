﻿using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Microsoft.Extensions.Configuration;
using static Arbitrum.DataEntities.NetworkUtils;

using Arbitrum.AssetBridgerModule;
using Arbitrum.DataEntities;
using SharedSettings;

public class Program
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

        var l2WalletAddress = l2Provider.TransactionManager.Account.Address;

        var ethFromL2WithdrawAmount = Web3.Convert.ToWei(0.000001);

        Console.WriteLine("Withdraw Eth via Arbitrum SDK");

        // Add the default local network configuration to the SDK
        // to allow this script to run on a local node
        var network = AddDefaultLocalNetwork();

        AddCustomNetwork(network.l1Network, network.l2Network);
        var l2Network = await GetL2Network(l2Provider);
        var ethBridger = new EthBridger(l2Network);

        var l2WalletInitialEthBalance = await l2Provider.Eth.GetBalance.SendRequestAsync(l2WalletAddress);

        if (l2WalletInitialEthBalance.Value < ethFromL2WithdrawAmount)
        {
            Console.WriteLine($"Oops - not enough ether; fund your account L2 wallet currently {l2WalletAddress} with at least 0.000001 ether");
            Environment.Exit(1);
        }
        Console.WriteLine("Wallet properly funded: initiating withdrawal now");

        var l2Signer = new SignerOrProvider(l2wallet, l2Provider);

        var withdrawTx = await ethBridger.Withdraw(new EthWithdrawParams { L2Signer = l2Signer, DestinationAddress = l2WalletAddress, Amount = ethFromL2WithdrawAmount });

        Console.WriteLine($"Ether withdrawal initiated! 🥳 {withdrawTx.TransactionHash}");

        Console.WriteLine("To claim funds (after dispute period), see outbox-execute repo 🫡");
    }
}
