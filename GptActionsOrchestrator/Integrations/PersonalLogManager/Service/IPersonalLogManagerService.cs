using System.Collections.Generic;

using GptActionsOrchestrator.Integrations.PersonalLogManager.Service.Models;

namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Service
{
    /// <summary>
    /// Provides methods for interacting with the Personal Log Manager API.
    /// </summary>
    public interface IPersonalLogManagerService
    {
        /// <summary>
        /// Retrieves personal logs based on the specified criteria.
        /// </summary>
        /// <param name="dateBeginning">The beginning date for filtering logs (yyyy-MM-dd).</param>
        /// <param name="dateEnd">The end date for filtering logs (yyyy-MM-dd).</param>
        /// <param name="template">The template to use for formatting logs.</param>
        /// <param name="localisation">The localisation for the logs.</param>
        /// <param name="data">Additional data to include in the logs.</param>
        /// <param name="count">The maximum number of logs to retrieve.</param>
        /// <returns>A collection of personal logs.</returns>
        PersonalLogs GetPersonalLogs(
            string dateBeginning,
            string dateEnd,
            string template,
            string localisation,
            Dictionary<string, string> data,
            string count);
    }
}
