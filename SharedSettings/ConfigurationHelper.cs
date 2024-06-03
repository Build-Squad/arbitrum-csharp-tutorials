using Microsoft.Extensions.Configuration;
using System.Reflection;
namespace SharedSettings
{
    public class ConfigurationHelper
    {
        public static IConfiguration LoadConfiguration()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var sharedSettingsPath = Path.Combine(baseDirectory, @"..\..\..\..\SharedSettings\appsettings.json");
            var path = Path.GetFullPath(sharedSettingsPath);

            var builder = new ConfigurationBuilder()
                .SetBasePath(baseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(sharedSettingsPath, optional: true, reloadOnChange: true);

            return builder.Build();
        }
        public static (string Abi, string Bytecode) GetAbiAndBytecode(string contractName)
        {
            // Get the directory of the currently executing assembly
            string executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Navigate up to the solution directory (assuming this is located in a bin\Debug or bin\Release folder)
            string solutionDirectory = Directory.GetParent(executingDirectory).Parent.Parent.Parent.FullName;

            // Extract project name from the executing directory
            string projectName = new DirectoryInfo(executingDirectory).Parent.Parent.Parent.Name;

            // Construct the project path dynamically
            string projectPath = Path.Combine(solutionDirectory, "bin", projectName, "contracts");

            // Construct the paths to the ABI and bytecode files
            string abiFilePath = Path.Combine(projectPath, $"{contractName}.abi");
            string binFilePath = Path.Combine(projectPath, $"{contractName}.bin");

            if (!File.Exists(abiFilePath))
            {
                throw new FileNotFoundException($"The ABI file was not found: {abiFilePath}");
            }

            if (!File.Exists(binFilePath))
            {
                throw new FileNotFoundException($"The bytecode file was not found: {binFilePath}");
            }

            string abi = File.ReadAllText(abiFilePath);
            string bytecode = File.ReadAllText(binFilePath);

            return (abi, bytecode);
        }
    }
}
