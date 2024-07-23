using System;
using Arbitrum.DataEntities;
using Arbitrum.Utils;
using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using SharedSettings;
using Nethereum.Web3.Accounts;

class Program
{
    public static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

        try
        {
            Console.WriteLine("Simple Pet Shop DApp");

            //hardcoding the private key as of now for testing
            var l2wallet = new Account("0x394d84674d89ffd6d02b8b768642ffc15cfd02ec7ea593c3d1cfc22da07f50dc", 421614);
            var web3 = new Web3(l2wallet, l2Rpc);

            Console.WriteLine("Your l2wallet address: " + l2wallet.Address);

            //helper method to get abi and bytecode
            var (abi, byteCode) = ConfigurationHelper.GetAbiAndBytecode("DepositContract");

            //estimate gas for contract deployment
            var gas = await web3.Eth.DeployContract.EstimateGasAsync(
                                                abi: abi,
                                                contractByteCode: byteCode,
                                                from: l2wallet.Address);

            //deploy contract and obtain the receipt
            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                            abi,
                            byteCode,
                            l2wallet.Address,
                            gas,
                            null);

            var contractAddress = receipt.ContractAddress;

            // Check if contract is deployed
            if (!await LoadContractUtils.IsContractDeployed(web3, contractAddress))
            {
                throw new ArbSdkError("Token not deployed");
            }
            else
            {
                Console.WriteLine($"Adoption contract deployed at address: {contractAddress}");
            }

            //get the contract
            var contract = web3.Eth.GetContract(abi, contractAddress);

            // The id of the pet that will be used for testing
            var expectedPetId = 8;

            // The expected owner of adopted pet is your l2wallet
            var expectedAdopter = l2wallet.Address;

            //var adoptFunction = contract.GetFunction("adopt");
            var depositEthFunction = contract.GetFunction("depositEth");
            var estimatedGas = await depositEthFunction.EstimateGasAsync();
            var functionReceipt = await depositEthFunction.SendTransactionAndWaitForReceiptAsync(expectedAdopter , estimatedGas, null, null);

            var result = await depositEthFunction.CallAsync<dynamic>();

            // Adopting a pet
            Console.WriteLine("Adopting pet:");

            //estimate gas for adoption function
            //var estimatedGas = await adoptFunction.EstimateGasAsync(expectedPetId);

            //get the receipt
            //var adoptionReceipt = await adoptFunction.SendTransactionAndWaitForReceiptAsync(expectedAdopter , estimatedGas, null, null, expectedPetId);

            //Check if the adoption event was emitted
            //if (adoptionReceipt.Logs.Count > 0)
            //{
            //    Console.WriteLine("Pet adoption event emitted");
            //}
            //else
            //{
            //    Console.WriteLine("No adoption event emitted");
            //}

            // Testing retrieval of a single pet's owner
            var adoptersFunction = contract.GetFunction("adopters");

            var adopter = await adoptersFunction.CallAsync<dynamic>(expectedPetId);

            Console.WriteLine($"Pet adopted; owner: {adopter}");

            // Testing retrieval of all pet owners
                var getAdoptersFunction = contract.GetFunction("getAdopters");

            var adopters = await getAdoptersFunction.CallAsync<List<string>>();

            Console.WriteLine("All pet owners:");

            foreach (var a in adopters)
            {
                Console.WriteLine(a);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}

