using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

using NuciLog.Core;

using NuciWeb.HTTP;

using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;
using GptActionsOrchestrator.Logging;

namespace GptActionsOrchestrator.Integrations.SteamStorefront.Service
{
    public sealed partial class SteamStoreService(ILogger logger) : ISteamStoreService
    {
        private readonly HttpClient httpClient = HttpClientCreator.Create();

        private static string StorefrontApiUrl => "http://store.steampowered.com/api";

        private static string StorefrontApiCountry => "RO";

        private static string StorefrontApiFilters => "basic";

        public SteamAppEntity GetAppData(string appId)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.AppId, appId)
            ];

            logger.Info(
                MyOperation.SteamStoreAppDataRetrieval,
                OperationStatus.Started,
                logInfos);

            SteamAppEntity? steamAppEntity = null;

            try
            {
                string endpoint = $"{StorefrontApiUrl}/appdetails?appids={appId}&cc={StorefrontApiCountry}&filters={StorefrontApiFilters}";
                string responseContent = httpClient.GetStringAsync(endpoint).Result;

                steamAppEntity = new()
                {
                    Id = appId,
                    Name = AppNameRegex().Match(responseContent).Groups[1].Value
                };
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.SteamStoreAppDataRetrieval,
                    OperationStatus.Failure,
                    exception,
                    logInfos);
            }

            logger.Debug(
                MyOperation.SteamStoreAppDataRetrieval,
                OperationStatus.Success,
                logInfos);

            return steamAppEntity!;
        }

        [GeneratedRegex("\"name\": *\"([^\"]*)\"")]
        private static partial Regex AppNameRegex();
    }
}
