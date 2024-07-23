//using System;
//using Arbitrum.Utils;
//using CommandLine;
//using Microsoft.Extensions.Configuration;
//using Nethereum.ABI.FunctionEncoding.Attributes;
//using Nethereum.Contracts;
//using Nethereum.Contracts.ContractHandlers;
//using Nethereum.Hex.HexConvertors.Extensions;
//using Nethereum.Hex.HexTypes;
//using Nethereum.Web3;
//using Nethereum.Web3.Accounts;
//using SharedSettings;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//class Program
//{
//    static async void Main(string[] args)
//    {

//        Parser.Default.ParseArguments<Options>(args)
//              .WithParsed(RunOptions)
//              .WithNotParsed(HandleParseError);

//        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

//        // Read values from appsettings.json
//        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
//        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
//        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

//        if (string.IsNullOrEmpty(l2Rpc))
//        {
//            throw new ArgumentException("L2RPC not defined in env!");
//        }

//        var l1Web3 = new Web3(l1Rpc);
//        var l2Web3 = new Web3(l2Rpc);

//        var action = args.Length > 0 ? args[0] : string.Empty;
//        var txHash = args.Length > 1 ? args[1] : string.Empty;

//        switch (action)
//        {
//            case "checkConfirmation":
//                if (string.IsNullOrEmpty(txHash))
//                {
//                    Console.WriteLine("Transaction hash is required for checkConfirmation action.");
//                    return;
//                }

//                var confirmations = await TutorialsUtils.CheckConfirmation(txHash, l2Web3);
//                if (confirmations == 0)
//                {
//                    Console.WriteLine("Block has not been submitted to l1 yet, please check it later...");
//                }
//                else
//                {
//                    Console.WriteLine($"Congrats! This block has been submitted to l1 for {confirmations} blocks");
//                }
//                break;

//            case "findSubmissionTx":
//                if (string.IsNullOrEmpty(l1Rpc))
//                {
//                    throw new ArgumentException("L1RPC not defined in env!");
//                }

//                if (string.IsNullOrEmpty(txHash))
//                {
//                    Console.WriteLine("Transaction hash is required for findSubmissionTx action.");
//                    return;
//                }

//                var submissionTx = await TutorialsUtils.FindSubmissionTx(txHash, l1Web3, l2Web3);
//                if (string.IsNullOrEmpty(submissionTx))
//                {
//                    Console.WriteLine("No submission transaction found. (If event too old some rpc will discard it)");
//                }
//                else
//                {
//                    Console.WriteLine($"Submission transaction found: {submissionTx}");
//                }
//                break;

//            default:
//                Console.WriteLine($"Unknown action: {action}");
//                break;
//        }
//    }

//    private static void HandleParseError(IEnumerable<Error> errs)
//    {
//        // Handle errors
//        foreach (var error in errs)
//        {
//            Console.WriteLine(error.ToString());
//        }
//    }
//    private static void RunOptions(Options opts)
//    {
//        // Handle options
//        Console.WriteLine($"L2 Network ID: {opts.L2NetworkID}");
//        Console.WriteLine($"Action: {opts.Action}");
//        Console.WriteLine($"Transaction Hash: {opts.TxHash}");
//    }
//}
//public class Options
//{
//    [Option('n', "l2NetworkID", Required = false, HelpText = "L2 Network ID.")]
//    public int? L2NetworkID { get; set; }

//    [Option('a', "action", Required = true, HelpText = "Action to perform.")]
//    public string Action { get; set; }

//    [Option('t', "txHash", Required = true, HelpText = "Transaction hash.")]
//    public string TxHash { get; set; }
//}

////With this setup, you can use command-line arguments similar to the ones in your JavaScript script. For example:
////dotnet run --action checkConfirmation --txHash 0x1234