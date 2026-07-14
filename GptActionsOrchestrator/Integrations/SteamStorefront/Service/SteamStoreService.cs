using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;
using GptActionsOrchestrator.Logging;

using NuciLog.Core;
using NuciWeb.HTTP;

namespace GptActionsOrchestrator.Integrations.SteamStorefront.Service
{
    public sealed class SteamStoreService(ILogger logger) : ISteamStoreService
    {
        private static string StorefrontApiUrl => "http://store.steampowered.com/api";
        private static string StorefrontApiCountry => "RO";
        private static string StorefrontApiFilters => "basic";
        private static string AppNamePattern => "\"name\": *\"([^\"]*)\"";

        private readonly HttpClient httpClient = HttpClientCreator.Create();

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

            SteamAppEntity steamAppEntity = null;

            try
            {
                string endpoint = $"{StorefrontApiUrl}/appdetails?appids={appId}&cc={StorefrontApiCountry}&filters={StorefrontApiFilters}";
                string responseContent = httpClient.GetStringAsync(endpoint).Result;

                steamAppEntity = new()
                {
                    Id = appId,
                    Name = Regex.Match(responseContent, AppNamePattern).Groups[1].Value
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

            return steamAppEntity;
        }
    }
}