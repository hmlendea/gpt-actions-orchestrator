using System.Collections.Generic;
using NuciAPI.Requests;
using NuciSecurity.HMAC;

namespace GptActionsOrchestrator.Integrations.PersonalLogManager.Client
{
    public class GetPersonalLogsRequest : NuciApiRequest
    {
        [HmacOrder(1)]
        public string Date { get; set; }

        [HmacOrder(2)]
        public string Time { get; set; }

        [HmacOrder(3)]
        public string Template { get; set; }

        [HmacOrder(4)]
        public string Localisation { get; set; }

        [HmacOrder(5)]
        public Dictionary<string, string> Data { get; set; }

        [HmacOrder(6)]
        public int Count { get; set; }
    }
}
