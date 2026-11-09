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

            // Remove using System.Linq; preserving trivia
            var newRoot = RemoveUsingDirective(root, usingDirective);
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

        private static SyntaxNode RemoveUsingDirective(SyntaxNode root, UsingDirectiveSyntax usingDirective)
        {
            var leadingTrivia = usingDirective.GetLeadingTrivia();
            bool hasComments = leadingTrivia.Any(static t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

            if (usingDirective.Parent is CompilationUnitSyntax compUnit)
            {
                var index = compUnit.Usings.IndexOf(usingDirective);
                var newUsings = compUnit.Usings.RemoveAt(index);

                if (hasComments)
                {
                    var triviaToKeep = TrimTrailingWhitespace(leadingTrivia);
                    if (index < newUsings.Count)
                    {
                        var next = newUsings[index];
                        newUsings = newUsings.Replace(next, next.WithLeadingTrivia(triviaToKeep.AddRange(next.GetLeadingTrivia())));
                    }
                    else if (compUnit.Members.Count > 0)
                    {
                        var firstMember = compUnit.Members[0];
                        compUnit = compUnit.WithMembers(compUnit.Members.Replace(firstMember, firstMember.WithLeadingTrivia(triviaToKeep.AddRange(firstMember.GetLeadingTrivia()))));
                    }
                }

                return compUnit.WithUsings(newUsings);
            }

            if (usingDirective.Parent is NamespaceDeclarationSyntax nsDecl)
            {
                var index = nsDecl.Usings.IndexOf(usingDirective);
                var newUsings = nsDecl.Usings.RemoveAt(index);

                if (hasComments)
                {
                    var triviaToKeep = TrimTrailingWhitespace(leadingTrivia);
                    if (index < newUsings.Count)
                    {
                        var next = newUsings[index];
                        newUsings = newUsings.Replace(next, next.WithLeadingTrivia(triviaToKeep.AddRange(next.GetLeadingTrivia())));
                    }
                    else if (nsDecl.Members.Count > 0)
                    {
                        var firstMember = nsDecl.Members[0];
                        nsDecl = nsDecl.WithMembers(nsDecl.Members.Replace(firstMember, firstMember.WithLeadingTrivia(triviaToKeep.AddRange(firstMember.GetLeadingTrivia()))));
                    }
                }

                return root.ReplaceNode(nsDecl, nsDecl.WithUsings(newUsings));
            }

            return root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
        }

        private static SyntaxTriviaList TrimTrailingWhitespace(SyntaxTriviaList trivia)
        {
            var list = new List<SyntaxTrivia>(trivia);
            while (list.Count > 0 && list[list.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                list.RemoveAt(list.Count - 1);
            }
            return SyntaxFactory.TriviaList(list);
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

            // Existing methods: (Name, ParamCount, Syntax)
            var existingMethods = new List<(string Name, int ParamCount, MethodDeclarationSyntax Syntax)>();
            foreach (var member in classDecl.Members)
            {
                if (member is MethodDeclarationSyntax methodSyntax)
                {
                    var symbol = portalModel?.GetDeclaredSymbol(methodSyntax);
                    var name = symbol?.Name ?? methodSyntax.Identifier.Text;
                    var paramCount = symbol?.Parameters.Length ?? methodSyntax.ParameterList.Parameters.Count;
                    existingMethods.Add((name, paramCount, methodSyntax));
                }
            }

            // Combine methods: existing methods reuse their syntax/body as-is, new methods get forwarder
            var allMethodSyntaxes = new List<(string Name, int ParamCount, MethodDeclarationSyntax Syntax)>();

            // a. Keep all existing methods with bodies
            for (int i = 0, count = existingMethods.Count; i < count; i++)
            {
                allMethodSyntaxes.Add(existingMethods[i]);
            }

            // b. Add newly required LINQ methods if not already existing
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
                    var forwarderText = GenerateMethodForwarder(lm, indent);
                    if (SyntaxFactory.ParseMemberDeclaration(forwarderText) is MethodDeclarationSyntax parsedSyntax)
                    {
                        allMethodSyntaxes.Add((lm.Name, lm.Parameters.Length, parsedSyntax));
                    }
                }
            }

            // Sort all methods in alphabetical order by method name
            var sortedList = allMethodSyntaxes
                .OrderBy(static m => m.Name, StringComparer.Ordinal)
                .ThenBy(static m => m.ParamCount)
                .ToList();

            var sortedSyntaxes = new List<MemberDeclarationSyntax>();
            for (int i = 0, count = sortedList.Count; i < count; i++)
            {
                var syntax = sortedList[i].Syntax;
                if (i == 0)
                {
                    syntax = syntax.WithLeadingTrivia(SyntaxFactory.Whitespace(indent));
                }
                else
                {
                    syntax = syntax.WithLeadingTrivia(SyntaxFactory.Whitespace("\n\n" + indent));
                }

                syntax = syntax.WithTrailingTrivia(SyntaxTriviaList.Empty);
                sortedSyntaxes.Add(syntax);
            }

            var closeBrace = classDecl.CloseBraceToken.WithLeadingTrivia(SyntaxFactory.Whitespace("\n" + (hasNamespace ? "    " : "")));
            var newClassDecl = classDecl.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(sortedSyntaxes))
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

        private static bool IsSystemLinqEnumerableMethod(IMethodSymbol method)
        {
            var containingType = method.ContainingType;
            if (containingType == null) return false;

            return containingType.Name == "Enumerable"
                && containingType.ContainingNamespace?.ToDisplayString() == "System.Linq";
        }

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

                if (node.GetType().Name == "FileScopedNamespaceDeclarationSyntax")
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
            var returnTypeStr = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var typeParameters = method.TypeParameters;
            string typeParamsStr = "";
            string typeArgsCallStr = "";
            if (typeParameters.Length > 0)
            {
                var tpNames = typeParameters.Select(static tp => tp.Name).ToArray();
                typeParamsStr = "<" + string.Join(", ", tpNames) + ">";
                typeArgsCallStr = "<" + string.Join(", ", tpNames) + ">";
            }

            var paramList = new List<string>();
            var argList = new List<string>();

            for (int i = 0, pCount = method.Parameters.Length; i < pCount; i++)
            {
                var param = method.Parameters[i];
                var pTypeStr = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var pModifier = i == 0 && method.IsExtensionMethod ? "this " : "";

                paramList.Add($"{pModifier}{pTypeStr} {param.Name}");
                argList.Add(param.Name);
            }

            var paramsFormatted = string.Join(", ", paramList);
            var argsFormatted = string.Join(", ", argList);

            var constraints = new List<string>();
            for (int i = 0, tpCount = typeParameters.Length; i < tpCount; i++)
            {
                var tp = typeParameters[i];
                var constraintClauses = new List<string>();
                if (tp.HasReferenceTypeConstraint) constraintClauses.Add("class");
                if (tp.HasValueTypeConstraint) constraintClauses.Add("struct");
                if (tp.HasNotNullConstraint) constraintClauses.Add("notnull");
                if (tp.HasUnmanagedTypeConstraint) constraintClauses.Add("unmanaged");

                for (int c = 0, ctCount = tp.ConstraintTypes.Length; c < ctCount; c++)
                {
                    constraintClauses.Add(tp.ConstraintTypes[c].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }

                if (tp.HasConstructorConstraint) constraintClauses.Add("new()");

                if (constraintClauses.Count > 0)
                {
                    constraints.Add($"where {tp.Name} : {string.Join(", ", constraintClauses)}");
                }
            }

            string constraintStr = constraints.Count > 0 ? " " + string.Join(" ", constraints) : "";
            bool isVoid = method.ReturnsVoid;
            string returnKeyword = isVoid ? "" : "return ";

            var sb = new StringBuilder();
            sb.AppendLine($"{indent}public static {returnTypeStr} {method.Name}{typeParamsStr}({paramsFormatted}){constraintStr}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    {returnKeyword}global::System.Linq.Enumerable.{method.Name}{typeArgsCallStr}({argsFormatted});");
            sb.Append($"{indent}}}");

            return sb.ToString();
        }
    }
}
