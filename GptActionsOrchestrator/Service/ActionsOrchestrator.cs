using System;
using System.Collections.Generic;

using NuciDAL.Repositories;

using GptActionsOrchestrator.Api.Responses;
using GptActionsOrchestrator.DataAccess.DataObjects;
using GptActionsOrchestrator.Integrations.GitHub.Service;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service;
using GptActionsOrchestrator.Service.Models;

namespace GptActionsOrchestrator.Service
{
    public sealed class ActionsOrchestrator(
        IGitHubService gitHubService,
        IPersonalLogManagerService personalLogManagerService,
        ISteamStoreService steamStoreService,
        IFileRepository<GptActionAliasDataObject> aliasesRepository) : IActionsOrchestrator
    {
        public GetActionResponse Get(Dictionary<string, string> rawParameters)
        {
            Dictionary<string, object> parameters = BuildParameters(rawParameters);
            GptAction action = GetGptActionFromParameters(rawParameters);

            return new()
            {
                GptActionName = action.ToString(),
                Data = ExecuteAction(action, parameters)
            };
        }

        private object ExecuteAction(GptAction action, Dictionary<string, object> parameters)
        {
            if (Equals(action, GptAction.GetPersonalLogs))
            {
                return personalLogManagerService.GetPersonalLogs(
                    GetParameter<string>(parameters, "date_beginning"),
                    GetParameter<string>(parameters, "date_end"),
                    GetParameter<string>(parameters, "template"),
                    GetParameter<string>(parameters, "localisation"),
                    GetParameter<Dictionary<string, string>>(parameters, "data"),
                    GetParameter<string>(parameters, "count"));
            }

            if (Equals(action, GptAction.GetSteamAppData))
            {
                return steamStoreService.GetAppData(GetParameter<string>(parameters, "appId"));
            }

            return ExecuteGitHubAction(action, parameters);
        }

        private object ExecuteGitHubAction(GptAction action, Dictionary<string, object> parameters)
        {
            if (Equals(action, GptAction.GetGitHubRepository))
            {
                return gitHubService.GetRepository(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"));
            }
            if (Equals(action, GptAction.GetGitHubRepositoryFile))
            {
                return gitHubService.GetRepositoryFile(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"),
                    GetParameter<string>(parameters, "path"));
            }
            if (Equals(action, GptAction.GetGitHubRepositoryReadme))
            {
                return gitHubService.GetRepositoryFile(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"),
                    "README.md");
            }
            if (Equals(action, GptAction.GetGitHubRepositoryReleases))
            {
                return gitHubService.GetRepositoryReleases(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"));
            }
            if (Equals(action, GptAction.GetGitHubUserRepositories))
            {
                return gitHubService.GetUserRepositories(
                    GetParameter<string>(parameters, "username"));
            }

            throw new NotImplementedException($"The '{action.Id}' action is not supported.");
        }

        private static TObject GetParameter<TObject>(Dictionary<string, object> parameters, string key)
            where TObject : class
        {
            if (parameters.TryGetValue(key, out object? value) && value is TObject typedValue)
            {
                return typedValue;
            }

            return null!;
        }

        private static Dictionary<string, object> BuildParameters(Dictionary<string, string> rawParameters)
        {
            Dictionary<string, object> parameters = [];

            foreach (KeyValuePair<string, string> pair in rawParameters)
            {
                if (pair.Key.Contains('.'))
                {
                    string[] parts = pair.Key.Split('.', 2);
                    string parentKey = parts[0];
                    string childKey = parts[1];

                    if (!parameters.ContainsKey(parentKey))
                    {
                        Dictionary<string, string> childParameters = [];
                        parameters[parentKey] = childParameters;
                    }

                    ((Dictionary<string, string>)parameters[parentKey])[childKey] = pair.Value;
                }
                else
                {
                    parameters[pair.Key] = pair.Value;
                }
            }

            return parameters;
        }

        private GptAction GetGptActionFromParameters(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("action", out string? gptActionId))
            {
                return GptAction.Unknown;
            }

            if (aliasesRepository.ContainsId(gptActionId))
            {
                gptActionId = aliasesRepository.Get(gptActionId).TargetActionId;
            }

            return GptAction.FromString(gptActionId);
        }
    }
}
