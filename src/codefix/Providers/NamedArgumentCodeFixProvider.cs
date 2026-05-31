// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamedArgumentCodeFixProvider)), Shared]
    public sealed class NamedArgumentCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(ArgumentAnalyzer.RuleId_LiteralArgument);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Properties.TryGetValue("isParams", out var isParams) && isParams == "true")
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFix_NamedArgument,
                            createChangedDocument: c => AddNamedParamsArgumentAsync(context.Document, diagnostic, c),
                            equivalenceKey: CodeFixResources.CodeFix_NamedArgument),
                        diagnostic);
                }
                else
                {
                    var diagnosticSpan = diagnostic.Location.SourceSpan;

                    var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
                    var argumentNode = node?.AncestorsAndSelf().FirstOrDefault(static n => n is ArgumentSyntax or AttributeArgumentSyntax);
                    if (argumentNode == null) continue;

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFix_NamedArgument,
                            createChangedDocument: c => AddNamedArgumentAsync(context.Document, diagnostic, c),
                            equivalenceKey: CodeFixResources.CodeFix_NamedArgument),
                        diagnostic);
                }
            }
        }

        private async Task<Document> AddNamedParamsArgumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (root == null) return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (semanticModel == null) return document;

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the argument list that contains the params arguments.
            var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
            if (node == null) return document;

            // Navigate up to find the ArgumentListSyntax.
            var argumentList = node.AncestorsAndSelf().OfType<ArgumentListSyntax>().FirstOrDefault();
            if (argumentList == null) return document;

            // Determine which arguments are params arguments by checking which ones fall within the diagnostic span.
            var paramsArgs = new List<ArgumentSyntax>();
            foreach (var arg in argumentList.Arguments)
            {
                if (arg.Span.Start >= diagnosticSpan.Start && arg.Span.End <= diagnosticSpan.End)
                {
                    paramsArgs.Add(arg);
                }
            }

            if (paramsArgs.Count == 0) return document;

            // Get the parameter info from the invocation/creation.
            string? parameterName = null;
            ITypeSymbol? elementType = null;

            var invocationOrCreation = argumentList.Parent;
            if (invocationOrCreation != null)
            {
                var operation = semanticModel.GetOperation(invocationOrCreation, cancellationToken);
                IMethodSymbol? method = null;

                if (operation is IInvocationOperation invOp)
                {
                    method = invOp.TargetMethod;
                }
                else if (operation is IObjectCreationOperation ctorOp)
                {
                    method = ctorOp.Constructor;
                }

                if (method != null && method.Parameters.Length > 0)
                {
                    var lastParam = method.Parameters[method.Parameters.Length - 1];
                    if (lastParam.IsParams && lastParam.Type is IArrayTypeSymbol arrayType)
                    {
                        parameterName = lastParam.Name;
                        elementType = arrayType.ElementType;
                    }
                }
            }

            if (parameterName == null || elementType == null) return document;

            // Build the array creation expression: new ElementType[] { arg1, arg2, ... }
            var expressions = paramsArgs.Select(a => a.Expression.WithoutTrivia()).ToArray();
            var separatedList = SyntaxFactory.SeparatedList(
                expressions,
                Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space), expressions.Length - 1));

            var arrayCreation = SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                SyntaxFactory.ArrayType(
                    SyntaxFactory.ParseTypeName(elementType.ToMinimalDisplayString(semanticModel, argumentList.SpanStart)),
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression())))),
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                    separatedList,
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Space)));

            // Build name colon.
            var kind = SyntaxFacts.GetKeywordKind(parameterName);
            var identifierToken = kind == SyntaxKind.None
                ? SyntaxFactory.Identifier(parameterName)
                : SyntaxFactory.Identifier(SyntaxFactory.TriviaList(), kind, "@" + parameterName, parameterName, SyntaxFactory.TriviaList());

            var nameColon = SyntaxFactory.NameColon(
                SyntaxFactory.IdentifierName(identifierToken),
                SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));

            // Preserve leading trivia from the first params argument.
            var firstArg = paramsArgs[0];
            var leadingTrivia = firstArg.GetFirstToken().LeadingTrivia;

            var newArgument = SyntaxFactory.Argument(nameColon, SyntaxFactory.Token(SyntaxKind.None), arrayCreation)
                .WithLeadingTrivia(leadingTrivia);

            // Build new argument list: non-params args + the single new named params argument at the correct position.
            var newArgs = new List<ArgumentSyntax>();
            bool inserted = false;
            foreach (var arg in argumentList.Arguments)
            {
                if (paramsArgs.Contains(arg))
                {
                    if (!inserted)
                    {
                        newArgs.Add(newArgument);
                        inserted = true;
                    }
                }
                else
                {
                    newArgs.Add(arg);
                }
            }

            // Rebuild the separators (commas).
            var newArgList = SyntaxFactory.ArgumentList(
                argumentList.OpenParenToken,
                SyntaxFactory.SeparatedList(newArgs),
                argumentList.CloseParenToken);

            var newRoot = root.ReplaceNode(argumentList, newArgList);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddNamedArgumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (root == null) return document;

            var argumentSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(argumentSpan, getInnermostNodeForTie: true);
            var argumentNode = node?.AncestorsAndSelf().FirstOrDefault(static n => n is ArgumentSyntax or AttributeArgumentSyntax);
            if (argumentNode == null) return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (semanticModel == null) return document;

            string? parameterName = null;
            ExpressionSyntax? expression = null;
            if (argumentNode is ArgumentSyntax argument)
            {
                var argOp = semanticModel.GetOperation(argument, cancellationToken) as IArgumentOperation;
                parameterName = argOp?.Parameter?.Name;
                expression = argument.Expression;
            }
            else if (argumentNode is AttributeArgumentSyntax attrArg)
            {
                expression = attrArg.Expression;
                if (attrArg.Parent is AttributeArgumentListSyntax argList && argList.Parent is AttributeSyntax attr)
                {
                    var symbol = semanticModel.GetSymbolInfo(attr, cancellationToken).Symbol as IMethodSymbol;
                    if (symbol != null)
                    {
                        int index = argList.Arguments.IndexOf(attrArg);
                        if (index >= 0 && index < symbol.Parameters.Length)
                        {
                            parameterName = symbol.Parameters[index].Name;
                        }
                    }
                }
            }

            if (parameterName is not { Length: > 0 } || parameterName == "<unknown>" || expression == null) return document;
            var paramName = parameterName;

            if (!SyntaxFacts.IsValidIdentifier(paramName)) return document;

            var firstToken = argumentNode.GetFirstToken();
            var leadingTrivia = firstToken.LeadingTrivia;

            var kind = SyntaxFacts.GetKeywordKind(paramName);
            var identifierToken = kind == SyntaxKind.None
                ? SyntaxFactory.Identifier(paramName)
                : SyntaxFactory.Identifier(SyntaxFactory.TriviaList(), kind, "@" + paramName, paramName, SyntaxFactory.TriviaList());

            var nameColon = SyntaxFactory.NameColon(
                SyntaxFactory.IdentifierName(identifierToken),
                SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space)
            ).WithLeadingTrivia(leadingTrivia);

            SyntaxNode? newNode = null;
            if (argumentNode is ArgumentSyntax arg)
            {
                newNode = arg.WithNameColon(nameColon)
                             .WithExpression(expression.WithLeadingTrivia(SyntaxTriviaList.Empty));
            }
            else if (argumentNode is AttributeArgumentSyntax aArg)
            {
                newNode = aArg.WithNameColon(nameColon)
                              .WithExpression(expression.WithLeadingTrivia(SyntaxTriviaList.Empty));
            }

            if (newNode == null) return document;

            var newRoot = root.ReplaceNode(argumentNode, newNode);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
