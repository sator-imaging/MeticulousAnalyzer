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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LinqOperationCodeFixProvider)), Shared]
    public sealed class LinqOperationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(LinqOperationAnalyzer.RuleId_LinqOperation);
        }

        public sealed override FixAllProvider? GetFixAllProvider()
        {
            return null;
        }

        // Required because FileScopedNamespaceDeclarationSyntax was introduced in C# 10 / Roslyn 4.0,
        // but this project references Roslyn 3.8.0 compile-time abstractions.
        private const string FileScopedNamespaceDeclarationSyntaxName = "FileScopedNamespaceDeclarationSyntax";

        private static readonly SymbolDisplayFormat MethodSignatureFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeExtensionThis,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var usingDirective = node?.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().FirstOrDefault();
                if (usingDirective == null) continue;

                var title = CodeFixResources.CodeFix_LinqOperation ?? "Create LinqPortal and remove using System.Linq";

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => FixLinqOperationAsync(context.Document, usingDirective, c),
                        equivalenceKey: nameof(LinqOperationCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Solution> FixLinqOperationAsync(
            Document document,
            UsingDirectiveSyntax usingDirective,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var solution = project.Solution;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (semanticModel == null || root == null) return solution;

            // Collect Linq methods used in the document
            var linqMethods = new List<IMethodSymbol>();

            foreach (var node in root.DescendantNodes())
            {
                if (node is InvocationExpressionSyntax inv)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(inv, cancellationToken);
                    var method = symbolInfo.Symbol as IMethodSymbol;
                    if (method == null && symbolInfo.CandidateSymbols.Length > 0)
                    {
                        method = symbolInfo.CandidateSymbols[0] as IMethodSymbol;
                    }

                    if (method != null && IsSystemLinqEnumerableMethod(method))
                    {
                        var reducedFrom = method.ReducedFrom ?? method;
                        var constructedFrom = reducedFrom.ConstructedFrom;
                        if (!ContainsMethod(linqMethods, constructedFrom))
                        {
                            linqMethods.Add(constructedFrom);
                        }
                    }
                }
            }

            var topLevelNamespace = GetTopLevelNamespace(root);
            var projectNamespace = !string.IsNullOrEmpty(project.DefaultNamespace)
                ? project.DefaultNamespace!
                : topLevelNamespace;

            // Delete using System.Linq; ONLY
            var newRoot = root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);

            // Output file: LinqPortal.<project namespace from csproj>.cs (no .g.cs)
            var portalFileName = !string.IsNullOrEmpty(projectNamespace)
                ? $"LinqPortal.{projectNamespace}.cs"
                : (string.IsNullOrEmpty(topLevelNamespace) ? "LinqPortal.cs" : $"LinqPortal.{topLevelNamespace}.cs");

            var currentProject = solution.GetProject(project.Id)!;
            var portalDoc = currentProject.Documents.FirstOrDefault(d => string.Equals(d.Name, portalFileName, StringComparison.OrdinalIgnoreCase))
                ?? currentProject.Documents.FirstOrDefault(d => d.Name != null && d.Name.StartsWith("LinqPortal.", StringComparison.OrdinalIgnoreCase) && d.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

            // 1. Generate empty placeholder if file is not found
            if (portalDoc == null)
            {
                var emptyPlaceholder = GenerateEmptyPlaceholder(topLevelNamespace);
                var projectDir = !string.IsNullOrEmpty(project.FilePath) ? Path.GetDirectoryName(project.FilePath) : null;
                var filePath = projectDir != null ? Path.Combine(projectDir, portalFileName) : null;

                var docId = DocumentId.CreateNewId(project.Id, portalFileName);
                solution = solution.AddDocument(
                    docId,
                    portalFileName,
                    Microsoft.CodeAnalysis.Text.SourceText.From(emptyPlaceholder),
                    filePath: filePath);

                currentProject = solution.GetProject(project.Id)!;
                portalDoc = currentProject.GetDocument(docId)!;
            }

            // 2. Collect existing symbols and their body, and place them with new methods in alphabetical order
            var portalRoot = await portalDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            var portalModel = await portalDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (portalRoot != null)
            {
                var updatedPortalRoot = UpdatePortalClass(portalRoot, portalModel, topLevelNamespace, linqMethods);
                solution = solution.WithDocumentSyntaxRoot(portalDoc.Id, updatedPortalRoot);
            }

            return solution;
        }

        private static string GenerateEmptyPlaceholder(string topLevelNamespace)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            bool hasNamespace = !string.IsNullOrEmpty(topLevelNamespace);
            if (hasNamespace)
            {
                sb.AppendLine($"namespace {topLevelNamespace}");
                sb.AppendLine("{");
            }

            string indent = hasNamespace ? "    " : "";
            sb.AppendLine($"{indent}public static partial class LinqPortal");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}}}");

            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static SyntaxNode UpdatePortalClass(
            SyntaxNode portalRoot,
            SemanticModel? portalModel,
            string topLevelNamespace,
            List<IMethodSymbol> linqMethods)
        {
            ClassDeclarationSyntax? classDecl = null;
            foreach (var node in portalRoot.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax cls && cls.Identifier.Text == "LinqPortal")
                {
                    classDecl = cls;
                    break;
                }
            }

            if (classDecl == null) return portalRoot;

            bool hasNamespace = !string.IsNullOrEmpty(topLevelNamespace);
            string indent = hasNamespace ? "        " : "    ";

            // Existing methods
            var existingMethods = new List<(string Name, int ParamCount)>();
            foreach (var member in classDecl.Members)
            {
                if (member is MethodDeclarationSyntax methodSyntax)
                {
                    var symbol = portalModel?.GetDeclaredSymbol(methodSyntax);
                    var name = symbol?.Name ?? methodSyntax.Identifier.Text;
                    var paramCount = symbol?.Parameters.Length ?? methodSyntax.ParameterList.Parameters.Count;
                    existingMethods.Add((name, paramCount));
                }
            }

            // Determine new methods to add (not yet in class)
            var newMethodsToAdd = new List<IMethodSymbol>();
            for (int i = 0, count = linqMethods.Count; i < count; i++)
            {
                var lm = linqMethods[i];
                bool exists = false;
                for (int j = 0, eCount = existingMethods.Count; j < eCount; j++)
                {
                    if (existingMethods[j].Name == lm.Name && existingMethods[j].ParamCount == lm.Parameters.Length)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    newMethodsToAdd.Add(lm);
                }
            }

            if (newMethodsToAdd.Count == 0) return portalRoot;

            // Order new methods by name
            newMethodsToAdd = newMethodsToAdd
                .OrderBy(static m => m.Name, StringComparer.Ordinal)
                .ThenBy(static m => m.Parameters.Length)
                .ToList();

            var currentMembers = classDecl.Members;

            // Insert new methods: immediate after existing method which has same name, or at last
            for (int i = 0, count = newMethodsToAdd.Count; i < count; i++)
            {
                var newMethod = newMethodsToAdd[i];
                var forwarderText = GenerateMethodForwarder(newMethod, indent);
                if (SyntaxFactory.ParseMemberDeclaration(forwarderText) is not MethodDeclarationSyntax parsedSyntax)
                {
                    continue;
                }

                // Find index of existing method with the same name
                int insertIndex = -1;
                for (int mIdx = currentMembers.Count - 1; mIdx >= 0; mIdx--)
                {
                    if (currentMembers[mIdx] is MethodDeclarationSyntax m && m.Identifier.Text == newMethod.Name)
                    {
                        insertIndex = mIdx + 1;
                        break;
                    }
                }

                // If not found, insert at last
                if (insertIndex < 0)
                {
                    insertIndex = currentMembers.Count;
                }

                parsedSyntax = parsedSyntax.WithLeadingTrivia(SyntaxFactory.Whitespace(insertIndex == 0 ? indent : "\n" + indent))
                    .WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));

                currentMembers = currentMembers.Insert(insertIndex, parsedSyntax);
            }

            var closeBrace = classDecl.CloseBraceToken.WithLeadingTrivia(SyntaxFactory.Whitespace(hasNamespace ? "    " : ""));
            var newClassDecl = classDecl.WithMembers(currentMembers)
                .WithCloseBraceToken(closeBrace);
            return portalRoot.ReplaceNode(classDecl, newClassDecl);
        }

        private static bool ContainsMethod(List<IMethodSymbol> list, IMethodSymbol method)
        {
            for (int i = 0, count = list.Count; i < count; i++)
            {
                if (AreMethodsEquivalent(list[i], method))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool AreMethodsEquivalent(IMethodSymbol m1, IMethodSymbol m2)
        {
            if (m1.Name != m2.Name) return false;
            if (m1.Parameters.Length != m2.Parameters.Length) return false;
            if (m1.TypeParameters.Length != m2.TypeParameters.Length) return false;

            return SymbolEqualityComparer.Default.Equals(m1, m2);
        }

        private static bool IsSystemLinqEnumerableMethod(IMethodSymbol method) =>
            method is {
                ContainingType: {
                    Name: "Enumerable",
                    ContainingNamespace: {
                        Name: "Linq",
                        ContainingNamespace: {
                            Name: "System",
                            ContainingNamespace: {
                                IsGlobalNamespace: true
                            }
                        }
                    }
                }
            };

        private static string GetTopLevelNamespace(SyntaxNode root)
        {
            foreach (var node in root.DescendantNodes())
            {
                if (node is NamespaceDeclarationSyntax ns)
                {
                    var full = ns.Name.ToString();
                    var dot = full.IndexOf('.');
                    return dot > 0 ? full.Substring(0, dot) : full;
                }

                if (node.GetType().Name == FileScopedNamespaceDeclarationSyntaxName)
                {
                    var nameProp = node.GetType().GetProperty("Name");
                    if (nameProp?.GetValue(node) is NameSyntax nameSyntax)
                    {
                        var full = nameSyntax.ToString();
                        var dot = full.IndexOf('.');
                        return dot > 0 ? full.Substring(0, dot) : full;
                    }
                }
            }

            return "Global";
        }

        private static string GenerateMethodForwarder(IMethodSymbol method, string indent)
        {
            var signature = method.ToDisplayString(MethodSignatureFormat);

            var typeArgs = method.TypeParameters.Length > 0
                ? "<" + string.Join(", ", method.TypeParameters.Select(static tp => tp.Name)) + ">"
                : "";

            var args = string.Join(", ", method.Parameters.Select(static p => p.Name));
            var returnKeyword = method.ReturnsVoid ? "" : "return ";

            var sb = new StringBuilder();
            sb.AppendLine($"{indent}[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}{signature}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    {returnKeyword}global::System.Linq.Enumerable.{method.Name}{typeArgs}({args});");
            sb.Append($"{indent}}}");

            return sb.ToString();
        }
    }
}
