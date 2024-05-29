using System;
using SharedSettings;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static void Main(string[] args)
    {
        var configuration = ConfigurationHelper.LoadConfiguration();

        // Read values from appsettings.json
        var devnetPrivKey = configuration["DevelopmentSettings:DEVNET_PRIVKEY"];
        var l1Rpc = configuration["DevelopmentSettings:L1RPC"];
        var l2Rpc = configuration["DevelopmentSettings:L2RPC"];
    }
}