//using System;
//using System.Numerics;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using Nethereum.ABI.FunctionEncoding.Attributes;
//using Nethereum.Contracts;
//using Nethereum.Web3;
//using static Arbitrum.DataEntities.NetworkUtils;
//using Nethereum.Web3.Accounts;
//using SharedSettings;

//class Program
//{
//    private static Web3 l1Web3;
//    private static Web3 l2Web3;
//    private static bool useCreatedNodeInsteadOfConfirmed = false;

//    static async Task Main(string[] args)
//    {
//        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

//        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
//        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

//        if (string.IsNullOrEmpty(l1Rpc) || string.IsNullOrEmpty(l2Rpc))
//        {
//            Console.WriteLine("L1RPC or L2RPC is not defined in environment variables.");
//            return;
//        }

//        l1Web3 = new Web3(l1Rpc);
//        l2Web3 = new Web3(l2Rpc);

//        if (args.Length < 1)
//        {
//            Console.WriteLine($"Missing L2 block number to verify whether it has been processed in the latest {(useCreatedNodeInsteadOfConfirmed ? "created" : "confirmed")} RBlock/node");
//            Console.WriteLine("Usage: dotnet run <L2 block number>");
//            return;
//        }

//        if (!int.TryParse(args[0], out int l2BlockNumber))
//        {
//            Console.WriteLine("Invalid L2 block number.");
//            return;
//        }

//        await Main(l2BlockNumber);
//    }

//    private static async Task Main(int l2BlockNumberToVerify)
//    {
//        Console.WriteLine("Find whether an L2 block has been processed as part of an RBlock");

//        // Add the default local network configuration to the SDK
//        AddDefaultLocalNetwork();

//        // Use l2Network to find the Rollup contract's address and instantiate a contract handler
//        var l2Network = await GetL2Network(l2Web3);
//        var rollupAddress = l2Network.EthBridge.Rollup;
//        var rollupContract = new Contract(l1Web3, RollupCoreABI, rollupAddress);

//        Console.WriteLine($"Rollup contract found at address {rollupContract.Address}");

//        // Get the latest node created or confirmed
//        var nodeId = useCreatedNodeInsteadOfConfirmed
//            ? await rollupContract.GetFunction("latestNodeCreated").CallAsync<BigInteger>()
//            : await rollupContract.GetFunction("latestConfirmed").CallAsync<BigInteger>();

//        Console.WriteLine($"Latest {(useCreatedNodeInsteadOfConfirmed ? "created" : "confirmed")} Rblock/node: {nodeId}");

//        // Find the NodeCreated event
//        var nodeCreatedEventFilter = rollupContract.GetEvent("NodeCreated").CreateFilterInput(nodeId);
//        var nodeCreatedEvents = await rollupContract.GetEvent("NodeCreated").GetAllChanges<EventLog<NodeCreatedEventDTO>>(nodeCreatedEventFilter);

//        if (nodeCreatedEvents == null || nodeCreatedEvents.Count == 0)
//        {
//            Console.WriteLine($"INTERNAL ERROR: NodeCreated events not found for Rblock/node: {nodeId}");
//            return;
//        }

//        var nodeCreatedEvent = nodeCreatedEvents[0];
//        Console.WriteLine($"NodeCreated event found in transaction {nodeCreatedEvent.Log.TransactionHash}");

//        // Finding the assertion within the NodeCreated event, and getting the afterState
//        if (nodeCreatedEvent.Event == null)
//        {
//            Console.WriteLine($"INTERNAL ERROR: NodeCreated event does not have an assertion for Rblock/node: {nodeId}");
//            return;
//        }

//        var assertion = nodeCreatedEvent.Event.Assertion;
//        var afterState = assertion.AfterState;

//        // Latest L2 block hash processed is in the first element of the bytes32Vals property in the globalState
//        var lastL2BlockHash = afterState.GlobalState.Bytes32Vals[0];
//        Console.WriteLine($"Last L2 block hash processed in this Rblock/node: {lastL2BlockHash}");

//        // Getting the block number from that block hash
//        var lastL2Block = await l2Web3.Eth.Blocks.GetBlockWithTransactionsByHash.SendRequestAsync(lastL2BlockHash.ToString());
//        var lastL2BlockNumber = (int)lastL2Block.Number.Value;
//        Console.WriteLine($"Last L2 block number processed in this Rblock/node: {lastL2BlockNumber}");

//        // Final verification
//        Console.WriteLine("************");
//        if (lastL2BlockNumber > l2BlockNumberToVerify)
//        {
//            Console.WriteLine($"{l2BlockNumberToVerify} has been processed as part of the latest {(useCreatedNodeInsteadOfConfirmed ? "created" : "confirmed")} RBlock/node");
//        }
//        else
//        {
//            Console.WriteLine($"{l2BlockNumberToVerify} has NOT been processed as part of the latest {(useCreatedNodeInsteadOfConfirmed ? "created" : "confirmed")} RBlock/node");
//        }
//        Console.WriteLine("************");
//    }


//    // Placeholder for RollupCore ABI
//    private static readonly string RollupCoreABI = "[]";


//    // Placeholder for Assertion class
//    private class Assertion
//    {
//        public GlobalState AfterState { get; set; }
//    }

//    // Placeholder for GlobalState class
//    private class GlobalState
//    {
//        public byte[][] Bytes32Vals { get; set; }
//    }
//}
