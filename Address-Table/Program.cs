using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Web3;
using static Arbitrum.Utils.LoadContractUtils;
using Nethereum.Web3.Accounts;
using System.Numerics;

class Program
{
    static async Task Main(string[] args)
    {
        var web3 = new Web3("https://rinkeby.infura.io/v3/your_infura_project_id"); // Use your Infura project ID or your own node URL

        var myAddress = "0xYourAddress"; // Replace with your Ethereum address

        var arbAddressTableAddress = "0x0000000000000000000000000000000000000066"; // Arbitrum address table address

        var arbAddressTable = await LoadContract("ArbAddressTable", web3, arbAddressTableAddress, false);

        // Check if address is registered
        var addressIsRegistered = await arbAddressTable.GetFunction("addressExists").CallAsync<bool>(myAddress);

        if (!addressIsRegistered)
        {
            // Register address if not already registered
            var txnRes = await arbAddressTable.GetFunction("register").SendTransactionAndWaitForReceiptAsync(myAddress);
            Console.WriteLine($"Successfully registered address {myAddress} to address table");
        }
        else
        {
            Console.WriteLine($"Address {myAddress} already (previously) registered to table");
        }

        // Lookup address index
        var addressIndex = await arbAddressTable.GetFunction("lookup").CallAsync<dynamic>(myAddress);

        // Assume arbitrumVIPContractAddress is your ArbitrumVIP contract address
        var arbitrumVIPContractAddress = "0xYourArbitrumVIPContractAddress";

        // Load ArbitrumVIP contract
        var arbitrumVIPContractAbi = @"[{\"constant\":false,\"inputs\":[{\"name\":\"_addressIndex\",\"type\":\"uint256\"}],\"name\":\"addVIPPoints\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";
        var arbitrumVIPContract = new Contract(null, arbitrumVIPContractAbi, web3, null, arbitrumVIPContractAddress);

        // Check if the address is registered in the Arbitrum address table
        var addressIsRegistered = await arbAddressTable.AddressExistsQueryAsync(myAddress);
        if (!addressIsRegistered)
        {
            // Register the address if not already registered
            var txnRes = await arbAddressTable.RegisterRequestAndWaitForReceiptAsync(myAddress);
            Console.WriteLine($"Successfully registered address {myAddress} to address table");
        }
        else
        {
            Console.WriteLine($"Address {myAddress} already (previously) registered to table");
        }

        // Retrieve the address index from the Arbitrum address table
        var addressIndex = await arbAddressTable.LookupQueryAsync(myAddress);

        // Call the addVIPPoints function of ArbitrumVIP contract with the address index
        var arbitrumVIPContractAddress = "0xYourArbitrumVIPContractAddress"; // Replace with your ArbitrumVIP contract address
        var arbitrumVIPContract = new ArbVIPContract__factory().CreateContract(new Account("YourPrivateKey"), new Nethereum.RPC.Eth.DTOs.BlockParameter(123456), null, null, arbitrumVIPContractAddress);
        var txnRes2 = await arbitrumVIPContract.AddVIPPointsRequestAndWaitForReceiptAsync(addressIndex);
        Console.WriteLine($"Successfully added VIP points using address w/ index {addressIndex}");


    }
}
