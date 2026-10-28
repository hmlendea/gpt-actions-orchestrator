using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace GptActionsOrchestrator.ArchitectureTests
{
    [TestFixture]
    public sealed class ProjectConfigurationTests
    {
        [TestCaseSource(typeof(RepositorySources), nameof(RepositorySources.ProjectNames))]
        public static void ProjectsDeclareRequiredBuildSettings(string projectName)
        {
            XElement project = LoadProject(projectName);

            using (Assert.EnterMultipleScope())
            {
                AssertProperty(project, "TargetFramework", "net10.0");
                AssertProperty(project, "ImplicitUsings", "disable");
                AssertProperty(project, "LangVersion", "latest");
                AssertProperty(project, "Nullable", "enable");
                AssertProperty(project, "IsPackable", "false");

                if (!string.Equals(projectName, "GptActionsOrchestrator", StringComparison.Ordinal))
                {
                    AssertProperty(project, "IsTestProject", "true");
                    AssertProperty(project, "ExcludeFromCodeCoverage", "true");
                }

                string[] packages = project.Descendants("PackageReference")
                    .Select(reference => (string)reference.Attribute("Include")!).ToArray();
                Assert.That(packages, Is.Ordered.Using<string>(StringComparer.Ordinal), projectName);
            }
        }

        [Test]
        public static void SolutionContainsOnlyInventoriedProjectsInRequiredOrder()
        {
            XDocument solution = XDocument.Load(
                Path.Combine(RepositorySources.Root, "GptActionsOrchestrator.slnx"));
            Assert.That(solution.Declaration, Is.Null);
            Assert.That(solution.Root!.Name.LocalName, Is.EqualTo("Solution"));
            Assert.That(solution.Root.Attributes(), Is.Empty);
            string[] paths = solution.Root.Elements("Project")
                .Select(project => (string)project.Attribute("Path")!).ToArray();
            Assert.That(paths, Is.EqualTo(RepositorySources.ProjectNames
                .Select(projectName => $"{projectName}/{projectName}.csproj")));

            foreach (XElement project in solution.Root.Elements("Project"))
            {
                Assert.That(project.Attributes().Select(attribute => attribute.Name.LocalName),
                    Is.EqualTo(new[] { "Path" }));
            }
        }

        [Test]
        public static void ProductionProjectReferencesDoNotPointToTestsOrForbiddenLayers()
        {
            // The documented architecture is one production assembly, not separate layer projects.
            XElement project = LoadProject("GptActionsOrchestrator");
            Assert.That(project.Descendants("ProjectReference"), Is.Empty,
                "New production projects require an explicit assembly inventory and dependency matrix.");
            Assert.That(project.Descendants("Reference"), Is.Empty,
                "Direct assembly references require an explicit reviewed dependency allowance.");
            Assert.That(project.Descendants("InternalsVisibleTo"), Is.Empty);
            Assert.That(project.Descendants("AssemblyAttribute")
                .Select(attribute => (string?)attribute.Attribute("Include")),
                Does.Not.Contain("System.Runtime.CompilerServices.InternalsVisibleTo"));
            string[] packages = project.Descendants("PackageReference")
                .Select(reference => (string)reference.Attribute("Include")!).ToArray();
            Assert.That(packages, Is.Not.Empty);
            Assert.That(packages.Where(package =>
                package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                package.StartsWith("AWSSDK.DynamoDB", StringComparison.Ordinal) ||
                package.Contains("Test", StringComparison.OrdinalIgnoreCase)), Is.Empty,
                "The production host must not reference persistence frameworks or test infrastructure.");
        }

        private static XElement LoadProject(string projectName)
        {
            string path = Path.Combine(RepositorySources.Root, projectName, projectName + ".csproj");
            Assert.That(File.Exists(path), Is.True, $"Missing project file: {path}");

            return XElement.Load(path);
        }

        private static void AssertProperty(XElement project, string name, string expected) =>
            Assert.That(project.Descendants(name).Select(element => element.Value),
                Is.EqualTo(new[] { expected }), $"Required project property: {name}");
    }
}
