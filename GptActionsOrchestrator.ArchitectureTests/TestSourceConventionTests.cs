using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace GptActionsOrchestrator.ArchitectureTests
{
    [TestFixture]
    public sealed class TestSourceConventionTests
    {
        [TestCase("GptActionsOrchestrator.ArchitectureTests")]
        [TestCase("GptActionsOrchestrator.IntegrationTests")]
        [TestCase("GptActionsOrchestrator.UnitTests")]
        public static void TestClassesDeclareNUnitFixturesAndCases(string projectName)
        {
            ClassDeclarationSyntax[] classes = RepositorySources.Files(projectName)
                .SelectMany(path => RepositorySources.Parse(path).DescendantNodes()
                    .OfType<ClassDeclarationSyntax>())
                .Where(declaration => declaration.Identifier.ValueText.EndsWith(
                    "Tests", StringComparison.Ordinal)).ToArray();
            Assert.That(classes, Is.Not.Empty, $"No test fixtures found in {projectName}.");

            using (Assert.EnterMultipleScope())
            {
                foreach (ClassDeclarationSyntax declaration in classes)
                {
                    string path = declaration.SyntaxTree.FilePath;
                    Assert.That(declaration.AttributeLists.SelectMany(list => list.Attributes)
                        .Select(attribute => attribute.Name.ToString()), Does.Contain("TestFixture"), path);
                    Assert.That(declaration.Members.OfType<MethodDeclarationSyntax>()
                        .SelectMany(method => method.AttributeLists)
                        .SelectMany(list => list.Attributes)
                        .Any(attribute => attribute.Name.ToString() is
                            "Test" or "TestCase" or "TestCaseSource"), Is.True, path);
                }
            }
        }
    }
}
