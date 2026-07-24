using NuciDAL.DataObjects;

namespace GptActionsOrchestrator.DataAccess.DataObjects
{
    public sealed class GptActionAliasDataObject : EntityBase
    {
        public string TargetActionId { get; set; }
    }
}
