using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using GptActionsOrchestrator.Configuration;
using GptActionsOrchestrator.Integrations.GitHub.Configuration;
using GptActionsOrchestrator.Integrations.GitHub.Service;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Configuration;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service;
using GptActionsOrchestrator.Service;

namespace GptActionsOrchestrator
{
    public static class ServiceCollectionExtensions
    {
        private static SecuritySettings securitySettings;
        private static NuciLoggerSettings loggingSettings;

        private static GitHubSettings gitHubSettings;
        private static PersonalLogManagerSettings personalLogManagerSettings;

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            securitySettings = new SecuritySettings();
            loggingSettings = new NuciLoggerSettings();

            gitHubSettings = new GitHubSettings();
            personalLogManagerSettings = new PersonalLogManagerSettings();

            configuration.Bind(nameof(SecuritySettings), securitySettings);
            configuration.Bind(nameof(NuciLoggerSettings), loggingSettings);

            configuration.Bind(nameof(GitHubSettings), gitHubSettings);
            configuration.Bind(nameof(PersonalLogManagerSettings), personalLogManagerSettings);

            services.AddSingleton(securitySettings);
            services.AddSingleton(loggingSettings);

            services.AddSingleton(gitHubSettings);
            services.AddSingleton(personalLogManagerSettings);

            return services;
        }

        public static IServiceCollection AddCustomServices(this IServiceCollection services) => services
            .AddSingleton<IActionsOrchestrator, ActionsOrchestrator>()
            .AddSingleton<IGitHubService, GitHubService>()
            .AddSingleton<IPersonalLogManagerService, PersonalLogManagerService>()
            .AddSingleton<ISteamStoreService, SteamStoreService>()
            .AddSingleton<ILogger, NuciLogger>();
    }
}
