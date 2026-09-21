using System.Collections.Generic;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Moq;

using NuciLog.Core;

using GptActionsOrchestrator.Integrations.GitHub.Service;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service;

namespace GptActionsOrchestrator.IntegrationTests.Infrastructure
{
    internal sealed class ActionsWebApplicationFactory : WebApplicationFactory<Program>
    {
        internal Mock<IGitHubService> GitHubServiceMock { get; } = new(MockBehavior.Strict);

        internal Mock<IPersonalLogManagerService> PersonalLogManagerServiceMock { get; } = new(MockBehavior.Strict);

        internal Mock<ISteamStoreService> SteamStoreServiceMock { get; } = new(MockBehavior.Strict);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration((webHostBuilderContext, configurationBuilder) =>
            {
                Dictionary<string, string?> configurationValues = new()
                {
                    ["SecuritySettings:ApiKey"] = IntegrationTestSettings.ApiKey,
                    ["NuciLoggerSettings:IsFileOutputEnabled"] = bool.FalseString
                };

                configurationBuilder.AddInMemoryCollection(configurationValues);
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGitHubService>();
                services.RemoveAll<IPersonalLogManagerService>();
                services.RemoveAll<ISteamStoreService>();
                services.RemoveAll<ILogger>();

                services.AddSingleton(GitHubServiceMock.Object);
                services.AddSingleton(PersonalLogManagerServiceMock.Object);
                services.AddSingleton(SteamStoreServiceMock.Object);
                services.AddSingleton(Mock.Of<ILogger>());
            });
        }
    }
}