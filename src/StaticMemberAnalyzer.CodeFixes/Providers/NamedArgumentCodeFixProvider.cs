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
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
            if (node == null) return;

            var argumentNode = node.AncestorsAndSelf().FirstOrDefault(n => n is ArgumentSyntax || n is AttributeArgumentSyntax);
            if (argumentNode == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFix_NamedArgument,
                    createChangedDocument: c => AddNamedArgumentAsync(context.Document, argumentNode, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFix_NamedArgument)),
                diagnostic);
        }

        private async Task<Document> AddNamedArgumentAsync(Document document, SyntaxNode argumentNode, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return document;

            string? parameterName = null;
            if (argumentNode is ArgumentSyntax argument)
            {
                var argOp = semanticModel.GetOperation(argument, cancellationToken) as IArgumentOperation;
                parameterName = argOp?.Parameter?.Name;
            }
            else if (argumentNode is AttributeArgumentSyntax attrArg)
            {
                if (attrArg.Parent is AttributeArgumentListSyntax argList && argList.Parent is AttributeSyntax attr)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(attr, cancellationToken);
                    var symbol = symbolInfo.Symbol as IMethodSymbol;
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

            if (string.IsNullOrEmpty(parameterName)) return document;

            var firstToken = argumentNode.GetFirstToken();
            var leadingTrivia = firstToken.LeadingTrivia;

            var nameColon = SyntaxFactory.NameColon(
                SyntaxFactory.IdentifierName(parameterName!),
                SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space)
            ).WithLeadingTrivia(leadingTrivia);

            SyntaxNode? newNode = null;
            if (argumentNode is ArgumentSyntax arg)
            {
                newNode = arg.WithNameColon(nameColon)
                             .WithExpression(arg.Expression.WithLeadingTrivia(SyntaxTriviaList.Empty));
            }
            else if (argumentNode is AttributeArgumentSyntax aArg)
            {
                newNode = aArg.WithNameColon(nameColon)
                              .WithExpression(aArg.Expression.WithLeadingTrivia(SyntaxTriviaList.Empty));
            }

            if (newNode == null) return document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var newRoot = root.ReplaceNode(argumentNode, newNode);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
