using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;

namespace GptActionsOrchestrator.Integrations.SteamStorefront.Service
{
    /// <summary>
    /// Provides methods for interacting with the Steam Storefront API.
    /// </summary>
    public interface ISteamStoreService
    {
        /// <summary>
        /// Retrieves Steam app data by its ID.
        /// </summary>
        /// <param name="appId">The Steam app ID.</param>
        /// <returns>The Steam app entity.</returns>
        SteamAppEntity GetAppData(string appId);
    }
}
