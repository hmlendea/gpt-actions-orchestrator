using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

using Assembly = System.Reflection.Assembly;

namespace GptActionsOrchestrator.ArchitectureTests
{
    internal static class ProductionArchitecture
    {
        public static readonly Assembly[] Assemblies = [typeof(Program).Assembly];

        public static readonly Architecture Instance = new ArchLoader()
            .LoadAssemblies(Assemblies)
            .Build();
    }
}
