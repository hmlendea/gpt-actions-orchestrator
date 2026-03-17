using System;
using System.Collections.Generic;
using GptActionsOrchestrator.Api.Responses;
using GptActionsOrchestrator.Integrations.GitHub.Service;
using GptActionsOrchestrator.Integrations.PersonalLogManager.Service;
using GptActionsOrchestrator.Integrations.SteamStorefront.Service;
using GptActionsOrchestrator.Service.Models;

namespace GptActionsOrchestrator.Service
{
    public sealed class ActionsOrchestrator(
        IGitHubService gitHubService,
        IPersonalLogManagerService personalLogManagerService,
        ISteamStoreService steamStoreService) : IActionsOrchestrator
    {
        public GetActionResponse Get(Dictionary<string, string> rawParameters)
        {
            Dictionary<string, object> parameters = BuildParameters(rawParameters);
            GptAction action = GetGptActionFromParameters(rawParameters);
            object data;

            if (action == GptAction.GetGitHubRepositoryFile)
            {
                data = gitHubService.GetRepositoryFile(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"),
                    GetParameter<string>(parameters, "path"));
            }
            else if (action == GptAction.GetGitHubRepositoryReadme)
            {
                data = gitHubService.GetRepositoryFile(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"),
                    "README.md");
            }
            else if (action == GptAction.GetGitHubRepositoryReleases)
            {
                data = gitHubService.GetRepositoryReleases(
                    GetParameter<string>(parameters, "username"),
                    GetParameter<string>(parameters, "repository"));
            }
            else if (action == GptAction.GetGitHubUserRepositories)
            {
                data = gitHubService.GetUserRepositories(
                    GetParameter<string>(parameters, "username"));
            }
            else if (action == GptAction.GetPersonalLogs)
            {
                data = personalLogManagerService.GetPersonalLogs(
                    GetParameter<string>(parameters, "date_beginning"),
                    GetParameter<string>(parameters, "date_end"),
                    GetParameter<string>(parameters, "template"),
                    GetParameter<string>(parameters, "localisation"),
                    GetParameter<Dictionary<string, string>>(parameters, "data"),
                    GetParameter<string>(parameters, "count"));
            }
            else if (action == GptAction.GetSteamAppData)
            {
                data = steamStoreService.GetAppData(GetParameter<string>(parameters, "appId"));
            }
            else
            {
                throw new NotImplementedException($"Action '{action.Name}' is not implemented.");
            }

            return new GetActionResponse
            {
                GptActionName = action.ToString(),
                Data = data
            };
        }

        TObject GetParameter<TObject>(Dictionary<string, object> parameters, string key)
        {
            if (parameters.TryGetValue(key, out object value) && value is TObject typedValue)
            {
                return typedValue;
            }

            return default;
        }

        Dictionary<string, object> BuildParameters(Dictionary<string, string> rawParameters)
        {
            Dictionary<string, object> parameters = [];

            foreach (var pair in rawParameters)
            {
                if (pair.Key.Contains('.'))
                {
                    string[] parts = pair.Key.Split('.', 2);
                    string parentKey = parts[0];
                    string childKey = parts[1];

                    if (!parameters.ContainsKey(parentKey))
                    {
                        parameters[parentKey] = new Dictionary<string, string>();
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

        GptAction GetGptActionFromParameters(Dictionary<string, string> parameters)
        {
            if (parameters.TryGetValue("action", out string actionName))
            {
                return GptAction.FromString(actionName);
            }

            return GptAction.Unknown;
        }
    }
}
