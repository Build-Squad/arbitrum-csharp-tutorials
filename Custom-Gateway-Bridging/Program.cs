using System;
using System.Threading.Tasks;
using Arbitrum.Utils;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using SharedSettings;
using static System.Runtime.InteropServices.JavaScript.JSType;
using NUnit.Framework;
using static Arbitrum.DataEntities.NetworkUtils;
using static Arbitrum.AssetBridger.Erc20Bridger;
using static Arbitrum.Message.L1ToL2MessageUtils;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

        // Set up providers
        var l1Provider = new Web3(l1Rpc);
        var l2Provider = new Web3(l2Rpc);

        // Set up wallets
        var l1Wallet = new Account(devnetPrivKey);
        var l2Wallet = new Account(devnetPrivKey);

        // Set the initial supply of L1 token that we want to bridge
        // Note that you can change the value
        var premine = Web3.Convert.ToWei("3");

        Console.WriteLine("Setting Up Your Token With The Generic Custom Gateway Using Arbitrum SDK Library");

        // Add the default local network configuration to the SDK
        // to allow this script to run on a local node
        AddDefaultLocalNetwork();

        // Use l2Network to create an Arbitrum SDK AdminErc20Bridger instance
        // We'll use AdminErc20Bridger for its convenience methods around registering tokens to the custom gateway
        var l2Network = await GetL2Network(l2Provider);
        var adminTokenBridger = new AdminErc20Bridger(l2Network);

        var l1Gateway = l2Network.TokenBridge.L1CustomGateway;
        var l1Router = l2Network.TokenBridge.L1GatewayRouter;
        var l2Gateway = l2Network.TokenBridge.L2CustomGateway;

        // Deploy our custom token smart contract to L1
        // We give the custom token contract the address of l1CustomGateway and l1GatewayRouter as well as the initial supply (premine)
        var L1CustomToken = new Contract(null, "L1Token", l1Wallet);
        Console.WriteLine("Deploying custom token to L1");
        var l1CustomTokenDeployment = await L1CustomToken.deployAndWaitForReceiptAsync(l1Gateway, l1Router, premine);
        var l1CustomTokenAddress = l1CustomTokenDeployment.ContractAddress;
        Console.WriteLine($"custom token is deployed to L1 at {l1CustomTokenAddress}");

        // Deploy our custom token smart contract to L2
        // We give the custom token contract the address of l2CustomGateway and our l1CustomToken
        var L2CustomToken = new Contract(null, "L2Token", l2Wallet);
        Console.WriteLine("Deploying custom token to L2");
        var l2CustomTokenDeployment = await L2CustomToken.deployAndWaitForReceiptAsync(l2Gateway, l1CustomTokenAddress);
        var l2CustomTokenAddress = l2CustomTokenDeployment.ContractAddress;
        Console.WriteLine($"custom token is deployed to L2 at {l2CustomTokenAddress}");

        Console.WriteLine("Registering custom token on L2:");

        // Register custom token on our custom gateway
        var registerTokenTx = await adminTokenBridger.RegisterCustomToken(l1CustomTokenAddress, l2CustomTokenAddress, l1Wallet, l2Provider);
        var registerTokenRec = await registerTokenTx.wait();
        Console.WriteLine($"Registering token txn confirmed on L1! 🙌 L1 receipt is: {registerTokenRec.TransactionHash}");

        // The L1 side is confirmed; now we listen and wait for the L2 side to be executed; we can do this by computing the expected txn hash of the L2 transaction.
        // To compute this txn hash, we need our message's "sequence numbers", unique identifiers of each L1 to L2 message.
        // We'll fetch them from the event logs with a helper method.
        var l1ToL2Msgs = await registerTokenRec.getL1ToL2Messages(l2Provider);

        // In principle, a single L1 txn can trigger any number of L1-to-L2 messages (each with its own sequencer number).
        // In this case, the registerTokenOnL2 method created 2 L1-to-L2 messages;
        // - (1) one to set the L1 token to the Custom Gateway via the Router, and
        // - (2) another to set the L1 token to its L2 token address via the Generic-Custom Gateway
        // Here, We check if both messages are redeemed on L2
        Assert.That(l1ToL2Msgs.Count(), Is.EqualTo(2), "Should be 2 messages.");

        var setTokenTx = await l1ToL2Msgs[0].waitForStatus();
        Assert.That(setTokenTx.status, Is.EqualTo(L1ToL2MessageStatus.REDEEMED), "Set token not redeemed.");

        var setGateways = await l1ToL2Msgs[1].waitForStatus();
        Assert.That(setGateways.status, Is.EqualTo(L1ToL2MessageStatus.REDEEMED), "Set gateways not redeemed.");

        Console.WriteLine("Your custom token is now registered on our custom gateway 🥳  Go ahead and make the deposit!");
    }
}
