using System;
using Arbitrum.DataEntities;
using Arbitrum.Utils;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Accounts;
using Nethereum.Util;
using Nethereum.Web3;
using SharedSettings;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Nethereum.Web3.Accounts;

class Program
{
    static void Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

        try
        {
            Console.WriteLine("Simple Pet Shop DApp");

            var wallet = new Account("0x45b8c07d7aeb36e4474d9e72790a0194885243fbb6dd62169d39b402feb43386");
            var web3 = new Web3(wallet, l2Rpc);

            //web3.Client.OverridingRequestInterceptor = new GethPoAMiddleware();
            //web3.Client.OverridingRequestInterceptor = new SignAndSendRawMiddleware(account);

            Console.WriteLine("Your wallet address: " + wallet.Address);

            // Define the deployment message
            var deployment = new PetDogShopDeployment()
            {
                FromAddress = wallet.Address,
            };
            var transactionInput = deployment.CreateTransactionInput();

            // Estimate gas for deployment
            var gasEstimate = web3.Eth.GetContractDeploymentHandler<PetDogShopDeployment>()
                                            .EstimateGasAsync(deployment).Result;
            deployment.Gas = gasEstimate;

            var transactionReceiptDeployment = web3.Eth.GetContractDeploymentHandler<PetDogShopDeployment>()
                                                    .SendRequestAndWaitForReceiptAsync(deployment).Result;

            var contractAddress = transactionReceiptDeployment.ContractAddress;

            Console.WriteLine($"Adoption contract deployed at address: {contractAddress}");

            var contractHandler = web3.Eth.GetContractHandler(contractAddress);

            var contract = LoadContractUtils.LoadContract("Adoption", web3, wallet.Address, true).Result;

            var adoptFunction = contract.GetFunction("adopt");

            // The id of the pet that will be used for testing
            var expectedPetId = 8;

            // Adopting a pet
            Console.WriteLine("Adopting pet:");
            var gas = adoptFunction.EstimateGasAsync(wallet.Address).Result;

            var adoptionReceipt = adoptFunction.SendTransactionAndWaitForReceiptAsync(wallet.Address).Result;

            // Check if the adoption event was emitted
            if (adoptionReceipt.Logs.Count > 0)
            {
                Console.WriteLine("Pet adoption event emitted");
            }
            else
            {
                Console.WriteLine("No adoption event emitted");
            }

            // Testing retrieval of a single pet's owner
            var adoptersFunction = contract.GetFunction("adopters");

            var adopter = adoptersFunction.CallAsync<string>(expectedPetId).Result;

            Console.WriteLine($"Pet adopted; owner: {adopter}");

            // Testing retrieval of all pet owners
            var getAdoptersFunction = contract.GetFunction("getAdopters");

            var adopters = getAdoptersFunction.CallAsync<string[]>().Result;

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
    public partial class PetDogShopDeployment : ContractDeploymentMessage
    {
        public static string BYTECODE = "6102806040526000608081815260a082905260c082905260e08290526101008290526101208290526101408290526101608290526101808290526101a08290526101c08290526101e082905261020082905261022082905261024082905261026082905261006f91906010610082565b5034801561007c57600080fd5b506100ef565b82601081019282156100ca579160200282015b828111156100ca57825182546001600160a01b0319166001600160a01b03909116178255602090920191600190910190610095565b506100d69291506100da565b5090565b5b808211156100d657600081556001016100db565b61024f806100fe6000396000f3fe608060405234801561001057600080fd5b50600436106100415760003560e01c80633de4eb171461004657806343ae80d3146100645780638588b2c51461008f575b600080fd5b61004e6100b0565b60405161005b91906101af565b60405180910390f35b6100776100723660046101ea565b6100f6565b6040516001600160a01b03909116815260200161005b565b6100a261009d3660046101ea565b610116565b60405190815260200161005b565b6100b8610190565b604080516102008101918290529060009060109082845b81546001600160a01b031681526001909101906020018083116100cf575050505050905090565b6000816010811061010657600080fd5b01546001600160a01b0316905081565b6000600f82111561012657600080fd5b336000836010811061013a5761013a610203565b0180546001600160a01b0319166001600160a01b03929092169190911790556040518281527f87b11a36a7d8951907601f47f6fa4a32bffadebbff2e4894922b25922d340d859060200160405180910390a15090565b6040518061020001604052806010906020820280368337509192915050565b6102008101818360005b60108110156101e15781516001600160a01b03168352602092830192909101906001016101b9565b50505092915050565b6000602082840312156101fc57600080fd5b5035919050565b634e487b7160e01b600052603260045260246000fdfea26469706673582212209b7a6efb1d439d2b8701e018e08d54579f4d35a1458973a5a97aa0efe22df47964736f6c63430008130033";

        public PetDogShopDeployment() : base(BYTECODE) { }
        public PetDogShopDeployment(string byteCode) : base(byteCode) { }
    }
    [Function("adopt", "uint256")]
    public class AdoptFunction : FunctionMessage
    {
        [Parameter("uint256", "petId", 1)]
        public int PetId { get; set; }
    }
    public class GethPoAMiddleware : RequestInterceptor
    {
        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request,
            string route = null)
        {
            // Modify the request if needed (e.g., add extra headers, modify payload, etc.)
            return await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        }

        public override async Task InterceptSendRequestAsync(
            Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request,
            string route = null)
        {
            // Modify the request if needed (e.g., add extra headers, modify payload, etc.)
            await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            // Modify the request if needed (e.g., add extra headers, modify payload, etc.)
            return await interceptedSendRequestAsync(method, route, paramList).ConfigureAwait(false);
        }

        public override Task InterceptSendRequestAsync(
            Func<string, string, object[], Task> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            // Modify the request if needed (e.g., add extra headers, modify payload, etc.)
            return interceptedSendRequestAsync(method, route, paramList);
        }
    }
    public class SignAndSendRawMiddleware : RequestInterceptor
    {
        private readonly Account _account;
        private readonly Web3 _provider;

        public SignAndSendRawMiddleware(Account account)
        {
            _account = account;
            _provider = new Web3(_account);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request,
            string route = null)
        {
            if (request.Method == "eth_sendTransaction")
            {
                var transactionInput = request.RawParameters[0] as TransactionInput;
                if (transactionInput != null)
                {
                    var txnHash = await _account.TransactionManager.SendTransactionAsync(transactionInput);
                    return txnHash;
                }
            }

            return await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        }

        public override async Task InterceptSendRequestAsync(
            Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request,
            string route = null)
        {
            if (request.Method == "eth_sendTransaction")
            {
                var transactionInput = request.RawParameters[0] as TransactionInput;
                if (transactionInput != null)
                {
                    var txnHash = await _account.TransactionManager.SendTransactionAsync(transactionInput);
                    return;
                }
            }

            await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            if (method == "eth_sendTransaction" && paramList.Length > 0 && paramList[0] is TransactionInput)
            {
                var transactionInput = paramList[0] as TransactionInput;
                if (transactionInput != null)
                {
                    var txnHash = await _account.TransactionManager.SendTransactionAsync(transactionInput);
                    return txnHash;
                }
            }

            return await interceptedSendRequestAsync(method, route, paramList).ConfigureAwait(false);
        }

        public override Task InterceptSendRequestAsync(
            Func<string, string, object[], Task> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            if (method == "eth_sendTransaction" && paramList.Length > 0 && paramList[0] is TransactionInput)
            {
                var transactionInput = paramList[0] as TransactionInput;
                if (transactionInput != null)
                {
                    var txnHash = _account.TransactionManager.SendTransactionAsync(transactionInput).Result;
                    return Task.FromResult(txnHash);
                }
            }

            return interceptedSendRequestAsync(method, route, paramList);
        }
    }
}
