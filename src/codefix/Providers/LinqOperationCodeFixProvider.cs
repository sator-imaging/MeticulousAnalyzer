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
            return LinqOperationFixAllProvider.Instance;
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
                var usingDirective = FindUsingDirective(node);
                if (usingDirective == null) continue;

                var title = CodeFixResources.CodeFix_LinqOperation ?? "Create LinqPortal and remove using System.Linq";

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => FixLinqOperationsAsync(
                            context.Document,
                            ImmutableArray.Create(usingDirective.Span),
                            context.Document.Project.Solution,
                            c),
                        equivalenceKey: nameof(LinqOperationCodeFixProvider)),
                    diagnostic);
            }
        }

        private static async Task<Solution> FixLinqOperationsAsync(
            Document document,
            ImmutableArray<Microsoft.CodeAnalysis.Text.TextSpan> directiveSpans,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (semanticModel == null || root == null) return solution;

            var usingDirectives = new List<UsingDirectiveSyntax>();
            for (int i = 0, count = directiveSpans.Length; i < count; i++)
            {
                var node = root.FindNode(directiveSpans[i], getInnermostNodeForTie: true);
                var usingDirective = FindUsingDirective(node);
                if (usingDirective != null && !ContainsUsingDirective(usingDirectives, usingDirective))
                {
                    usingDirectives.Add(usingDirective);
                }
            }

            var namespaceChanges = new List<PortalChange>();
            var methodCallRewrites = new Dictionary<SyntaxNode, string>();
            var typeRewrites = new Dictionary<SyntaxNode, string>();
            foreach (var usingDirective in usingDirectives)
            {
                var namespaceName = GetNamespaceName(usingDirective, root);
                var scope = GetUsingScope(usingDirective);
                var change = FindPortalChange(namespaceChanges, namespaceName);
                foreach (var node in root.DescendantNodes())
                {
                    if (!IsWithinScope(node, scope)) continue;

                    if (node is InvocationExpressionSyntax invocation)
                    {
                        AddMethod(semanticModel.GetSymbolInfo(invocation, cancellationToken), change.Methods);
                    }

                    if (node is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
                        AddMethod(symbolInfo, change.Methods);

                        if (semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is INamedTypeSymbol containingType
                            && IsSystemLinqMethodType(containingType))
                        {
                            methodCallRewrites[memberAccess] = GetPortalClassName(namespaceName);
                        }
                    }

                    if (node is NameSyntax nameSyntax
                        && !(nameSyntax is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "var")
                        && semanticModel.GetSymbolInfo(nameSyntax, cancellationToken).Symbol is INamedTypeSymbol namedType
                        && IsSystemLinqType(namedType)
                        && !IsNestedTypeName(nameSyntax, semanticModel, cancellationToken))
                    {
                        typeRewrites[nameSyntax] = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }
                }
            }

            var rewrittenNodes = new List<SyntaxNode>(methodCallRewrites.Count + typeRewrites.Count + usingDirectives.Count);
            var nodeAnnotations = new Dictionary<SyntaxNode, SyntaxAnnotation>();
            var rewriteAnnotations = new Dictionary<SyntaxAnnotation, (string Value, bool IsPortalCall)>();
            var directiveAnnotations = new List<SyntaxAnnotation>();
            foreach (var entry in methodCallRewrites)
            {
                var annotation = new SyntaxAnnotation();
                nodeAnnotations[entry.Key] = annotation;
                rewriteAnnotations[annotation] = (entry.Value, true);
                rewrittenNodes.Add(entry.Key);
            }
            foreach (var entry in typeRewrites)
            {
                if (!nodeAnnotations.ContainsKey(entry.Key))
                {
                    var annotation = new SyntaxAnnotation();
                    nodeAnnotations[entry.Key] = annotation;
                    rewriteAnnotations[annotation] = (entry.Value, false);
                    rewrittenNodes.Add(entry.Key);
                }
            }
            for (int i = 0, count = usingDirectives.Count; i < count; i++)
            {
                var directive = usingDirectives[i];
                var annotation = new SyntaxAnnotation();
                nodeAnnotations[directive] = annotation;
                directiveAnnotations.Add(annotation);
                rewrittenNodes.Add(directive);
            }

            if (rewrittenNodes.Count > 0)
            {
                var annotatedRoot = root.ReplaceNodes(
                    rewrittenNodes,
                    (original, rewritten) => rewritten.WithAdditionalAnnotations(nodeAnnotations[original]));

                foreach (var entry in rewriteAnnotations)
                {
                    var annotatedNode = annotatedRoot.GetAnnotatedNodes(entry.Key);
                    var node = FindFirstNode(annotatedNode);
                    if (node == null) continue;

                    SyntaxNode replacement = entry.Value.IsPortalCall
                        ? node is MemberAccessExpressionSyntax member
                            ? member.WithExpression(SyntaxFactory.ParseExpression(entry.Value.Value).WithTriviaFrom(member.Expression))
                            : node
                        : SyntaxFactory.ParseTypeName(entry.Value.Value).WithTriviaFrom(node);
                    annotatedRoot = annotatedRoot.ReplaceNode(node, replacement);
                }

                var nodesToRemove = new List<SyntaxNode>();
                for (int i = 0, count = directiveAnnotations.Count; i < count; i++)
                {
                    var node = FindFirstNode(annotatedRoot.GetAnnotatedNodes(directiveAnnotations[i]));
                    if (node != null) nodesToRemove.Add(node);
                }
                root = annotatedRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepExteriorTrivia) ?? annotatedRoot;
            }
            solution = solution.WithDocumentSyntaxRoot(document.Id, root);

            for (int i = 0, count = namespaceChanges.Count; i < count; i++)
            {
                var change = namespaceChanges[i];
                var portalFileName = string.IsNullOrEmpty(change.NamespaceName)
                    ? "LinqPortal.cs"
                    : $"LinqPortal.{change.NamespaceName}.cs";
                var currentProject = solution.GetProject(project.Id)!;
                var portalDoc = await FindPortalDocumentAsync(currentProject, change.NamespaceName, cancellationToken).ConfigureAwait(false);

                if (portalDoc == null)
                {
                    var emptyPlaceholder = GenerateEmptyPlaceholder(change.NamespaceName);
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

                var portalRoot = await portalDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                var portalModel = await portalDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                if (portalRoot != null)
                {
                    var updatedPortalRoot = UpdatePortalClass(portalRoot, portalModel, change.NamespaceName, change.Methods);
                    solution = solution.WithDocumentSyntaxRoot(portalDoc.Id, updatedPortalRoot);
                }
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
            var existingMethods = new List<IMethodSymbol>();
            foreach (var member in classDecl.Members)
            {
                if (member is MethodDeclarationSyntax methodSyntax)
                {
                    var symbol = portalModel?.GetDeclaredSymbol(methodSyntax);
                    if (symbol != null) existingMethods.Add(symbol);
                }
            }

            var newMethodsToAdd = new List<IMethodSymbol>();
            for (int i = 0, count = linqMethods.Count; i < count; i++)
            {
                var lm = linqMethods[i];
                bool exists = false;
                for (int j = 0, eCount = existingMethods.Count; j < eCount; j++)
                {
                    if (AreMethodsEquivalent(existingMethods[j], lm))
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

            newMethodsToAdd.Sort(static (left, right) =>
            {
                var nameComparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (nameComparison != 0) return nameComparison;

                var parameterCountComparison = left.Parameters.Length.CompareTo(right.Parameters.Length);
                if (parameterCountComparison != 0) return parameterCountComparison;

                return StringComparer.Ordinal.Compare(
                    GetMethodSignatureKey(left),
                    GetMethodSignatureKey(right));
            });

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

            var closeBrace = classDecl.CloseBraceToken;
            var newClassDecl = classDecl.WithMembers(currentMembers)
                .WithCloseBraceToken(closeBrace);
            return portalRoot.ReplaceNode(classDecl, newClassDecl);
        }

        private static bool AreMethodsEquivalent(IMethodSymbol m1, IMethodSymbol m2)
        {
            if (m1.Name != m2.Name) return false;
            if (m1.Parameters.Length != m2.Parameters.Length) return false;
            if (m1.TypeParameters.Length != m2.TypeParameters.Length) return false;
            if (m1.IsExtensionMethod != m2.IsExtensionMethod) return false;

            for (int i = 0, count = m1.Parameters.Length; i < count; i++)
            {
                var left = m1.Parameters[i];
                var right = m2.Parameters[i];
                if (left.RefKind != right.RefKind) return false;
                if (left.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    != right.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetMethodSignatureKey(IMethodSymbol method)
        {
            var sb = new StringBuilder();
            sb.Append(method.TypeParameters.Length).Append('|');
            for (int i = 0, count = method.Parameters.Length; i < count; i++)
            {
                var parameter = method.Parameters[i];
                sb.Append((int)parameter.RefKind)
                    .Append(':')
                    .Append(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .Append('|');
            }

            return sb.ToString();
        }

        private static string GenerateMethodForwarder(IMethodSymbol method, string indent)
        {
            var signature = method.ToDisplayString(MethodSignatureFormat);

            var typeArgs = string.Empty;
            if (method.TypeParameters.Length > 0)
            {
                var typeArgBuilder = new StringBuilder("<");
                for (int i = 0, count = method.TypeParameters.Length; i < count; i++)
                {
                    if (i > 0) typeArgBuilder.Append(", ");
                    typeArgBuilder.Append(method.TypeParameters[i].Name);
                }
                typeArgs = typeArgBuilder.Append('>').ToString();
            }

            var argsBuilder = new StringBuilder();
            for (int i = 0, count = method.Parameters.Length; i < count; i++)
            {
                if (i > 0) argsBuilder.Append(", ");
                var parameter = method.Parameters[i];
                var refKind = parameter.RefKind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    RefKind.In => "in ",
                    _ => string.Empty
                };
                argsBuilder.Append(refKind);
                if (SyntaxFacts.GetKeywordKind(parameter.Name) != SyntaxKind.None)
                {
                    argsBuilder.Append('@');
                }
                argsBuilder.Append(parameter.Name);
            }
            var args = argsBuilder.ToString();
            var returnKeyword = method.ReturnsVoid ? "" : "return ";

            var sb = new StringBuilder();
            sb.AppendLine($"{indent}[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}{signature}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    {returnKeyword}global::System.Linq.{method.ContainingType.Name}.{method.Name}{typeArgs}({args});");
            sb.Append($"{indent}}}");

            return sb.ToString();
        }

        private static UsingDirectiveSyntax? FindUsingDirective(SyntaxNode? node)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current is UsingDirectiveSyntax usingDirective) return usingDirective;
            }
            return null;
        }

        private static bool ContainsUsingDirective(List<UsingDirectiveSyntax> list, UsingDirectiveSyntax directive)
        {
            for (int i = 0, count = list.Count; i < count; i++)
            {
                if (list[i].Span == directive.Span) return true;
            }
            return false;
        }

        private static void AddMethod(SymbolInfo symbolInfo, List<IMethodSymbol> methods)
        {
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                AddMethod(method, methods);
                return;
            }

            for (int i = 0, count = symbolInfo.CandidateSymbols.Length; i < count; i++)
            {
                if (symbolInfo.CandidateSymbols[i] is IMethodSymbol candidate)
                {
                    AddMethod(candidate, methods);
                }
            }
        }

        private static void AddMethod(IMethodSymbol method, List<IMethodSymbol> methods)
        {
            if (!IsSystemLinqMethodType(method.ContainingType)) return;
            method = (method.ReducedFrom ?? method).ConstructedFrom;

            for (int i = 0, count = methods.Count; i < count; i++)
            {
                if (AreMethodsEquivalent(methods[i], method)) return;
            }
            methods.Add(method);
        }

        private static bool IsSystemLinqMethodType(INamedTypeSymbol type) =>
            (type.Name is "Enumerable" or "Queryable") && IsSystemLinqType(type);

        private static bool IsSystemLinqType(INamedTypeSymbol type) =>
            type.ContainingNamespace is {
                Name: "Linq",
                ContainingNamespace: {
                    Name: "System",
                    ContainingNamespace: { IsGlobalNamespace: true }
                }
            };

        private static bool IsNestedTypeName(
            NameSyntax nameSyntax,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (nameSyntax.Parent is not NameSyntax parentName) return false;
            return semanticModel.GetTypeInfo(parentName, cancellationToken).Type is INamedTypeSymbol parentType
                && IsSystemLinqType(parentType);
        }

        private static SyntaxNode? FindFirstNode(IEnumerable<SyntaxNode> nodes)
        {
            foreach (var node in nodes) return node;
            return null;
        }

        private static PortalChange FindPortalChange(List<PortalChange> changes, string namespaceName)
        {
            for (int i = 0, count = changes.Count; i < count; i++)
            {
                if (changes[i].NamespaceName == namespaceName) return changes[i];
            }

            var change = new PortalChange(namespaceName);
            changes.Add(change);
            return change;
        }

        private static SyntaxNode? GetUsingScope(UsingDirectiveSyntax usingDirective)
        {
            for (var current = usingDirective.Parent; current != null; current = current.Parent)
            {
                if (current is NamespaceDeclarationSyntax || current.GetType().Name == FileScopedNamespaceDeclarationSyntaxName)
                {
                    return current;
                }
            }
            return null;
        }

        private static bool IsWithinScope(SyntaxNode node, SyntaxNode? scope)
        {
            if (scope == null) return true;
            for (var current = node; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, scope)) return true;
            }
            return false;
        }

        private static string GetNamespaceName(UsingDirectiveSyntax usingDirective, SyntaxNode root)
        {
            var scope = GetUsingScope(usingDirective);
            if (scope != null)
            {
                return GetTopLevelNamespaceName(GetNamespaceDeclarationName(scope) ?? string.Empty);
            }

            string commonNamespace = string.Empty;
            bool foundNamespace = false;
            foreach (var node in root.ChildNodes())
            {
                if (node is UsingDirectiveSyntax || node is ExternAliasDirectiveSyntax) continue;
                var namespaceName = GetNamespaceDeclarationName(node);
                if (namespaceName == null) return string.Empty;
                namespaceName = GetTopLevelNamespaceName(namespaceName);
                if (!foundNamespace)
                {
                    commonNamespace = namespaceName;
                    foundNamespace = true;
                }
                else if (commonNamespace != namespaceName)
                {
                    return string.Empty;
                }
            }

            return foundNamespace ? commonNamespace : string.Empty;
        }

        private static string? GetNamespaceDeclarationName(SyntaxNode node)
        {
            if (node is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration.Name.ToString();
            }

            if (node.GetType().Name == FileScopedNamespaceDeclarationSyntaxName)
            {
                var nameProperty = node.GetType().GetProperty("Name");
                return nameProperty?.GetValue(node) is NameSyntax name ? name.ToString() : string.Empty;
            }

            return null;
        }

        private static string GetTopLevelNamespaceName(string namespaceName)
        {
            var dot = namespaceName.IndexOf('.');
            return dot > 0 ? namespaceName.Substring(0, dot) : namespaceName;
        }

        private static string GetPortalClassName(string namespaceName) =>
            string.IsNullOrEmpty(namespaceName)
                ? "global::LinqPortal"
                : $"global::{namespaceName}.LinqPortal";

        private static async Task<Document?> FindPortalDocumentAsync(
            Project project,
            string namespaceName,
            CancellationToken cancellationToken)
        {
            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (root == null || model == null) continue;

                foreach (var node in root.DescendantNodes())
                {
                    if (node is ClassDeclarationSyntax classDeclaration
                        && classDeclaration.Identifier.ValueText == "LinqPortal"
                        && model.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol type
                        && type.ContainingNamespace.ToDisplayString() == namespaceName)
                    {
                        return document;
                    }
                }
            }
            return null;
        }

        private sealed class PortalChange
        {
            public string NamespaceName { get; }
            public List<IMethodSymbol> Methods { get; } = new();

            public PortalChange(string namespaceName)
            {
                NamespaceName = namespaceName;
            }
        }

        private sealed class LinqOperationFixAllProvider : FixAllProvider
        {
            public static readonly FixAllProvider Instance = new LinqOperationFixAllProvider();

            public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var title = CodeFixResources.CodeFix_LinqOperation ?? "Create LinqPortal and remove using System.Linq";
                return Task.FromResult<CodeAction?>(CodeAction.Create(
                    title,
                    cancellationToken => FixAllAsync(fixAllContext, cancellationToken),
                    nameof(LinqOperationCodeFixProvider)));
            }

            private static async Task<Solution> FixAllAsync(
                FixAllContext fixAllContext,
                CancellationToken cancellationToken)
            {
                var originalSolution = fixAllContext.Solution;
                var documentIds = new List<DocumentId>();
                if (fixAllContext.Scope == FixAllScope.Document && fixAllContext.Document != null)
                {
                    documentIds.Add(fixAllContext.Document.Id);
                }
                else
                {
                    foreach (var project in originalSolution.Projects)
                    {
                        if (fixAllContext.Scope == FixAllScope.Project && project.Id != fixAllContext.Project.Id) continue;
                        foreach (var document in project.Documents)
                        {
                            documentIds.Add(document.Id);
                        }
                    }
                }

                var solution = originalSolution;
                for (int i = 0, count = documentIds.Count; i < count; i++)
                {
                    var originalDocument = originalSolution.GetDocument(documentIds[i]);
                    var currentDocument = solution.GetDocument(documentIds[i]);
                    if (originalDocument == null || currentDocument == null) continue;

                    var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(originalDocument).ConfigureAwait(false);
                    var spans = ImmutableArray.CreateBuilder<Microsoft.CodeAnalysis.Text.TextSpan>();
                    foreach (var diagnostic in diagnostics)
                    {
                        if (diagnostic.Id == LinqOperationAnalyzer.RuleId_LinqOperation && diagnostic.Location.IsInSource)
                        {
                            spans.Add(diagnostic.Location.SourceSpan);
                        }
                    }

                    if (spans.Count > 0)
                    {
                        solution = await FixLinqOperationsAsync(
                            currentDocument,
                            spans.ToImmutable(),
                            solution,
                            cancellationToken).ConfigureAwait(false);
                    }
                }

                return solution;
            }
        }
    }
}
