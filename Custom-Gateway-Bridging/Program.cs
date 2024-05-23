using System;
using Arbitrum.Utils;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static void Main(string[] args)
    {
        // Determine the base directory for the solution
        var baseDirectory = AppContext.BaseDirectory;

        // Build the path to the shared appsettings.json file
        var sharedSettingsPath = Path.Combine(baseDirectory, @"..\..\..\..\SharedSettings\appsettings.json");

        // Build configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(sharedSettingsPath, optional: true, reloadOnChange: true);

        IConfiguration configuration = builder.Build();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];
    }
}