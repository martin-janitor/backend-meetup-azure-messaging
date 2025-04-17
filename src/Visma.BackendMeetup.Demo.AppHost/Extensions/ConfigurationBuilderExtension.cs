using Microsoft.Extensions.Configuration;

namespace Visma.BackendMeetup.Demo.AppHost.Extensions
{
    public static class ConfigurationBuilderExtension
    {
        private const string LocalSettingJsonFile = "appsettings.json";

        public static IConfigurationBuilder BuildAppConfiguration(
            this IConfigurationBuilder config)
        {
            var currentConfiguration = config.Build();

            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile(LocalSettingJsonFile, true, true);
            config.AddEnvironmentVariables();
            config.Build();

            return config;
        }
    }
}
