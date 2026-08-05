// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParamsArgumentCodeFixProvider)), Shared]
    public sealed class ParamsArgumentCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation);
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
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Convert to explicit array allocation",
                        createChangedDocument: c => ConvertToExplicitArrayAllocationAsync(context.Document, diagnostic, c),
                        equivalenceKey: "Convert to explicit array allocation"),
                    diagnostic);
            }
        }

        private async Task<Document> ConvertToExplicitArrayAllocationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
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
            var argumentList = node.AncestorsAndSelf().FirstOrDefault(static n => n is ArgumentListSyntax) as ArgumentListSyntax;
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
                        elementType = arrayType.ElementType;
                    }
                }
            }

            if (elementType == null) return document;

            // Build the array creation expression: new ElementType[] { arg1, arg2, ... }
            // Preserve trivia on expressions except leading trivia on the first (moved to newArgument).
            var expressions = paramsArgs.Select((a, i) => i == 0 ? a.Expression.WithLeadingTrivia(SyntaxTriviaList.Empty) : a.Expression).ToArray();

            // Preserve original separators (commas and their trivia) between params arguments.
            var firstParamsIndex = argumentList.Arguments.IndexOf(paramsArgs[0]);
            var arraySeparators = new List<SyntaxToken>();
            for (int i = 0; i < paramsArgs.Count - 1; i++)
            {
                arraySeparators.Add(argumentList.Arguments.GetSeparator(firstParamsIndex + i));
            }
            var separatedList = SyntaxFactory.SeparatedList(expressions, arraySeparators);

            var typeSyntax = SyntaxFactory.ParseTypeName(elementType.ToMinimalDisplayString(semanticModel, argumentList.SpanStart));
            var arrayTypeSyntax = SyntaxFactory.ArrayType(typeSyntax)
                .AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression())));

            var arrayCreation = SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                arrayTypeSyntax,
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                    separatedList,
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Space)));

            // Preserve leading trivia from the first params argument.
            var firstArg = paramsArgs[0];
            var leadingTrivia = firstArg.GetFirstToken().LeadingTrivia;

            var newArgument = SyntaxFactory.Argument(arrayCreation)
                .WithLeadingTrivia(leadingTrivia);

            // Replace params arguments with the single array argument,
            // preserving all preceding and trailing arguments and their separators.
            var arguments = argumentList.Arguments;
            for (int i = paramsArgs.Count - 1; i > 0; i--)
            {
                arguments = arguments.RemoveAt(firstParamsIndex + i);
            }
            arguments = arguments.Replace(arguments[firstParamsIndex], newArgument);

            var newArgList = argumentList.WithArguments(arguments);

            var newRoot = root.ReplaceNode(argumentList, newArgList);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
