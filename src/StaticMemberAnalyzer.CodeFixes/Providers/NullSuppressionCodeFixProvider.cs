// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullSuppressionCodeFixProvider)), Shared]
    public sealed class NullSuppressionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(NullSuppressionAnalyzer.RuleId_NullSuppression);
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
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var node = root.FindNode(diagnosticSpan);

                if (node is PostfixUnaryExpressionSyntax suppression && suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFix_NullSuppression,
                            createChangedDocument: c => AddParenthesesFenceAsync(context.Document, diagnostic, c),
                            equivalenceKey: CodeFixResources.CodeFix_NullSuppression),
                        diagnostic);
                }
            }
        }

        private async Task<Document> AddParenthesesFenceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (root == null) return document;

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            if (node is not PostfixUnaryExpressionSyntax suppression || !suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                return document;

            // Unwrap existing parentheses
            var operand = suppression.Operand;
            while (operand is ParenthesizedExpressionSyntax parenthesized)
            {
                operand = parenthesized.Expression;
            }

            // Add 3 parentheses
            var newOperand = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.ParenthesizedExpression(operand.WithoutTrivia())
                )
            ).WithTriviaFrom(suppression.Operand);

            var newSuppression = suppression.WithOperand(newOperand);
            var newRoot = root.ReplaceNode(suppression, newSuppression);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
