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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LambdaStaticCodeFixProvider)), Shared]
    public sealed class LambdaStaticCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(LambdaAnalyzer.RuleId_LambdaShouldBeStatic);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node == null) continue;

                var lambda = node.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().FirstOrDefault();
                if (lambda == null) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add 'static' keyword",
                        createChangedDocument: c => AddStaticModifierAsync(context.Document, lambda, c),
                        equivalenceKey: "AddStaticModifier"),
                    diagnostic);
            }
        }

        private async Task<Document> AddStaticModifierAsync(Document document, LambdaExpressionSyntax lambda, CancellationToken cancellationToken)
        {
            SyntaxTokenList newModifiers;
            if (lambda is SimpleLambdaExpressionSyntax simple)
            {
                newModifiers = simple.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space));
                var newLambda = simple.WithModifiers(newModifiers);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root!.ReplaceNode(simple, newLambda);
                return document.WithSyntaxRoot(newRoot);
            }
            else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized)
            {
                newModifiers = parenthesized.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space));
                var newLambda = parenthesized.WithModifiers(newModifiers);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root!.ReplaceNode(parenthesized, newLambda);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
    }
}
