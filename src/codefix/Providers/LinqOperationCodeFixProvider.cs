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
using System.Text.RegularExpressions;
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

            // Collect Linq methods and call site namespaces
            var linqMethods = new List<IMethodSymbol>();
            var callSiteNamespaces = new List<string>();

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

                        var ns = GetCallSiteNamespace(inv);
                        if (!string.IsNullOrEmpty(ns) && !callSiteNamespaces.Contains(ns))
                        {
                            callSiteNamespaces.Add(ns);
                        }
                    }
                }
            }

            var topLevelNamespace = GetTopLevelNamespace(callSiteNamespaces, root);
            var projectNamespace = GetProjectNamespace(project, topLevelNamespace);

            // Remove using System.Linq; preserving trivia
            var newRoot = RemoveUsingDirectivePreservingTrivia(root, usingDirective);
            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);

            // Output file: LinqPortal.<project namespace from csproj>.cs (no .g.cs)
            var portalFileName = !string.IsNullOrEmpty(projectNamespace)
                ? $"LinqPortal.{projectNamespace}.cs"
                : (string.IsNullOrEmpty(topLevelNamespace) ? "LinqPortal.cs" : $"LinqPortal.{topLevelNamespace}.cs");

            var currentProject = solution.GetProject(project.Id)!;
            // Look for existing portal document
            var existingDoc = currentProject.Documents.FirstOrDefault(d => string.Equals(d.Name, portalFileName, StringComparison.OrdinalIgnoreCase))
                ?? currentProject.Documents.FirstOrDefault(d => d.Name != null && d.Name.StartsWith("LinqPortal.", StringComparison.OrdinalIgnoreCase) && d.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

            if (existingDoc != null)
            {
                var existingRoot = await existingDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                var existingModel = await existingDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

                if (existingRoot != null)
                {
                    var updatedRoot = MergePortalDocument(existingRoot, existingModel, topLevelNamespace, linqMethods);
                    solution = solution.WithDocumentSyntaxRoot(existingDoc.Id, updatedRoot);
                }
            }
            else
            {
                var portalSource = GeneratePortalSource(topLevelNamespace, linqMethods);
                var projectDir = !string.IsNullOrEmpty(project.FilePath) ? Path.GetDirectoryName(project.FilePath) : null;
                var filePath = projectDir != null ? Path.Combine(projectDir, portalFileName) : null;

                solution = solution.AddDocument(
                    DocumentId.CreateNewId(project.Id, portalFileName),
                    portalFileName,
                    Microsoft.CodeAnalysis.Text.SourceText.From(portalSource),
                    filePath: filePath);
            }

            return solution;
        }

        private static string GetProjectNamespace(Project project, string fallbackNamespace)
        {
            // 1. Try project.DefaultNamespace
            if (!string.IsNullOrEmpty(project.DefaultNamespace))
            {
                return project.DefaultNamespace!;
            }

            // 2. Try parsing RootNamespace from csproj file if it exists
            if (!string.IsNullOrEmpty(project.FilePath) && File.Exists(project.FilePath))
            {
                try
                {
                    var content = File.ReadAllText(project.FilePath);
                    var match = Regex.Match(content, @"<RootNamespace>\s*([^<\s]+)\s*</RootNamespace>", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }

                    var projName = Path.GetFileNameWithoutExtension(project.FilePath);
                    if (!string.IsNullOrEmpty(projName))
                    {
                        return projName!;
                    }
                }
                catch
                {
                    // Fall back
                }
            }

            return fallbackNamespace;
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

        private static string GetCallSiteNamespace(SyntaxNode node)
        {
            var nsDecl = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (nsDecl != null)
            {
                return nsDecl.Name.ToString();
            }

            var fileScopedNs = node.Ancestors().FirstOrDefault(static n => n.GetType().Name == "FileScopedNamespaceDeclarationSyntax");
            if (fileScopedNs != null)
            {
                var nameProp = fileScopedNs.GetType().GetProperty("Name");
                if (nameProp?.GetValue(fileScopedNs) is NameSyntax nameSyntax)
                {
                    return nameSyntax.ToString();
                }
            }

            return string.Empty;
        }

        private static string GetTopLevelNamespace(List<string> namespaces, SyntaxNode root)
        {
            if (namespaces.Count > 0)
            {
                var firstNs = namespaces[0];
                var dotIndex = firstNs.IndexOf('.');
                return dotIndex > 0 ? firstNs.Substring(0, dotIndex) : firstNs;
            }

            var nsDecl = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (nsDecl != null)
            {
                var fullNs = nsDecl.Name.ToString();
                var dotIndex = fullNs.IndexOf('.');
                return dotIndex > 0 ? fullNs.Substring(0, dotIndex) : fullNs;
            }

            var fileScopedNs = root.DescendantNodes().FirstOrDefault(static n => n.GetType().Name == "FileScopedNamespaceDeclarationSyntax");
            if (fileScopedNs != null)
            {
                var nameProp = fileScopedNs.GetType().GetProperty("Name");
                if (nameProp?.GetValue(fileScopedNs) is NameSyntax nameSyntax)
                {
                    var fullNs = nameSyntax.ToString();
                    var dotIndex = fullNs.IndexOf('.');
                    return dotIndex > 0 ? fullNs.Substring(0, dotIndex) : fullNs;
                }
            }

            return "Global";
        }

        private static SyntaxNode RemoveUsingDirectivePreservingTrivia(SyntaxNode root, UsingDirectiveSyntax usingDirective)
        {
            var leadingTrivia = usingDirective.GetLeadingTrivia();
            var hasComments = leadingTrivia.Any(static t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

            var parent = usingDirective.Parent;
            if (parent is CompilationUnitSyntax compUnit)
            {
                var index = compUnit.Usings.IndexOf(usingDirective);
                var newUsings = compUnit.Usings.RemoveAt(index);

                if (hasComments)
                {
                    if (index < newUsings.Count)
                    {
                        var nextUsing = newUsings[index];
                        var preservedLeading = CombineLeadingTrivia(leadingTrivia, nextUsing.GetLeadingTrivia());
                        newUsings = newUsings.Replace(nextUsing, nextUsing.WithLeadingTrivia(preservedLeading));
                    }
                    else if (compUnit.Members.Count > 0)
                    {
                        var firstMember = compUnit.Members[0];
                        var preservedLeading = CombineLeadingTrivia(leadingTrivia, firstMember.GetLeadingTrivia());
                        compUnit = compUnit.WithMembers(compUnit.Members.Replace(firstMember, firstMember.WithLeadingTrivia(preservedLeading)));
                    }
                }

                return compUnit.WithUsings(newUsings);
            }
            else if (parent is NamespaceDeclarationSyntax nsDecl)
            {
                var index = nsDecl.Usings.IndexOf(usingDirective);
                var newUsings = nsDecl.Usings.RemoveAt(index);

                if (hasComments)
                {
                    if (index < newUsings.Count)
                    {
                        var nextUsing = newUsings[index];
                        var preservedLeading = CombineLeadingTrivia(leadingTrivia, nextUsing.GetLeadingTrivia());
                        newUsings = newUsings.Replace(nextUsing, nextUsing.WithLeadingTrivia(preservedLeading));
                    }
                    else if (nsDecl.Members.Count > 0)
                    {
                        var firstMember = nsDecl.Members[0];
                        var preservedLeading = CombineLeadingTrivia(leadingTrivia, firstMember.GetLeadingTrivia());
                        nsDecl = nsDecl.WithMembers(nsDecl.Members.Replace(firstMember, firstMember.WithLeadingTrivia(preservedLeading)));
                    }
                }

                return root.ReplaceNode(nsDecl, nsDecl.WithUsings(newUsings));
            }

            return root.RemoveNode(usingDirective, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
        }

        private static SyntaxTriviaList CombineLeadingTrivia(SyntaxTriviaList leadingTrivia, SyntaxTriviaList nextLeadingTrivia)
        {
            var list = new List<SyntaxTrivia>(leadingTrivia);
            while (list.Count > 0 && list[list.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                list.RemoveAt(list.Count - 1);
            }
            if (list.Count > 0 && !list[list.Count - 1].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                list.Add(SyntaxFactory.CarriageReturnLineFeed);
            }
            return SyntaxFactory.TriviaList(list.Concat(nextLeadingTrivia));
        }

        private static string GeneratePortalSource(string topLevelNamespace, List<IMethodSymbol> linqMethods)
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

            var sortedMethods = linqMethods.OrderBy(static m => m.Name, StringComparer.Ordinal)
                .ThenBy(static m => m.Parameters.Length)
                .ToList();

            for (int i = 0, count = sortedMethods.Count; i < count; i++)
            {
                var method = sortedMethods[i];
                var methodDecl = GenerateMethodForwarder(method, indent + "    ");
                sb.AppendLine(methodDecl);
                if (i < count - 1)
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine($"{indent}}}");

            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static SyntaxNode MergePortalDocument(
            SyntaxNode existingRoot,
            SemanticModel? existingModel,
            string topLevelNamespace,
            List<IMethodSymbol> linqMethods)
        {
            ClassDeclarationSyntax? classDecl = null;
            foreach (var node in existingRoot.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax cls && cls.Identifier.Text == "LinqPortal")
                {
                    classDecl = cls;
                    break;
                }
            }

            if (classDecl == null)
            {
                return existingRoot;
            }

            bool hasNamespace = !string.IsNullOrEmpty(topLevelNamespace);
            string indent = hasNamespace ? "        " : "    ";

            // Resolve existing methods using Roslyn semantic model / symbols if available, else syntax
            var existingMethods = new List<(string Name, int ParamCount)>();
            foreach (var member in classDecl.Members)
            {
                if (member is MethodDeclarationSyntax methodSyntax)
                {
                    var symbol = existingModel?.GetDeclaredSymbol(methodSyntax);
                    if (symbol != null)
                    {
                        existingMethods.Add((symbol.Name, symbol.Parameters.Length));
                    }
                    else
                    {
                        existingMethods.Add((methodSyntax.Identifier.Text, methodSyntax.ParameterList.Parameters.Count));
                    }
                }
            }

            var newMethodsToAdd = new List<IMethodSymbol>();
            for (int i = 0, count = linqMethods.Count; i < count; i++)
            {
                var m = linqMethods[i];
                bool alreadyExists = false;
                for (int j = 0, eCount = existingMethods.Count; j < eCount; j++)
                {
                    var em = existingMethods[j];
                    if (em.Name == m.Name && em.ParamCount == m.Parameters.Length)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    newMethodsToAdd.Add(m);
                }
            }

            if (newMethodsToAdd.Count == 0)
            {
                return existingRoot;
            }

            newMethodsToAdd = newMethodsToAdd.OrderBy(static m => m.Name, StringComparer.Ordinal)
                .ThenBy(static m => m.Parameters.Length)
                .ToList();

            var currentMembers = classDecl.Members;

            // Insert each method into class members determined by alphabetical order of method name
            for (int i = 0, count = newMethodsToAdd.Count; i < count; i++)
            {
                var newMethod = newMethodsToAdd[i];
                var forwarderText = "\n" + GenerateMethodForwarder(newMethod, indent) + "\n";
                var methodSyntax = SyntaxFactory.ParseMemberDeclaration(forwarderText);
                if (methodSyntax == null) continue;

                int insertIndex = currentMembers.Count;
                for (int mIdx = 0, mCount = currentMembers.Count; mIdx < mCount; mIdx++)
                {
                    if (currentMembers[mIdx] is MethodDeclarationSyntax existingM)
                    {
                        int cmp = string.Compare(newMethod.Name, existingM.Identifier.Text, StringComparison.Ordinal);
                        if (cmp < 0)
                        {
                            insertIndex = mIdx;
                            break;
                        }
                    }
                }

                currentMembers = currentMembers.Insert(insertIndex, methodSyntax);
            }

            var newClassDecl = classDecl.WithMembers(currentMembers);
            return existingRoot.ReplaceNode(classDecl, newClassDecl);
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
