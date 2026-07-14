using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NuciAPI.Controllers;
using NuciAPI.Responses;
using GptActionsOrchestrator.Api.Requests;
using GptActionsOrchestrator.Configuration;
using GptActionsOrchestrator.Service;
using GptActionsOrchestrator.Service.Models;

namespace GptActionsOrchestrator.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ActionsController(
        IActionsOrchestrator actionsOrchestrator,
        SecuritySettings securitySettings) : NuciApiController
    {
        readonly NuciApiAuthorisation authorisation = NuciApiAuthorisation.ApiKey(securitySettings.ApiKey);

        [HttpGet]
        public ActionResult Get([FromQuery] GetActionRequest request)
            => ProcessRequest(
                request,
                () => actionsOrchestrator.Get(Request.Query.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToString()
                )),
                authorisation);
    }
}
