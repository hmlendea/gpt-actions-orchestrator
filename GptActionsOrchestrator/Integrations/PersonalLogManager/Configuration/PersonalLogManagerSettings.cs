namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Configuration
{
    public sealed class PersonalLogManagerSettings
    {
        public string BaseUrl { get; set; }

        public string ApiKey { get; set; }

        public string HmacSigningKey { get; set; }
    }
}
