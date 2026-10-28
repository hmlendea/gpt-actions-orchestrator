using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;

using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using GptActionsOrchestrator.Configuration;
using GptActionsOrchestrator.DataAccess.DataObjects;
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
        public static IServiceCollection AddConfigurations(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            SecuritySettings securitySettings = new();
            DataStoreSettings dataStoreSettings = new();
            NuciLoggerSettings loggingSettings = new();

            GitHubSettings gitHubSettings = new();
            PersonalLogManagerSettings personalLogManagerSettings = new();

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
            .AddSingleton<IFileRepository<GptActionAliasDataObject>>(CreateAliasesRepository)
            .AddSingleton<IActionsOrchestrator, ActionsOrchestrator>()
            .AddSingleton<IGitHubService, GitHubService>()
            .AddSingleton<IPersonalLogManagerService, PersonalLogManagerService>()
            .AddSingleton<ISteamStoreService, SteamStoreService>()
            .AddSingleton<ILogger, NuciLogger>();

        private static JsonRepository<GptActionAliasDataObject> CreateAliasesRepository(
            IServiceProvider serviceProvider)
            => new(serviceProvider.GetRequiredService<DataStoreSettings>().GptActionAliasesStorePath);
    }
}
