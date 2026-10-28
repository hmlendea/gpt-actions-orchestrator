using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace GptActionsOrchestrator.ArchitectureTests
{
    [TestFixture]
    public sealed class SourceFileConventionTests
    {
        [TestCaseSource(typeof(RepositorySources), nameof(RepositorySources.ProjectNames))]
        public static void HandwrittenFilesDeclareExactlyOneMatchingType(string projectName)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (string path in RepositorySources.Files(projectName))
                {
                    SyntaxNode root = RepositorySources.Parse(path);
                    SyntaxNode[] declarations = root.DescendantNodes()
                        .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
                        .ToArray();
                    Assert.That(declarations, Has.Length.EqualTo(1), path);

                    foreach (SyntaxNode declaration in declarations)
                    {
                        Assert.That(TypeName(declaration), Is.EqualTo(Path.GetFileNameWithoutExtension(path)), path);
                    }

                    Assert.That(root.DescendantNodes().OfType<GlobalStatementSyntax>(), Is.Empty, path);
                }
            }
        }

        [TestCaseSource(typeof(RepositorySources), nameof(RepositorySources.ProjectNames))]
        public static void NamespacesMatchProjectFoldersAndUseBlocks(string projectName)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (string path in RepositorySources.Files(projectName))
                {
                    SyntaxNode root = RepositorySources.Parse(path);
                    string relativeDirectory = Path.GetRelativePath(
                        Path.Combine(RepositorySources.Root, projectName),
                        Path.GetDirectoryName(path)!);
                    string expectedNamespace = projectName;

                    if (!string.Equals(relativeDirectory, ".", StringComparison.Ordinal))
                    {
                        expectedNamespace += "." + relativeDirectory.Replace(Path.DirectorySeparatorChar, '.');
                    }

                    BaseNamespaceDeclarationSyntax[] namespaces = root.DescendantNodes()
                        .OfType<BaseNamespaceDeclarationSyntax>().ToArray();
                    Assert.That(namespaces, Has.Length.EqualTo(1), path);

                    foreach (BaseNamespaceDeclarationSyntax declaration in namespaces)
                    {
                        Assert.That(declaration, Is.TypeOf<NamespaceDeclarationSyntax>(), path);
                        Assert.That(declaration.Name.ToString(), Is.EqualTo(expectedNamespace), path);
                    }

                    Assert.That(root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                        .Where(directive => directive.Parent is not CompilationUnitSyntax), Is.Empty, path);
                }
            }
        }

        [TestCaseSource(typeof(RepositorySources), nameof(RepositorySources.ProjectNames))]
        public static void SourcesAvoidProhibitedLanguageForms(string projectName)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (string path in RepositorySources.Files(projectName))
                {
                    SyntaxNode root = RepositorySources.Parse(path);
                    Assert.That(root.DescendantNodes().OfType<ConditionalExpressionSyntax>(), Is.Empty, path);
                    Assert.That(root.DescendantNodes().OfType<TupleTypeSyntax>(), Is.Empty, path);
                    Assert.That(root.DescendantNodes().OfType<TupleExpressionSyntax>(), Is.Empty, path);
                    Assert.That(root.DescendantNodes().OfType<ParameterSyntax>()
                        .Where(parameter => parameter.Default is not null), Is.Empty, path);
                    Assert.That(root.DescendantTokens().Where(token => token.IsKind(SyntaxKind.RefKeyword)),
                        Is.Empty, path);
                    Assert.That(root.DescendantNodes().OfType<AccessorDeclarationSyntax>()
                        .Where(accessor => accessor.IsKind(SyntaxKind.InitAccessorDeclaration)), Is.Empty, path);
                    Assert.That(root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                        .Where(expression => expression.IsKind(SyntaxKind.CoalesceExpression)), Is.Empty, path);
                    Assert.That(root.DescendantNodes().OfType<ThrowStatementSyntax>()
                        .Where(statement => statement.Expression is IdentifierNameSyntax &&
                            statement.Ancestors().OfType<CatchClauseSyntax>().Any(clause =>
                                string.Equals(clause.Declaration?.Identifier.ValueText,
                                    statement.Expression.ToString(), StringComparison.Ordinal))), Is.Empty, path);
                    CheckDirectives(root, path);
                }
            }
        }

        [TestCaseSource(typeof(RepositorySources), nameof(RepositorySources.ProjectNames))]
        public static void DeclarationsHaveExplicitAccessibilityAndLimitedParameters(string projectName)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (string path in RepositorySources.Files(projectName))
                {
                    SyntaxNode root = RepositorySources.Parse(path);

                    foreach (MemberDeclarationSyntax member in root.DescendantNodes()
                        .OfType<MemberDeclarationSyntax>().Where(RequiresAccessibility))
                    {
                        Assert.That(member.Modifiers.Any(IsAccessibility), Is.True,
                            $"{path}: {member.Kind()} requires explicit accessibility.");
                    }

                    foreach (ParameterListSyntax parameters in root.DescendantNodes()
                        .OfType<ParameterListSyntax>())
                    {
                        Assert.That(parameters.Parameters.Count, Is.LessThanOrEqualTo(7), path);
                    }
                }
            }
        }

        [TestCaseSource(typeof(RepositorySources), nameof(RepositorySources.ProjectNames))]
        public static void SourceWhitespaceUsesSpacesAndFinalNewlines(string projectName)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (string path in RepositorySources.Files(projectName))
                {
                    string text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
                    Assert.That(text.EndsWith('\n'), Is.True, $"{path}: Missing final newline.");
                    Assert.That(text.Contains('\t'), Is.False, $"{path}: Tab indentation is prohibited.");
                    Assert.That(text.Contains("\n\n\n", StringComparison.Ordinal), Is.False,
                        $"{path}: Consecutive blank lines are prohibited.");
                }
            }
        }

        private static string TypeName(SyntaxNode declaration) => declaration switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            DelegateDeclarationSyntax type => type.Identifier.ValueText,
            _ => throw new ArgumentException("Expected a type declaration.", nameof(declaration))
        };

        private static bool RequiresAccessibility(MemberDeclarationSyntax member) =>
            member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or
                BaseFieldDeclarationSyntax or BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax &&
            member is not DestructorDeclarationSyntax && member.Parent is not InterfaceDeclarationSyntax &&
            !(member is ConstructorDeclarationSyntax && member.Modifiers.Any(SyntaxKind.StaticKeyword));

        private static bool IsAccessibility(SyntaxToken token) =>
            token.IsKind(SyntaxKind.PublicKeyword) || token.IsKind(SyntaxKind.InternalKeyword) ||
            token.IsKind(SyntaxKind.ProtectedKeyword) || token.IsKind(SyntaxKind.PrivateKeyword);

        private static void CheckDirectives(SyntaxNode root, string path)
        {
            IEnumerable<SyntaxTrivia> trivia = root.DescendantTrivia(descendIntoTrivia: true);
            Assert.That(trivia.Where(item => item.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
                item.IsKind(SyntaxKind.EndRegionDirectiveTrivia)), Is.Empty, path);
            Assert.That(trivia.Select(item => item.GetStructure()).OfType<NullableDirectiveTriviaSyntax>()
                .Where(directive => directive.SettingToken.IsKind(SyntaxKind.EnableKeyword)), Is.Empty, path);
            Assert.That(trivia.Where(item => item.IsKind(SyntaxKind.DisabledTextTrivia)), Is.Empty,
                $"{path}: Inactive source requires an explicit configuration inventory for convention analysis.");
        }
    }
}
