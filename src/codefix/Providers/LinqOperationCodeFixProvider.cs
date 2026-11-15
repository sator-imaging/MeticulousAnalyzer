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
        // Microsoft.CodeAnalysis.CSharp 3.8.0 has no named SyntaxKind for file-scoped namespaces.
        private const int FileScopedNamespaceDeclarationRawKind = 8845;
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
            var portalNamespace = GetTopLevelNamespaceName(document.Project.DefaultNamespace ?? string.Empty);
            var hasProjectDefaultNamespace = !string.IsNullOrEmpty(portalNamespace);
            foreach (var node in root.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax
                    && (node is not MemberAccessExpressionSyntax || node.Parent is InvocationExpressionSyntax))
                {
                    continue;
                }

                if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is not IMethodSymbol method
                    || !IsSystemLinqNamespace(method.ContainingNamespace))
                {
                    continue;
                }

                if (methods.Count == 0 && !hasProjectDefaultNamespace)
                {
                    portalNamespace = GetTopLevelNamespaceName(node);
                }
                AddIfMissing(methods, (method.ReducedFrom ?? method).ConstructedFrom);
            }

            root = RemoveAdjacentWhitespaceTrivia(root, usingDirective);
            usingDirective = FindUsingDirective(root.FindNode(usingDirective.Span, getInnermostNodeForTie: true)) ?? usingDirective;
            root = root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepTrailingTrivia) ?? root;
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
                var closeBrace = portalClass.CloseBraceToken;
                var portalText = await portalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var closeBraceLine = portalText.Lines.GetLineFromPosition(closeBrace.SpanStart);
                var closeBraceIndentationSpan = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(
                    closeBraceLine.Start,
                    closeBrace.SpanStart);
                var classIndentation = portalText.ToString(closeBraceIndentationSpan);
                if (classIndentation.Trim().Length != 0) classIndentation = string.Empty;
                var methodIndentation = classIndentation + "    ";
                var existingMethods = new List<IMethodSymbol>();
                foreach (var member in members)
                {
                    if (member is MethodDeclarationSyntax declaration
                        && portalModel?.GetDeclaredSymbol(declaration) is IMethodSymbol existing)
                    {
                        existingMethods.Add(existing);
                    }
                }

                var forwardersByPosition = new Dictionary<int, List<string>>();
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

                    var insertionPosition = FindMethodInsertionPosition(
                        members,
                        method,
                        portalText,
                        closeBraceIndentationSpan.Start);
                    if (!forwardersByPosition.TryGetValue(insertionPosition, out var forwarders))
                    {
                        forwarders = new List<string>();
                        forwardersByPosition.Add(insertionPosition, forwarders);
                    }
                    forwarders.Add(GenerateMethodForwarder(method, methodIndentation));
                }

                var changes = new List<Microsoft.CodeAnalysis.Text.TextChange>();
                foreach (var member in members)
                {
                    var insertionPosition = GetMemberInsertionPosition(member, portalText);
                    if (!forwardersByPosition.TryGetValue(insertionPosition, out var forwarders)) continue;
                    changes.Add(new Microsoft.CodeAnalysis.Text.TextChange(
                        new Microsoft.CodeAnalysis.Text.TextSpan(insertionPosition, 0),
                        string.Join(Environment.NewLine + Environment.NewLine, forwarders)
                            + Environment.NewLine
                            + Environment.NewLine));
                    forwardersByPosition.Remove(insertionPosition);
                }

                if (forwardersByPosition.TryGetValue(closeBraceIndentationSpan.Start, out var appendedForwarders))
                {
                    var insertionText = (members.Count == 0 ? string.Empty : Environment.NewLine)
                        + string.Join(Environment.NewLine + Environment.NewLine, appendedForwarders)
                        + Environment.NewLine
                        + classIndentation;
                    changes.Add(new Microsoft.CodeAnalysis.Text.TextChange(
                        closeBraceIndentationSpan,
                        insertionText));
                }

                if (changes.Count > 0)
                {
                    portalText = portalText.WithChanges(changes);
                    solution = solution.WithDocumentText(portalDocument.Id, portalText);
                }
            }

            return solution;
        }

        private static int FindMethodInsertionPosition(
            SyntaxList<MemberDeclarationSyntax> members,
            IMethodSymbol method,
            Microsoft.CodeAnalysis.Text.SourceText portalText,
            int appendPosition)
        {
            var lastOverloadIndex = -1;
            for (int i = 0, count = members.Count; i < count; i++)
            {
                if (members[i] is MethodDeclarationSyntax declaration
                    && declaration.Identifier.ValueText == method.Name)
                {
                    lastOverloadIndex = i;
                }
            }

            if (lastOverloadIndex < 0 || lastOverloadIndex + 1 >= members.Count)
            {
                return appendPosition;
            }

            return GetMemberInsertionPosition(members[lastOverloadIndex + 1], portalText);
        }

        private static int GetMemberInsertionPosition(
            MemberDeclarationSyntax member,
            Microsoft.CodeAnalysis.Text.SourceText portalText)
        {
            foreach (var trivia in member.GetLeadingTrivia())
            {
                if (trivia.Kind() != SyntaxKind.WhitespaceTrivia
                    && trivia.Kind() != SyntaxKind.EndOfLineTrivia)
                {
                    return portalText.Lines.GetLineFromPosition(trivia.SpanStart).Start;
                }
            }

            return portalText.Lines.GetLineFromPosition(member.GetFirstToken().SpanStart).Start;
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
                    || !AreParameterTypesEquivalent(left.Parameters[i].Type, right.Parameters[i].Type))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AreParameterTypesEquivalent(ITypeSymbol left, ITypeSymbol right)
        {
            if (left is ITypeParameterSymbol leftTypeParameter
                && right is ITypeParameterSymbol rightTypeParameter)
            {
                if (leftTypeParameter.TypeParameterKind != rightTypeParameter.TypeParameterKind
                    || leftTypeParameter.Ordinal != rightTypeParameter.Ordinal)
                {
                    return false;
                }

                return leftTypeParameter.TypeParameterKind == TypeParameterKind.Method
                    || SymbolEqualityComparer.Default.Equals(
                        leftTypeParameter.ContainingSymbol,
                        rightTypeParameter.ContainingSymbol);
            }

            if (left is INamedTypeSymbol leftNamedType
                && right is INamedTypeSymbol rightNamedType)
            {
                if (!SymbolEqualityComparer.Default.Equals(
                        leftNamedType.OriginalDefinition,
                        rightNamedType.OriginalDefinition)
                    || leftNamedType.TypeArguments.Length != rightNamedType.TypeArguments.Length)
                {
                    return false;
                }

                for (int i = 0, count = leftNamedType.TypeArguments.Length; i < count; i++)
                {
                    if (!AreParameterTypesEquivalent(
                            leftNamedType.TypeArguments[i],
                            rightNamedType.TypeArguments[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (left is IArrayTypeSymbol leftArrayType
                && right is IArrayTypeSymbol rightArrayType)
            {
                return leftArrayType.Rank == rightArrayType.Rank
                    && AreParameterTypesEquivalent(leftArrayType.ElementType, rightArrayType.ElementType);
            }

            if (left is IPointerTypeSymbol leftPointerType
                && right is IPointerTypeSymbol rightPointerType)
            {
                return AreParameterTypesEquivalent(leftPointerType.PointedAtType, rightPointerType.PointedAtType);
            }

            return SymbolEqualityComparer.Default.Equals(left, right);
        }

        private static bool IsSystemLinqNamespace(INamespaceSymbol namespaceSymbol)
        {
            return namespaceSymbol.Name is "Linq"
                && namespaceSymbol.ContainingNamespace is INamespaceSymbol
                   {
                       Name: "System", ContainingNamespace: INamespaceSymbol
                       {
                           IsGlobalNamespace: true
                       }
                   };
        }

        private static UsingDirectiveSyntax? FindUsingDirective(SyntaxNode node)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current is UsingDirectiveSyntax directive) return directive;
            }
            return null;
        }

        private static SyntaxNode RemoveAdjacentWhitespaceTrivia(SyntaxNode root, UsingDirectiveSyntax usingDirective)
        {
            var previousToken = usingDirective.GetFirstToken().GetPreviousToken();
            if (previousToken.RawKind != 0)
            {
                var leadingTrivia = default(SyntaxTriviaList);
                AddTrivia(ref leadingTrivia, previousToken.TrailingTrivia);
                AddTrivia(ref leadingTrivia, usingDirective.GetLeadingTrivia());

                var whitespaceStart = leadingTrivia.Count;
                while (whitespaceStart > 0
                    && leadingTrivia[whitespaceStart - 1].Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    whitespaceStart--;
                }

                var remainingLeadingTrivia = TakeTrivia(leadingTrivia, 0, whitespaceStart);
                root = root.ReplaceToken(previousToken, previousToken.WithTrailingTrivia(remainingLeadingTrivia));
                usingDirective = FindUsingDirective(root.FindNode(usingDirective.Span, getInnermostNodeForTie: true)) ?? usingDirective;
            }

            var nextToken = usingDirective.GetLastToken().GetNextToken();
            if (nextToken.RawKind != 0)
            {
                var trailingTrivia = default(SyntaxTriviaList);
                AddTrivia(ref trailingTrivia, usingDirective.GetTrailingTrivia());
                AddTrivia(ref trailingTrivia, nextToken.LeadingTrivia);

                var trailingStart = 0;
                while (trailingStart < trailingTrivia.Count
                    && trailingTrivia[trailingStart].Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    trailingStart++;
                }
                if (trailingStart < trailingTrivia.Count
                    && trailingTrivia[trailingStart].Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    trailingStart++;
                }

                var remainingTrailingTrivia = TakeTrivia(trailingTrivia, trailingStart, trailingTrivia.Count);
                var updatedUsingDirective = usingDirective.WithTrailingTrivia(default(SyntaxTriviaList));
                root = root.ReplaceNode(usingDirective, updatedUsingDirective);
                usingDirective = FindUsingDirective(root.FindNode(usingDirective.Span, getInnermostNodeForTie: true)) ?? updatedUsingDirective;
                nextToken = usingDirective.GetLastToken().GetNextToken();
                root = root.ReplaceToken(nextToken, nextToken.WithLeadingTrivia(remainingTrailingTrivia));
            }

            return root;
        }

        private static void AddTrivia(ref SyntaxTriviaList destination, SyntaxTriviaList source)
        {
            for (int i = 0, count = source.Count; i < count; i++)
            {
                destination = destination.Add(source[i]);
            }
        }

        private static SyntaxTriviaList TakeTrivia(SyntaxTriviaList source, int start, int end)
        {
            var result = default(SyntaxTriviaList);
            for (int i = start; i < end; i++)
            {
                result = result.Add(source[i]);
            }
            return result;
        }

        private static string GetTopLevelNamespaceName(SyntaxNode node)
        {
            NameSyntax? outermostName = null;
            for (var current = node.Parent; current != null; current = current.Parent)
            {
                if (current is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    outermostName = namespaceDeclaration.Name;
                }
                else if (current.RawKind == FileScopedNamespaceDeclarationRawKind)
                {
                    foreach (var child in current.ChildNodes())
                    {
                        if (child is NameSyntax name)
                        {
                            outermostName = name;
                            break;
                        }
                    }
                }
            }

            return outermostName == null ? string.Empty : GetTopLevelName(outermostName);
        }

        private static string GetTopLevelNamespaceName(string namespaceName)
        {
            var separator = namespaceName.IndexOf('.');
            return separator < 0 ? namespaceName : namespaceName.Substring(0, separator);
        }

        private static string GetTopLevelName(NameSyntax name)
        {
            while (name is QualifiedNameSyntax qualifiedName)
            {
                name = qualifiedName.Left;
            }

            return name is IdentifierNameSyntax identifierName
                ? identifierName.Identifier.ValueText
                : string.Empty;
        }

        private static Document? FindPortalDocument(Project project, string fileName)
        {
            foreach (var document in project.Documents)
            {
                if (document.Name == fileName)
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

        private static string GenerateMethodForwarder(IMethodSymbol method, string indentation)
        {
            // Keep the complete signature from Roslyn; do not reconstruct its type parameters or constraints.
            var parameterNames = new string[method.Parameters.Length];
            for (int i = 0, count = method.Parameters.Length; i < count; i++)
            {
                parameterNames[i] = method.Parameters[i].Name;
            }

            return $"{indentation}[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]{Environment.NewLine}"
                + $"{indentation}{method.ToDisplayString(MethodSignatureFormat)}{Environment.NewLine}"
                + $"{indentation}{{{Environment.NewLine}"
                + $"{indentation}    return {method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{method.Name}({string.Join(", ", parameterNames)});{Environment.NewLine}"
                + $"{indentation}}}";
        }
    }
}
