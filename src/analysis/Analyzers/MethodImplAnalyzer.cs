// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MethodImplAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_AggressiveInliningOnPublicMember = "SMA7003";

        private static readonly DiagnosticDescriptor Rule_AggressiveInliningOnPublicMember = new(
            RuleId_AggressiveInliningOnPublicMember,
            new LocalizableResourceString(nameof(Resources.SMA7003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7003_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_AggressiveInliningOnPublicMember);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            if (context.Symbol is not IMethodSymbol method)
                return;

            if (method.DeclaredAccessibility != Accessibility.Public)
                return;

            // Find the MethodImplAttribute that has AggressiveInlining
            AttributeData? methodImplAttr = null;
            foreach (var attribute in method.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.MethodImplAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        var arg = attribute.ConstructorArguments[0];
                        if (arg.Value != null)
                        {
                            try
                            {
                                var val = System.Convert.ToInt32(arg.Value);
                                if ((val & 256) != 0) // 256 is AggressiveInlining
                                {
                                    methodImplAttr = attribute;
                                    break;
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }

            if (methodImplAttr == null)
                return;

            // Report diagnostic on the attribute syntax if available, otherwise fallback to method identifier
            var attributeSyntax = methodImplAttr.ApplicationSyntaxReference?.GetSyntax();
            if (attributeSyntax != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_AggressiveInliningOnPublicMember,
                    attributeSyntax.GetLocation(),
                    method.ToDiagnosticMessageName()));
            }
            else
            {
                foreach (var syntaxRef in method.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax();
                    var location = GetIdentifierLocation(syntax);
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_AggressiveInliningOnPublicMember,
                        location,
                        method.ToDiagnosticMessageName()));
                }
            }
        }

        private static Location GetIdentifierLocation(SyntaxNode syntax)
        {
            if (syntax is MethodDeclarationSyntax methodDecl)
                return methodDecl.Identifier.GetLocation();
            if (syntax is ConstructorDeclarationSyntax ctorDecl)
                return ctorDecl.Identifier.GetLocation();
            if (syntax is AccessorDeclarationSyntax accessorDecl)
                return accessorDecl.Keyword.GetLocation();
            if (syntax is PropertyDeclarationSyntax propDecl)
                return propDecl.Identifier.GetLocation();
            if (syntax is IndexerDeclarationSyntax indexerDecl)
                return indexerDecl.ThisKeyword.GetLocation();

            return syntax.GetLocation();
        }
    }
}
