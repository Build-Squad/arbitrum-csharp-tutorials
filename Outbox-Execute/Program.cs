using System;
using Arbitrum.DataEntities;
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
using static Arbitrum.DataEntities.NetworkUtils;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Arbitrum.Message;

class Program
{
    static async void Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

        // Set up L1 wallet connected to provider
        var account = new Account(devnetPrivKey);
        var l1Web3 = new Web3(account, l1Rpc);
        var l2Web3 = new Web3(l2Rpc);

        Console.WriteLine("Outbox Execution");

        // Add the default local network configuration to the SDK
        AddDefaultLocalNetwork();

        // We start with a txn hash; we assume this is a transaction that triggered an L2 to L1 Message on L2
        if (args.Length == 0)
        {
            throw new ArgumentException("Provide a transaction hash of an L2 transaction that sends an L2 to L1 message");
        }

        var txnHash = args[0];
        if (!txnHash.StartsWith("0x") || txnHash.Trim().Length != 66)
        {
            throw new ArgumentException($"Hmm, {txnHash} doesn't look like a txn hash...");
        }

        // Find the Arbitrum txn from the txn hash provided
        var receipt = await l2Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txnHash);
        var l2Receipt = new L2TransactionReceipt(receipt);

        // Assuming there's only one outgoing message
        var messages = await l2Receipt.GetL2ToL1Messages(account);
        var l2ToL1Msg = messages[0];

        // Check if already executed
        if (await l2ToL1Msg.StatusAsync(l2Web3) == L2ToL1MessageStatus.Executed)
        {
            Console.WriteLine("Message already executed! Nothing else to do here");
            return;
        }

        // Wait until the L2 block is confirmed on L1
        var timeToWaitMs = 1000 * 60;
        Console.WriteLine("Waiting for the outbox entry to be created. This only happens when the L2 block is confirmed on L1, ~1 week after its creation.");
        await l2ToL1Msg.WaitUntilReadyToExecuteAsync(l2Web3, timeToWaitMs);
        Console.WriteLine("Outbox entry exists! Trying to execute now");

        // Execute the message in its outbox entry
        var res = await l2ToL1Msg.ExecuteAsync(l2Web3);
        var rec = await res.WaitForReceiptAsync(l2Web3);
        Console.WriteLine("Done! Your transaction is executed", rec.TransactionHash);
    }
}