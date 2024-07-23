//using System;
//using System.Threading.Tasks;
//using Nethereum.Web3;
//using Nethereum.Web3.Accounts;
//using Nethereum.Contracts;
//using NUnit.Framework;
//using SharedSettings;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        Console.WriteLine("Simple Election DApp");

//        var privateKey = Environment.GetEnvironmentVariable("DEVNET_PRIVKEY");
//        var l2RPC = Environment.GetEnvironmentVariable("L2RPC");

//        var web3 = new Web3(new Account(privateKey), l2RPC);

//        var l2WalletAddress = (await web3.Eth.Accounts.SendRequestAsync())[0];
//        Console.WriteLine($"Your wallet address: {l2WalletAddress}");

//        //get abi and bytecode from compiled contract
//        var (abi, bytecode) = ConfigurationHelper.GetAbiAndBytecode("Election");

//        var electionContract = web3.Eth.GetContract(abi, bytecode);

//        Console.WriteLine("Deploying Election contract to L2");

//        var deploymentReceipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync();

//        Console.WriteLine($"Election contract is deployed to {deploymentReceipt.ContractAddress}");

//        var count = await electionContract.GetFunction("candidatesCount").CallAsync<dynamic>();
//        Assert.That(count, Is.EqualTo(2));
//        Console.WriteLine("The election is indeed initialized with two candidates!");

//        var candidate1 = await electionContract.GetFunction("candidates").CallAsync<(uint, string, uint)>(1);
//        Assert.That(candidate1.Item1, Is.EqualTo(1));
//        Assert.That(candidate1.Item2, Is.EqualTo("Candidate 1"));
//        Assert.That(candidate1.Item3, Is.EqualTo(0));
//        Console.WriteLine("Candidates are initialized with the correct values!");

//        var candidateId = 1;

//        var voteTx1 = await electionContract.GetFunction("vote").SendTransactionAndWaitForReceiptAsync(l2WalletAddress, candidateId);
//        Assert.That(voteTx1.Status.Value, Is.EqualTo(1));
//        Console.WriteLine("Vote tx is executed!");

//        var voted = await electionContract.GetFunction("voters").CallAsync<bool>(l2WalletAddress);
//        Assert.That(voted, Is.True);
//        Console.WriteLine("You have voted for candidate1!");

//        candidate1 = await electionContract.GetFunction("candidates").CallAsync<(uint, string, uint)>(candidateId);
//        Assert.That(candidate1.Item3, Is.EqualTo(1));
//        Console.WriteLine("Candidate1 has one vote!");

//        var candidate2 = await electionContract.GetFunction("candidates").CallAsync<(uint, string, uint)>(2);
//        Assert.That(candidate2.Item3, Is.EqualTo(0));
//        Console.WriteLine("Candidate2 has zero vote!");
//    }
//}
