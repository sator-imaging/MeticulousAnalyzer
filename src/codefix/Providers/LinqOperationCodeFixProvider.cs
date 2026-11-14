// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LinqOperationCodeFixProvider)), Shared]
    public sealed class LinqOperationCodeFixProvider : CodeFixProvider
    {
        private static readonly Type? FileScopedNamespaceType =
            typeof(CompilationUnitSyntax).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Syntax.FileScopedNamespaceDeclarationSyntax");
        private static readonly PropertyInfo? FileScopedNamespaceName =
            FileScopedNamespaceType?.GetProperty("Name");
        private static readonly SymbolDisplayFormat MethodSignatureFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                | SymbolDisplayGenericsOptions.IncludeVariance
                | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType
                | SymbolDisplayParameterOptions.IncludeName
                | SymbolDisplayParameterOptions.IncludeParamsRefOut
                | SymbolDisplayParameterOptions.IncludeExtensionThis
                | SymbolDisplayParameterOptions.IncludeDefaultValue,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(LinqOperationAnalyzer.RuleId_LinqOperation);

        public sealed override FixAllProvider? GetFixAllProvider() => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var usingDirective = FindUsingDirective(node);
                if (usingDirective == null) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.CodeFix_LinqOperation ?? "Create LinqPortal and remove using System.Linq",
                        cancellationToken => FixLinqOperationAsync(context.Document, usingDirective, cancellationToken),
                        nameof(LinqOperationCodeFixProvider)),
                    diagnostic);
            }
        }

        private static async Task<Solution> FixLinqOperationAsync(
            Document document,
            UsingDirectiveSyntax usingDirective,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root == null || semanticModel == null) return solution;

            var methods = new List<IMethodSymbol>();
            var portalNamespace = string.Empty;
            foreach (var node in root.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax invocation) continue;
                if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
                    || !IsSystemLinqNamespace(method.ContainingNamespace))
                {
                    continue;
                }

                if (methods.Count == 0) portalNamespace = GetNamespaceName(invocation);
                AddIfMissing(methods, (method.ReducedFrom ?? method).ConstructedFrom);
            }

            root = root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepExteriorTrivia) ?? root;
            solution = solution.WithDocumentSyntaxRoot(document.Id, root);

            if (methods.Count > 0)
            {
                var fileName = string.IsNullOrEmpty(portalNamespace) ? "LinqPortal.cs" : $"LinqPortal.{portalNamespace}.cs";
                var project = solution.GetProject(document.Project.Id)!;
                var portalDocument = FindPortalDocument(project, fileName);
                if (portalDocument == null)
                {
                    var filePath = project.FilePath == null
                        ? null
                        : Path.Combine(Path.GetDirectoryName(project.FilePath)!, fileName);
                    var source = GenerateEmptyPortal(portalNamespace);
                    var documentId = DocumentId.CreateNewId(project.Id, fileName);
                    solution = solution.AddDocument(
                        documentId,
                        fileName,
                        Microsoft.CodeAnalysis.Text.SourceText.From(source),
                        filePath: filePath);
                    portalDocument = solution.GetDocument(documentId)!;
                }

                var portalRoot = await portalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var portalModel = await portalDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (portalRoot == null) return solution;

                var portalClass = FindPortalClass(portalRoot);
                if (portalClass == null) return solution;
                var members = portalClass.Members;
                var existingMethods = new List<IMethodSymbol>();
                foreach (var member in members)
                {
                    if (member is MethodDeclarationSyntax declaration
                        && portalModel?.GetDeclaredSymbol(declaration) is IMethodSymbol existing)
                    {
                        existingMethods.Add(existing);
                    }
                }

                foreach (var method in methods)
                {
                    var exists = false;
                    for (int i = 0, count = existingMethods.Count; i < count; i++)
                    {
                        if (AreMethodsEquivalent(existingMethods[i], method))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) continue;

                    var declaration = SyntaxFactory.ParseMemberDeclaration(GenerateMethodForwarder(method));
                    if (declaration != null)
                    {
                        var leadingTrivia = members.Count == 0
                            ? SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("        "))
                            : SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(Environment.NewLine), SyntaxFactory.Whitespace("        "));
                        members = members.Add(declaration
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine)));
                    }
                }

                solution = solution.WithDocumentSyntaxRoot(
                    portalDocument.Id,
                    portalRoot.ReplaceNode(portalClass, portalClass.WithMembers(members)));
            }

            return solution;
        }

        private static void AddIfMissing(List<IMethodSymbol> methods, IMethodSymbol method)
        {
            for (int i = 0, count = methods.Count; i < count; i++)
            {
                if (AreMethodsEquivalent(methods[i], method)) return;
            }
            methods.Add(method);
        }

        private static bool AreMethodsEquivalent(IMethodSymbol left, IMethodSymbol right)
        {
            if (left.Name != right.Name
                || left.TypeParameters.Length != right.TypeParameters.Length
                || left.Parameters.Length != right.Parameters.Length)
            {
                return false;
            }

            for (int i = 0, count = left.Parameters.Length; i < count; i++)
            {
                if (left.Parameters[i].RefKind != right.Parameters[i].RefKind
                    || !SymbolEqualityComparer.Default.Equals(left.Parameters[i].Type, right.Parameters[i].Type))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsSystemLinqNamespace(INamespaceSymbol namespaceSymbol)
        {
            return namespaceSymbol.Name == "Linq"
                && namespaceSymbol.ContainingNamespace.Name == "System"
                && namespaceSymbol.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;
        }

        private static UsingDirectiveSyntax? FindUsingDirective(SyntaxNode node)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current is UsingDirectiveSyntax directive) return directive;
            }
            return null;
        }

        private static string GetNamespaceName(SyntaxNode node)
        {
            for (var current = node.Parent; current != null; current = current.Parent)
            {
                if (current is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    return GetName(namespaceDeclaration.Name);
                }

                if (FileScopedNamespaceType != null && FileScopedNamespaceType.IsInstanceOfType(current)
                    && FileScopedNamespaceName?.GetValue(current) is NameSyntax name)
                {
                    return GetName(name);
                }
            }
            return string.Empty;
        }

        private static string GetName(NameSyntax name)
        {
            if (name is IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.ValueText;
            }

            if (name is QualifiedNameSyntax qualifiedName)
            {
                return $"{GetName(qualifiedName.Left)}.{GetName(qualifiedName.Right)}";
            }

            return string.Empty;
        }

        private static Document? FindPortalDocument(Project project, string fileName)
        {
            foreach (var document in project.Documents)
            {
                if (string.Equals(document.Name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
            foreach (var document in project.Documents)
            {
                if (document.Name.StartsWith("LinqPortal.", StringComparison.OrdinalIgnoreCase)
                    && document.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
            return null;
        }

        private static ClassDeclarationSyntax? FindPortalClass(SyntaxNode root)
        {
            foreach (var node in root.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax declaration && declaration.Identifier.ValueText == "LinqPortal")
                {
                    return declaration;
                }
            }
            return null;
        }

        private static string GenerateEmptyPortal(string namespaceName)
        {
            var source = new StringBuilder();
            source.AppendLine("using System;");
            source.AppendLine("using System.Collections.Generic;");
            source.AppendLine();
            if (!string.IsNullOrEmpty(namespaceName))
            {
                source.AppendLine($"namespace {namespaceName}");
                source.AppendLine("{");
            }

            var indent = string.IsNullOrEmpty(namespaceName) ? string.Empty : "    ";
            source.AppendLine($"{indent}internal static partial class LinqPortal");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}}}");
            if (!string.IsNullOrEmpty(namespaceName)) source.AppendLine("}");
            return source.ToString();
        }

        private static string GenerateMethodForwarder(IMethodSymbol method)
        {
            var declaration = method.ToDisplayString(MethodSignatureFormat);
            var genericArguments = new StringBuilder();
            if (method.TypeParameters.Length > 0)
            {
                genericArguments.Append('<');
                for (int i = 0, count = method.TypeParameters.Length; i < count; i++)
                {
                    if (i > 0) genericArguments.Append(", ");
                    genericArguments.Append(method.TypeParameters[i].Name);
                }
                genericArguments.Append('>');
            }
            var arguments = new StringBuilder();
            for (int i = 0, count = method.Parameters.Length; i < count; i++)
            {
                if (i > 0) arguments.Append(", ");
                var parameter = method.Parameters[i];
                if (parameter.RefKind != RefKind.None)
                {
                    arguments.Append(parameter.RefKind.ToString().ToLowerInvariant()).Append(' ');
                }
                arguments.Append(parameter.Name);
            }

            var returnKeyword = method.ReturnsVoid ? string.Empty : "return ";
            var containingType = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"        {declaration}{Environment.NewLine}"
                + $"        {{{Environment.NewLine}"
                + $"            {returnKeyword}{containingType}.{method.Name}{genericArguments}({arguments});{Environment.NewLine}"
                + "        }";
        }
    }
}
