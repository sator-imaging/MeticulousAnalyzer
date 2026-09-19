// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SatorImaging.MeticulousAnalyzer.Analysis;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LambdaStaticCodeFixProvider)), Shared]
    public sealed class LambdaStaticCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(
                LambdaAnalyzer.RuleId_LambdaCanBeStatic,
                LambdaAnalyzer.RuleId_InefficientDelegateDeclaration
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

                if (diagnostic.Id == LambdaAnalyzer.RuleId_LambdaCanBeStatic)
                {
                    var lambda = node.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().FirstOrDefault();
                    if (lambda == null) continue;

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Add 'static' keyword",
                            createChangedDocument: c => AddStaticModifierAsync(context.Document, lambda, c),
                            equivalenceKey: "AddStaticModifier"),
                        diagnostic);
                }
                else if (diagnostic.Id == LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    if (semanticModel == null) continue;

                    var operation = semanticModel.GetOperation(node, context.CancellationToken);
                    if (operation == null) continue;

                    // Unwrap conversion
                    operation = operation.UnwrapConversion();
                    if (operation is IDelegateCreationOperation del) operation = del.Target;
                    operation = operation.UnwrapConversion();

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
            var paramCount = method.Parameters.Length;
            var parameters = new ParameterSyntax[paramCount];
            var arguments = new ArgumentSyntax[paramCount];

            for (int i = 0; i < paramCount; i++)
            {
                var p = method.Parameters[i];
                var name = string.IsNullOrEmpty(p.Name) ? "arg" + (i + 1) : p.Name;
                var rawName = name.StartsWith("@") ? name.Substring(1) : name;
                var kind = SyntaxFacts.GetKeywordKind(rawName);
                if (kind == SyntaxKind.None)
                {
                    kind = SyntaxFacts.GetContextualKeywordKind(rawName);
                }

                var token = (kind != SyntaxKind.None || name.StartsWith("@"))
                    ? SyntaxFactory.Identifier(SyntaxFactory.TriviaList(), kind != SyntaxKind.None ? kind : SyntaxKind.IdentifierToken, "@" + rawName, rawName, SyntaxFactory.TriviaList())
                    : SyntaxFactory.Identifier(rawName);

                var paramSyntax = SyntaxFactory.Parameter(token);

                if (p.RefKind != RefKind.None)
                {
                    var typeName = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    paramSyntax = paramSyntax.WithType(SyntaxFactory.ParseTypeName(typeName));
                }

                switch (p.RefKind)
                {
                    case RefKind.Ref:
                        paramSyntax = paramSyntax.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
                        break;
                    case RefKind.Out:
                        paramSyntax = paramSyntax.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
                        break;
                    case RefKind.In:
                        paramSyntax = paramSyntax.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
                        break;
                }

                parameters[i] = paramSyntax;

                var identifierExpr = SyntaxFactory.IdentifierName(token);

                arguments[i] = p.RefKind switch
                {
                    RefKind.Ref => SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), identifierExpr),
                    RefKind.Out => SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword), identifierExpr),
                    RefKind.In => SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.InKeyword), identifierExpr),
                    _ => SyntaxFactory.Argument(identifierExpr)
                };
            }

            var lambdaParameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));

            var nodeToReplace = node;
            while (nodeToReplace.Parent is ParenthesizedExpressionSyntax parenthesized)
            {
                nodeToReplace = parenthesized;
            }

            var unwrappedNode = nodeToReplace is ExpressionSyntax expr ? expr.UnwrapParentheses() : nodeToReplace;

            var methodAccess = (unwrappedNode is ExpressionSyntax exprAccess ? exprAccess : SyntaxFactory.IdentifierName(method.Name))
                .WithLeadingTrivia(SyntaxTriviaList.Empty)
                .WithTrailingTrivia(SyntaxTriviaList.Empty);

            var invocation = SyntaxFactory.InvocationExpression(methodAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            var staticLambda = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space)),
                lambdaParameters,
                SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken),
                block: null,
                invocation
            ).WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
             .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia());

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            var newRoot = root!.ReplaceNode(nodeToReplace, staticLambda);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddStaticModifierAsync(Document document, LambdaExpressionSyntax lambda, CancellationToken cancellationToken)
        {
            var leadingTrivia = lambda.GetLeadingTrivia();
            var lambdaWithoutLeading = lambda.WithLeadingTrivia(SyntaxTriviaList.Empty);

            LambdaExpressionSyntax newLambda;
            if (lambdaWithoutLeading is SimpleLambdaExpressionSyntax simple)
            {
                var newModifiers = simple.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space));
                newLambda = simple.WithModifiers(newModifiers);
            }
            else if (lambdaWithoutLeading is ParenthesizedLambdaExpressionSyntax parenthesized)
            {
                var newModifiers = parenthesized.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space));
                newLambda = parenthesized.WithModifiers(newModifiers);
            }
            else
            {
                return document;
            }

            newLambda = newLambda.WithLeadingTrivia(leadingTrivia);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            var newRoot = root!.ReplaceNode(lambda, newLambda);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
