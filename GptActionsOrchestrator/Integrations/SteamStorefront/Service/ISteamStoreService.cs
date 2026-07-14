using GptActionsOrchestrator.Integrations.SteamStorefront.Service.Models;

namespace GptActionsOrchestrator.Integrations.SteamStorefront.Service
{
    public interface ISteamStoreService
    {
        public SteamAppEntity GetAppData(string appId);
    }
}
