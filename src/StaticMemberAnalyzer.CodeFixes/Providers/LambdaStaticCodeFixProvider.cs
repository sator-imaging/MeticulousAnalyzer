// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LambdaStaticCodeFixProvider)), Shared]
    public sealed class LambdaStaticCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(
                LambdaAnalyzer.RuleId_ImplicitConversionToDelegate
            );
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
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node == null) continue;

                if (diagnostic.Id == LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    if (semanticModel == null) continue;

                    var operation = semanticModel.GetOperation(node, context.CancellationToken);
                    if (operation == null) continue;

                    // Unwrap conversion
                    while (operation is IConversionOperation conv) operation = conv.Operand;
                    if (operation is IDelegateCreationOperation del) operation = del.Target;
                    while (operation is IConversionOperation conv) operation = conv.Operand;

                    if (operation is IMethodReferenceOperation methodRef && methodRef.Method.IsStatic)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Wrap with static lambda",
                                createChangedDocument: c => WrapWithStaticLambdaAsync(context.Document, node, methodRef.Method, c),
                                equivalenceKey: "WrapWithStaticLambda"),
                            diagnostic);
                    }
                }
            }
        }

        private async Task<Document> WrapWithStaticLambdaAsync(Document document, SyntaxNode node, IMethodSymbol method, CancellationToken cancellationToken)
        {
            var parameters = method.Parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))).ToArray();
            var arguments = method.Parameters.Select(p => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Name))).ToArray();

            var lambdaParameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
            var methodAccess = node is ExpressionSyntax expr ? expr : SyntaxFactory.IdentifierName(method.Name);
            var invocation = SyntaxFactory.InvocationExpression(methodAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            var staticLambda = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space)),
                lambdaParameters,
                SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken),
                block: null,
                invocation
            );

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            var newRoot = root!.ReplaceNode(node, staticLambda);
            return document.WithSyntaxRoot(newRoot);
        }

    }
}
