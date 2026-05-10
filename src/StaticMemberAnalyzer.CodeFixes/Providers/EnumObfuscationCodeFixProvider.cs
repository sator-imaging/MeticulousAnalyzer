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
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(continueOnCapturedContext: false) as CompilationUnitSyntax;
            if (root == null)
                return;
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (model == null)
                return;

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            // NOTE: this method is called when Alt+Enter is pressed on source code where diagnostic reported.
            //       only need to provide codefix for first one.
            var diagnostic = context.Diagnostics.First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFix_EnumObfuscation,
                    createChangedDocument: token => ExcludeEnumFromObfuscation(diagnostic, context.Document, model, root, token),
                    equivalenceKey: nameof(CodeFixResources.CodeFix_EnumObfuscation)),
                diagnostic);
        }


        readonly static string ATTR_OBFUSCATION_SHORT_NAME
            = nameof(ObfuscationAttribute).Substring(startIndex: 0, length: (nameof(ObfuscationAttribute).Length - nameof(Attribute).Length));

        private async Task<Document> ExcludeEnumFromObfuscation(Diagnostic diagnostic,
                                                                Document document,
                                                                SemanticModel model,
                                                                CompilationUnitSyntax root,
                                                                CancellationToken token
            )
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var typeDecl = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<EnumDeclarationSyntax>().FirstOrDefault();
            if (typeDecl == null || !typeDecl.Span.IntersectsWith(diagnosticSpan))
                return document;

            // Get the symbol representing the type to be renamed.
            var typeSymbol = model.GetDeclaredSymbol(typeDecl, token);
            if (typeSymbol == null)
                return document;

            // add using statement
            const string NS_OBFUSCATION = nameof(System) + "." + nameof(System.Reflection);

            var updatedUsings = root.Usings;
            if (!HasObfuscationUsing(updatedUsings, NS_OBFUSCATION))
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
                .FirstOrDefault(attr =>
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
                                            expression: SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                                        ),
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.NameEquals(
                                                SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.ApplyToMembers))
                                            ),
                                            nameColon: null,
                                            expression: SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
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
                AttributeSyntax? foundAttr = null;
                foreach (var list in typeDecl.AttributeLists)
                {
                    foreach (var x in list.Attributes)
                    {
                        var name = x.Name.ToString();
                        if (name == ATTR_OBFUSCATION_SHORT_NAME
                         || name.EndsWith(ATTR_OBFUSCATION_SHORT_NAME, StringComparison.Ordinal)
                         || name == nameof(ObfuscationAttribute)
                         || name.EndsWith(nameof(ObfuscationAttribute), StringComparison.Ordinal)
                        )
                        {
                            foundAttr = x;
                            break;
                        }
                    }
                    if (foundAttr != null) break;
                }

                if (foundAttr == null)
                    return document;

                // NOTE: to prevent error on no parentheses syntax --> `[Obfuscation]` (no '()' at end)
                var updatedArgList = foundAttr.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
                var updatedArgsList = new System.Collections.Generic.List<AttributeArgumentSyntax>();
                foreach (var x in updatedArgList.Arguments)
                {
                    if (x.NameEquals?.Name.ToString() is not nameof(ObfuscationAttribute.Exclude) and not nameof(ObfuscationAttribute.ApplyToMembers))
                    {
                        updatedArgsList.Add(x);
                    }
                }

                // 1st & 2nd
                updatedArgsList.Insert(index: 0, item: SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(
                        SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.Exclude))
                    ),
                    nameColon: null,
                    expression: SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                ));
                updatedArgsList.Insert(index: 1, item: SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(
                        SyntaxFactory.IdentifierName(nameof(ObfuscationAttribute.ApplyToMembers))
                    ),
                    nameColon: null,
                    expression: SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                ));

                root = root.ReplaceNode(
                    typeDecl,
                    typeDecl.ReplaceNode(
                        foundAttr,
                        foundAttr.WithArgumentList(
                            updatedArgList.WithArguments(
                                SyntaxFactory.SeparatedList(updatedArgsList)
                            )
                        )
                    )
                );
            }

            return document.WithSyntaxRoot(root.WithUsings(updatedUsings));
        }

        private static bool HasObfuscationUsing(SyntaxList<UsingDirectiveSyntax> usings, string ns)
        {
            foreach (var u in usings)
            {
                if (u.Name.Span.Length == ns.Length && u.Name.ToString() == ns) return true;
            }
            return false;
        }
    }
}
