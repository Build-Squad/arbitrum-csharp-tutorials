﻿using System;
using Arbitrum.Utils;
using SharedSettings;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using static Arbitrum.DataEntities.NetworkUtils;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Numerics;
using Arbitrum.DataEntities;
using Arbitrum.AssetBridger;

class Program
{
    public static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

        //hardcoding private key for testing
        var wallet = new Account("0x394d84674d89ffd6d02b8b768642ffc15cfd02ec7ea593c3d1cfc22da07f50dc", 421614);

        //var wallet = new Account("0x3f1Eae7D46d88F08fc2F8ed27FCb2AB183EB2d0E");
        var l1Provider = new Web3(wallet, l1Rpc);
        var l2Provider = new Web3(wallet, l2Rpc);

        // Add the default local network configuration to the SDK to allow this script to run on a local node
        AddDefaultLocalNetwork();

        var tokenAmount = new BigInteger(50);

        // Deploy DappToken on L1
        Console.WriteLine("Deploying the test DappToken to L1:");

        var l1DappTokenDeployment = new DappTokenDeployment
        {
            FromAddress = wallet.Address,
            InitialSupply = 100 
        };

        l1DappTokenDeployment.CreateTransactionInput();

        //get abi and bytecode from compiled contract
        var a = ConfigurationHelper.GetAbiAndBytecode("DappToken");

        // Estimate gas for deployment
        var gasEstimate = await l1Provider.Eth.GetContractDeploymentHandler<DappTokenDeployment>()
                                        .EstimateGasAsync(l1DappTokenDeployment);

        var l1DappTokenTransactionReceipt = await l1Provider.Eth.GetContractDeploymentHandler<DappTokenDeployment>()
                                        .SendRequestAndWaitForReceiptAsync(l1DappTokenDeployment);

        
        var l1DappTokenAddress = l1DappTokenTransactionReceipt.ContractAddress;
        var contract = l1Provider.Eth.GetContract(a.Abi, l1DappTokenTransactionReceipt.ContractAddress);


        // Check if token is deployed
        if (!(await LoadContractUtils.IsContractDeployed(l1Provider, l1DappTokenAddress)))
        {
            throw new ArbSdkError("Token not deployed");
        }
        else
        {
            Console.WriteLine($"DappToken is deployed to L1 at {l1DappTokenAddress}");
        }
        var l2Network = await GetL2Network(l2Provider);
        var erc20Bridger = new Erc20Bridger(l2Network);

        var l1Erc20Address = l1DappTokenAddress;

        var expectedL1GatewayAddress = await erc20Bridger.GetL1GatewayAddress(l1Erc20Address, l1Provider);

        var l1DappToken = await LoadContractUtils.LoadContract("DappToken", l1Provider, l1Erc20Address);

        //var initialBridgeTokenBalance = await l1DappTokenTransactionReceipt

        //// Approve the token transfer to the bridge
        //Console.WriteLine("Approving:");

        //var approveFunction = new ApproveFunction
        //{
        //    Spender = expectedL1GatewayAddress,
        //    Value = tokenDepositAmount
        //};

        var approveHandler = l1Provider.Eth.GetContractTransactionHandler<ApproveFunction>();

        //var approveTransactionReceipt = await approveHandler.SendRequestAndWaitForReceiptAsync(l1DappTokenAddress, approveFunction);

        //Console.WriteLine($"You successfully allowed the Arbitrum Bridge to spend DappToken {approveTransactionReceipt.TransactionHash}");

        // Deposit the token to L2 (this will require a custom implementation based on Arbitrum's bridge contract)
        Console.WriteLine("Transferring DappToken to L2:");
    }
}

[Function("approve", "bool")]
public class ApproveFunction : FunctionMessage
{
    [Parameter("address", "_spender", 1)]
    public string Spender { get; set; }

    [Parameter("uint256", "_value", 2)]
    public BigInteger Value { get; set; }
}
public class DappTokenDeployment : ContractDeploymentMessage
{
    public static string BYTECODE = "60806040523480156200001157600080fd5b5060405162000c6438038062000c6483398101604081905262000034916200024c565b6040518060400160405280600a8152602001692230b838102a37b5b2b760b11b815250604051806040016040528060048152602001630444150560e41b81525081600390816200008591906200030b565b5060046200009482826200030b565b505050620000cb33620000ac620000d260201b60201c565b620000b990600a620004ec565b620000c5908462000504565b620000d7565b5062000534565b601290565b6001600160a01b038216620001075760405163ec442f0560e01b8152600060048201526024015b60405180910390fd5b620001156000838362000119565b5050565b6001600160a01b038316620001485780600260008282546200013c91906200051e565b90915550620001bc9050565b6001600160a01b038316600090815260208190526040902054818110156200019d5760405163391434e360e21b81526001600160a01b03851660048201526024810182905260448101839052606401620000fe565b6001600160a01b03841660009081526020819052604090209082900390555b6001600160a01b038216620001da57600280548290039055620001f9565b6001600160a01b03821660009081526020819052604090208054820190555b816001600160a01b0316836001600160a01b03167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef836040516200023f91815260200190565b60405180910390a3505050565b6000602082840312156200025f57600080fd5b5051919050565b634e487b7160e01b600052604160045260246000fd5b600181811c908216806200029157607f821691505b602082108103620002b257634e487b7160e01b600052602260045260246000fd5b50919050565b601f8211156200030657600081815260208120601f850160051c81016020861015620002e15750805b601f850160051c820191505b818110156200030257828155600101620002ed565b5050505b505050565b81516001600160401b0381111562000327576200032762000266565b6200033f816200033884546200027c565b84620002b8565b602080601f8311600181146200037757600084156200035e5750858301515b600019600386901b1c1916600185901b17855562000302565b600085815260208120601f198616915b82811015620003a85788860151825594840194600190910190840162000387565b5085821015620003c75787850151600019600388901b60f8161c191681555b5050505050600190811b01905550565b634e487b7160e01b600052601160045260246000fd5b600181815b808511156200042e578160001904821115620004125762000412620003d7565b808516156200042057918102915b93841c9390800290620003f2565b509250929050565b6000826200044757506001620004e6565b816200045657506000620004e6565b81600181146200046f57600281146200047a576200049a565b6001915050620004e6565b60ff8411156200048e576200048e620003d7565b50506001821b620004e6565b5060208310610133831016604e8410600b8410161715620004bf575081810a620004e6565b620004cb8383620003ed565b8060001904821115620004e257620004e2620003d7565b0290505b92915050565b6000620004fd60ff84168362000436565b9392505050565b8082028115828204841417620004e657620004e6620003d7565b80820180821115620004e657620004e6620003d7565b61072080620005446000396000f3fe608060405234801561001057600080fd5b50600436106100935760003560e01c8063313ce56711610066578063313ce567146100fe57806370a082311461010d57806395d89b4114610136578063a9059cbb1461013e578063dd62ed3e1461015157600080fd5b806306fdde0314610098578063095ea7b3146100b657806318160ddd146100d957806323b872dd146100eb575b600080fd5b6100a061018a565b6040516100ad919061056a565b60405180910390f35b6100c96100c43660046105d4565b61021c565b60405190151581526020016100ad565b6002545b6040519081526020016100ad565b6100c96100f93660046105fe565b610236565b604051601281526020016100ad565b6100dd61011b36600461063a565b6001600160a01b031660009081526020819052604090205490565b6100a061025a565b6100c961014c3660046105d4565b610269565b6100dd61015f36600461065c565b6001600160a01b03918216600090815260016020908152604080832093909416825291909152205490565b6060600380546101999061068f565b80601f01602080910402602001604051908101604052809291908181526020018280546101c59061068f565b80156102125780601f106101e757610100808354040283529160200191610212565b820191906000526020600020905b8154815290600101906020018083116101f557829003601f168201915b5050505050905090565b60003361022a818585610277565b60019150505b92915050565b600033610244858285610289565b61024f85858561030c565b506001949350505050565b6060600480546101999061068f565b60003361022a81858561030c565b610284838383600161036b565b505050565b6001600160a01b03838116600090815260016020908152604080832093861683529290522054600019811461030657818110156102f757604051637dc7a0d960e11b81526001600160a01b038416600482015260248101829052604481018390526064015b60405180910390fd5b6103068484848403600061036b565b50505050565b6001600160a01b03831661033657604051634b637e8f60e11b8152600060048201526024016102ee565b6001600160a01b0382166103605760405163ec442f0560e01b8152600060048201526024016102ee565b610284838383610440565b6001600160a01b0384166103955760405163e602df0560e01b8152600060048201526024016102ee565b6001600160a01b0383166103bf57604051634a1406b160e11b8152600060048201526024016102ee565b6001600160a01b038085166000908152600160209081526040808320938716835292905220829055801561030657826001600160a01b0316846001600160a01b03167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b9258460405161043291815260200190565b60405180910390a350505050565b6001600160a01b03831661046b57806002600082825461046091906106c9565b909155506104dd9050565b6001600160a01b038316600090815260208190526040902054818110156104be5760405163391434e360e21b81526001600160a01b038516600482015260248101829052604481018390526064016102ee565b6001600160a01b03841660009081526020819052604090209082900390555b6001600160a01b0382166104f957600280548290039055610518565b6001600160a01b03821660009081526020819052604090208054820190555b816001600160a01b0316836001600160a01b03167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef8360405161055d91815260200190565b60405180910390a3505050565b600060208083528351808285015260005b818110156105975785810183015185820160400152820161057b565b506000604082860101526040601f19601f8301168501019250505092915050565b80356001600160a01b03811681146105cf57600080fd5b919050565b600080604083850312156105e757600080fd5b6105f0836105b8565b946020939093013593505050565b60008060006060848603121561061357600080fd5b61061c846105b8565b925061062a602085016105b8565b9150604084013590509250925092565b60006020828403121561064c57600080fd5b610655826105b8565b9392505050565b6000806040838503121561066f57600080fd5b610678836105b8565b9150610686602084016105b8565b90509250929050565b600181811c908216806106a357607f821691505b6020821081036106c357634e487b7160e01b600052602260045260246000fd5b50919050565b8082018082111561023057634e487b7160e01b600052601160045260246000fdfea26469706673582212205f76c64f52cc03750ccad66a28b62b7a5e979c8e92bba012a9bb346077ca52f164736f6c63430008130033";

    public DappTokenDeployment() : base(BYTECODE) { }
    public DappTokenDeployment(string byteCode) : base(byteCode) { }

    [Parameter("uint256", "_initialSupply", 1)]
    public BigInteger InitialSupply { get; set; }
}