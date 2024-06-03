using System;
using System.Numerics;
using System.Threading.Tasks;
using Arbitrum.Utils;
using Microsoft.Extensions.Configuration;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Nethereum.Util;
using Nethereum.Web3;
using static Arbitrum.DataEntities.NetworkUtils;
using Nethereum.Web3.Accounts;
using SharedSettings;
using Arbitrum.DataEntities;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];

        // Initial setup
        var baseL2Provider = new Web3(new RpcClient(new Uri(l2Rpc)));

        // Values of the transaction to estimate
        var destinationAddress = "0x1234563d5de0d7198451f87bcbf15aefd00d434d";
        var txData = "0x";

        // Add the default local network configuration to the SDK
        // to allow this script to run on a local node
        AddDefaultLocalNetwork();

        // Instantiation of the NodeInterface object
        Contract nodeInterface = await LoadContractUtils.LoadContract("NodeInterface", baseL2Provider, Constants.NODE_INTERFACE_ADDRESS, false);

        // Getting the estimations from NodeInterface.GasEstimateComponents()
        // ------------------------------------------------------------------
        var gasEstimateComponents = await nodeInterface.GetFunction("gasEstimateComponents").CallAsync<dynamic>(
            destinationAddress,
            false,
            txData
        );

        // Getting useful values for calculating the formula
        var l1GasEstimated = gasEstimateComponents.GasEstimateForL1;
        var l2GasUsed = gasEstimateComponents.GasEstimate - gasEstimateComponents.GasEstimateForL1;
        var l2EstimatedPrice = gasEstimateComponents.BaseFee;
        var l1EstimatedPrice = gasEstimateComponents.L1BaseFeeEstimate * 16;

        // Calculating some extra values to be able to apply all variables of the formula
        // -------------------------------------------------------------------------------
        // NOTE: This one might be a bit confusing, but l1GasEstimated (B in the formula) is calculated based on l2 gas fees
        var l1Cost = l1GasEstimated * l2EstimatedPrice;
        // NOTE: This is similar to 140 + utils.hexDataLength(txData);
        var l1Size = l1Cost / l1EstimatedPrice;

        // Getting the result of the formula
        // ---------------------------------
        // Setting the basic variables of the formula
        var P = l2EstimatedPrice;
        var L2G = l2GasUsed;
        var L1P = l1EstimatedPrice;
        var L1S = l1Size;

        // L1C (L1 Cost) = L1P * L1S
        var L1C = L1P * L1S;

        // B (Extra Buffer) = L1C / P
        var B = L1C / P;

        // G (Gas Limit) = L2G + B
        var G = L2G + B;

        // TXFEES (Transaction fees) = P * G
        var TXFEES = P * G;

        Console.WriteLine("Gas estimation components");
        Console.WriteLine("-------------------");
        Console.WriteLine($"Full gas estimation = {gasEstimateComponents.GasEstimate} units");
        Console.WriteLine($"L2 Gas (L2G) = {L2G} units");
        Console.WriteLine($"L1 estimated Gas (L1G) = {l1GasEstimated} units");

        Console.WriteLine($"P (L2 Gas Price) = {Web3.Convert.FromWei(P, UnitConversion.EthUnit.Gwei)} gwei");
        Console.WriteLine($"L1P (L1 estimated calldata price per byte) = {Web3.Convert.FromWei(L1P, UnitConversion.EthUnit.Gwei)} gwei");
        Console.WriteLine($"L1S (L1 Calldata size in bytes) = {L1S} bytes");

        Console.WriteLine("-------------------");
        Console.WriteLine($"Transaction estimated fees to pay = {Web3.Convert.FromWei(TXFEES, UnitConversion.EthUnit.Ether)} ETH");
    }
}
