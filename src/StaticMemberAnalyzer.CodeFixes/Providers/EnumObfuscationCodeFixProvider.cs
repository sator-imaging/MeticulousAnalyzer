// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumObfuscationCodeFixProvider)), Shared]
    public sealed class EnumObfuscationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(EnumAnalyzer.RuleId_EnumObfuscation);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
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
                var typeDecl = node?.AncestorsAndSelf().OfType_FirstOrDefault<EnumDeclarationSyntax>();
                if (typeDecl == null || !typeDecl.Span.IntersectsWith(diagnosticSpan))
                    continue;

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFix_EnumObfuscation,
                        createChangedDocument: token => ExcludeEnumFromObfuscation(diagnostic, context.Document, token),
                        equivalenceKey: CodeFixResources.CodeFix_EnumObfuscation),
                    diagnostic);
            }
        }


        readonly static string ATTR_OBFUSCATION_SHORT_NAME
            = nameof(ObfuscationAttribute).Substring(startIndex: 0, (nameof(ObfuscationAttribute).Length - nameof(Attribute).Length));

        private async Task<Document> ExcludeEnumFromObfuscation(Diagnostic diagnostic,
                                                                Document document,
                                                                CancellationToken token
            )
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(continueOnCapturedContext: false) as CompilationUnitSyntax;
            if (root == null)
                return document;
            var model = await document.GetSemanticModelAsync(token).ConfigureAwait(continueOnCapturedContext: false);
            if (model == null)
                return document;

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var node = root.FindNode(diagnosticSpan);
            var typeDecl = node?.AncestorsAndSelf().OfType_FirstOrDefault<EnumDeclarationSyntax>();
            if (typeDecl == null || !typeDecl.Span.IntersectsWith(diagnosticSpan))
                return document;

            // Get the symbol representing the type to be renamed.
            var typeSymbol = model.GetDeclaredSymbol(typeDecl, token);
            if (typeSymbol == null)
                return document;

            // add using statement
            const string NS_OBFUSCATION = nameof(System) + "." + nameof(System.Reflection);

            var updatedUsings = root.Usings;
            if (!updatedUsings.Any(static x => x.Name.Span.Length == NS_OBFUSCATION.Length && x.Name.ToString() == NS_OBFUSCATION))
            {
                updatedUsings = updatedUsings.Add(
                    SyntaxFactory.UsingDirective(
                        // NOTE: QualifiedName is required to pass unit test
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName(nameof(System)),
                            SyntaxFactory.IdentifierName(nameof(System.Reflection))
                        )
                    )
                );
            }

            var attr = typeSymbol.GetAttributes()
                .FirstOrDefault(static attr =>
                {
                    if (attr.AttributeClass?.Name == nameof(ObfuscationAttribute)
                     && attr.AttributeClass.ToString() == NS_OBFUSCATION + "." + nameof(ObfuscationAttribute))
                    {
                        return true;
                    }
                    return false;
                });

            if (attr == null)
            {
                var attributes = typeDecl.AttributeLists.Add(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName(ATTR_OBFUSCATION_SHORT_NAME),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(new AttributeArgumentSyntax[]
                                    {
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.NameEquals(
                                                SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.Exclude))
                                            ),
                                            nameColon: null,
                                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                                        ),
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.NameEquals(
                                                SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.ApplyToMembers))
                                            ),
                                            nameColon: null,
                                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                                        ),
                                    })
                                )
                            )
                        )
                    )
                );

                root = root.ReplaceNode(typeDecl, typeDecl.WithAttributeLists(attributes));
            }
            // update existing
            else
            {
                var foundAttr = typeDecl.AttributeLists
                    .SelectMany_FirstOrDefault(
                        static x => x.Attributes,
                        static x =>
                        {
                            var name = x.Name.ToString();
                            if (name == ATTR_OBFUSCATION_SHORT_NAME
                             || name.EndsWith(ATTR_OBFUSCATION_SHORT_NAME, StringComparison.Ordinal)
                             || name == nameof(ObfuscationAttribute)
                             || name.EndsWith(nameof(ObfuscationAttribute), StringComparison.Ordinal)
                            )
                            {
                                return true;
                            }
                            return false;
                        })
                        ;

                if (foundAttr == null)
                    return document;

                // NOTE: to prevent error on no parentheses syntax --> `[Obfuscation]` (no '()' at end)
                var updatedArgList = foundAttr.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
                var currentArgs = updatedArgList.Arguments.Where(static x =>
                {
                    return x.NameEquals?.Name.ToString() is not nameof(ObfuscationAttribute.Exclude) and not nameof(ObfuscationAttribute.ApplyToMembers);
                });

                var updatedArgs = currentArgs.ToImmutableArray()
                    //1st
                    .Insert(index: 0, SyntaxFactory.AttributeArgument(
                        SyntaxFactory.NameEquals(
                            SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.Exclude))
                        ),
                        nameColon: null,
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    ))
                    //2nd
                    .Insert(index: 1, SyntaxFactory.AttributeArgument(
                        SyntaxFactory.NameEquals(
                            SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.ApplyToMembers))
                        ),
                        nameColon: null,
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    ))
                    ;

                root = root.ReplaceNode(
                    typeDecl,
                    typeDecl.ReplaceNode(
                        foundAttr,
                        foundAttr.WithArgumentList(
                            updatedArgList.WithArguments(
                                SyntaxFactory.SeparatedList(updatedArgs)
                            )
                        )
                    )
                );
            }

            return document.WithSyntaxRoot(root.WithUsings(updatedUsings));
        }
    }
}
