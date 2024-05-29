using System;
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

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];

        Console.WriteLine("Deposit Eth via Arbitrum SDK");

        // Set up L1 / L2 wallets connected to providers
        var walletPrivateKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1RpcUrl = configuration["DevelopmentSettings:L1RPC"];
        var l2RpcUrl = configuration["DevelopmentSettings:L2RPC"];

        var account = new Account(walletPrivateKey);
        var l2Provider = new Web3(account, l2RpcUrl);
        var l2WalletAddress = l2Provider.TransactionManager.Account.Address;

        var ethFromL2WithdrawAmount = Web3.Convert.ToWei(0.000001);

        await Console.Out.WriteLineAsync("Withdraw Eth via Arbitrum SDK");

        // Replace with proper network configuration if necessary
        // AddDefaultLocalNetwork();

        var l2WalletInitialEthBalance = await l2Provider.Eth.GetBalance.SendRequestAsync(l2WalletAddress);

        if (l2WalletInitialEthBalance.Value < ethFromL2WithdrawAmount)
        {
            Console.WriteLine($"Oops - not enough ether; fund your account L2 wallet currently {l2WalletAddress} with at least 0.000001 ether");
            Environment.Exit(1);
        }
        Console.WriteLine("Wallet properly funded: initiating withdrawal now");

        var l2Network = await GetL2Network(l2Provider);
        var ethBridger = new EthBridger(l2Network);

        var l2Signer = new SignerOrProvider(account, l2Provider);

        var withdrawTx = await ethBridger.Withdraw(new EthWithdrawParams { L2Signer = l2Signer, DestinationAddress = l2WalletAddress, Amount = ethFromL2WithdrawAmount });
        //var withdrawRec = await l2Provider.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(withdrawTx);

        Console.WriteLine($"Ether withdrawal initiated! 🥳 {withdrawTx.TransactionHash}");

        Console.WriteLine("To claim funds (after dispute period), see outbox-execute repo 🫡");
    }
}
