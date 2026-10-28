using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace GptActionsOrchestrator.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyTests
    {
        [Test]
        public static void ProductionAssemblyInventoryExcludesTestAssemblies()
        {
            Assert.That(ProductionArchitecture.Assemblies, Has.Length.EqualTo(1));
            Assert.That(
                ProductionArchitecture.Assemblies.Select(assembly => assembly.GetName().Name),
                Is.EqualTo(new[] { "GptActionsOrchestrator" }));
        }

        [TestCase("GptActionsOrchestrator.Api.Controllers", "GptActionsOrchestrator.Integrations")]
        [TestCase("GptActionsOrchestrator.Api.Controllers", "GptActionsOrchestrator.DataAccess")]
        [TestCase("GptActionsOrchestrator.Integrations", "GptActionsOrchestrator.Api")]
        [TestCase("GptActionsOrchestrator.Integrations", "GptActionsOrchestrator.Service")]
        [TestCase("GptActionsOrchestrator.DataAccess", "GptActionsOrchestrator.Api")]
        [TestCase("GptActionsOrchestrator.DataAccess", "GptActionsOrchestrator.Service")]
        public static void ComponentsDoNotDependOnForbiddenComponents(
            string sourceNamespace,
            string targetNamespace)
        {
            Architecture architecture = ProductionArchitecture.Instance;
            Assert.That(
                Types().That().ResideInNamespaceOrBelow(sourceNamespace).GetObjects(architecture),
                Is.Not.Empty,
                $"Missing source component: {sourceNamespace}");
            Assert.That(
                Types().That().ResideInNamespaceOrBelow(targetNamespace).GetObjects(architecture),
                Is.Not.Empty,
                $"Missing target component: {targetNamespace}");

            Types().That().ResideInNamespaceOrBelow(sourceNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceOrBelow(targetNamespace))
                .Because($"{sourceNamespace} must not reference {targetNamespace}")
                .Check(architecture);
        }

        [TestCase("GitHub", "PersonalLogManager")]
        [TestCase("GitHub", "SteamStorefront")]
        [TestCase("PersonalLogManager", "GitHub")]
        [TestCase("PersonalLogManager", "SteamStorefront")]
        [TestCase("SteamStorefront", "GitHub")]
        [TestCase("SteamStorefront", "PersonalLogManager")]
        public static void IntegrationAdaptersRemainIndependent(string source, string target) =>
            ComponentsDoNotDependOnForbiddenComponents(
                $"GptActionsOrchestrator.Integrations.{source}",
                $"GptActionsOrchestrator.Integrations.{target}");

        [TestCase("GptActionsOrchestrator.Api", "Microsoft.EntityFrameworkCore")]
        [TestCase("GptActionsOrchestrator.Api", "Amazon.DynamoDBv2")]
        [TestCase("GptActionsOrchestrator.Api", "NuciDAL")]
        [TestCase("GptActionsOrchestrator.Integrations", "Microsoft.AspNetCore.Mvc")]
        [TestCase("GptActionsOrchestrator.Integrations", "Microsoft.AspNetCore.Http")]
        [TestCase("GptActionsOrchestrator.Integrations", "Microsoft.AspNetCore.Routing")]
        public static void BoundariesDoNotUseForbiddenFrameworks(
            string sourceNamespace,
            string forbiddenNamespace)
        {
            Architecture architecture = ProductionArchitecture.Instance;
            Assert.That(
                Types().That().ResideInNamespaceOrBelow(sourceNamespace).GetObjects(architecture),
                Is.Not.Empty,
                $"Missing component: {sourceNamespace}");

            // A forbidden framework may legitimately be absent from the assembly graph.
            Types().That().ResideInNamespaceOrBelow(sourceNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceOrBelow(forbiddenNamespace))
                .Because($"{sourceNamespace} must not use {forbiddenNamespace}")
                .Check(architecture);
        }
    }
}
