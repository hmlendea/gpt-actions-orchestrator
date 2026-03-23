using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using GptActionsOrchestrator.Configuration;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Client;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Configuration;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models;
using GptActionsOrchestrator.Logging;
using NuciAPI.Client;
using NuciAPI.Responses;
using NuciLog.Core;

namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Service
{
    public sealed class PersonalLogManagerService(
        PersonalLogManagerSettings plmSettings,
        SecuritySettings securitySettings,
        ILogger logger) : IPersonalLogManagerService
    {
        readonly NuciApiClient client = new(plmSettings.BaseUrl);

        public PersonalLogs GetPersonalLogs(
            string dateBeginning,
            string dateEnd,
            string template,
            string localisation,
            Dictionary<string, string> data,
            string count)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Template, template),
                new(MyLogInfoKey.DateBeginning, dateBeginning),
                new(MyLogInfoKey.DateEnd, dateEnd),
                new(MyLogInfoKey.Localisation, localisation),
                new(MyLogInfoKey.Count, count)
            ];

            logger.Info(
                MyOperation.GetPersonalLogs,
                OperationStatus.Started,
                logInfos);

            try
            {
                PersonalLogs personalLogs = RetrievePersonalLogs(
                    dateBeginning,
                    dateEnd,
                    template,
                    localisation,
                    data,
                    count);

                logger.Debug(
                    MyOperation.GetPersonalLogs,
                    OperationStatus.Success,
                    logInfos);

                return personalLogs;
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.GetPersonalLogs,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

                throw;
            }
        }

        PersonalLogs RetrievePersonalLogs(
            string dateBeginning,
            string dateEnd,
            string template,
            string localisation,
            Dictionary<string, string> data,
            string count)
        {
            NuciApiRequestAuthorisationInfo authorisation = new()
            {
                ClientId = securitySettings.ClientId,
                BearerToken = plmSettings.ApiKey,
                HmacSharedSecretKey = plmSettings.HmacSigningKey
            };

            NuciApiResponse response =
                client.SendRequestAsync<GetPersonalLogsRequest, GetPersonalLogsResponse>(
                    HttpMethod.Get,
                    BuildRequest(dateBeginning, dateEnd, template, localisation, data, count),
                    authorisation,
                    "PersonalLog").Result;

            if (!response.IsSuccessful)
            {
                throw new Exception(response.Message);
            }

            return new()
            {
                Logs = ((GetPersonalLogsResponse)response).Logs
            };
        }

        GetPersonalLogsRequest BuildRequest(
            string dateBeginning,
            string dateEnd,
            string template,
            string localisation,
            Dictionary<string, string> data,
            string count)
        {
            GetPersonalLogsRequest request = new()
            {
                Date = BuildDateRangeRegex(dateBeginning, dateEnd),
                Template = template,
                Localisation = localisation,
                Data = data
            };

            if (string.IsNullOrWhiteSpace(localisation))
            {
                request.Localisation = "ro";
            }

            if (string.IsNullOrWhiteSpace(count))
            {
                request.Count = 1000;
            }
            else
            {
                request.Count = int.Parse(count);
            }

            return request;
        }

        public static string BuildDateRangeRegex(string dateBeginning, string dateEnd)
        {
            DateOnly start = DateOnly.ParseExact(dateBeginning, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateOnly end = DateOnly.ParseExact(dateEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (end < start)
            {
                throw new ArgumentException($"The end date must be greater than or equal to the beginning date.");
            }

            List<string> dates = [];

            DateOnly current = start;
            while (current <= end)
            {
                dates.Add(current.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                current = current.AddDays(1);
            }

            return "(" + string.Join("|", dates) + ")";
        }
    }
}
