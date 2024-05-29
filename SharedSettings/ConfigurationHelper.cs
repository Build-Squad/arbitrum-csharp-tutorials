using Microsoft.Extensions.Configuration;
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
    }
}
