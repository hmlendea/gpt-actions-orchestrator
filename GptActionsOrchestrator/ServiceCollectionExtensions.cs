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
using NuciDAL.Repositories;
using GptActionsOrchestrator.DataAccess.DataObjects;

namespace GptActionsOrchestrator
{
    public static class ServiceCollectionExtensions
    {
        private static SecuritySettings securitySettings;
        private static DataStoreSettings dataStoreSettings;
        private static NuciLoggerSettings loggingSettings;

        private static GitHubSettings gitHubSettings;
        private static PersonalLogManagerSettings personalLogManagerSettings;

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            securitySettings = new SecuritySettings();
            dataStoreSettings = new DataStoreSettings();
            loggingSettings = new NuciLoggerSettings();

            gitHubSettings = new GitHubSettings();
            personalLogManagerSettings = new PersonalLogManagerSettings();

            configuration.Bind(nameof(SecuritySettings), securitySettings);
            configuration.Bind(nameof(DataStoreSettings), dataStoreSettings);
            configuration.Bind(nameof(NuciLoggerSettings), loggingSettings);

            configuration.Bind(nameof(GitHubSettings), gitHubSettings);
            configuration.Bind(nameof(PersonalLogManagerSettings), personalLogManagerSettings);

            services.AddSingleton(securitySettings);
            services.AddSingleton(dataStoreSettings);
            services.AddSingleton(loggingSettings);

            services.AddSingleton(gitHubSettings);
            services.AddSingleton(personalLogManagerSettings);

            return services;
        }

        public static IServiceCollection AddCustomServices(this IServiceCollection services) => services
            .AddSingleton<IFileRepository<GptActionAliasDataObject>>(x => new JsonRepository<GptActionAliasDataObject>(dataStoreSettings.GptActionAliasesStorePath))
            .AddSingleton<IActionsOrchestrator, ActionsOrchestrator>()
            .AddSingleton<IGitHubService, GitHubService>()
            .AddSingleton<IPersonalLogManagerService, PersonalLogManagerService>()
            .AddSingleton<ISteamStoreService, SteamStoreService>()
            .AddSingleton<ILogger, NuciLogger>();
    }
}
