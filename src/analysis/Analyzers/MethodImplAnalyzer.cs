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

            var methodImplAttr = GetMethodImplAttributeWithAggressiveInlining(method);
            if (methodImplAttr == null)
                return;

            ReportWithFallback(context, method, methodImplAttr);
        }

        private static AttributeData? GetMethodImplAttributeWithAggressiveInlining(IMethodSymbol method)
        {
            foreach (var attribute in method.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "MethodImplAttribute" &&
                    attribute.AttributeClass.ToString() == "System.Runtime.CompilerServices.MethodImplAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        var arg = attribute.ConstructorArguments[0];
                        var valObj = arg.Value;
                        if (valObj is int intVal)
                        {
                            if ((intVal & 256) != 0) // 256 is AggressiveInlining
                            {
                                return attribute;
                            }
                        }
                        else if (valObj is short shortVal)
                        {
                            if ((shortVal & 256) != 0)
                            {
                                return attribute;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static void ReportWithFallback(SymbolAnalysisContext context, IMethodSymbol method, AttributeData methodImplAttr)
        {
            var attributeSyntax = methodImplAttr.ApplicationSyntaxReference?.GetSyntax();
            if (attributeSyntax != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_AggressiveInliningOnPublicMember,
                    attributeSyntax.GetLocation(),
                    method.ToDiagnosticMessageName()));
                return;
            }

            ReportFallback(context, method);
        }

        private static void ReportFallback(SymbolAnalysisContext context, IMethodSymbol method)
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

        private static Location GetIdentifierLocation(SyntaxNode syntax)
        {
            if (syntax is AccessorDeclarationSyntax accessorDecl)
                return accessorDecl.Keyword.GetLocation();
            if (syntax is IndexerDeclarationSyntax indexerDecl)
                return indexerDecl.ThisKeyword.GetLocation();
            if (syntax is MemberDeclarationSyntax memberDecl)
            {
                if (memberDecl is MethodDeclarationSyntax methodDecl)
                    return methodDecl.Identifier.GetLocation();
                if (memberDecl is ConstructorDeclarationSyntax ctorDecl)
                    return ctorDecl.Identifier.GetLocation();
                if (memberDecl is PropertyDeclarationSyntax propDecl)
                    return propDecl.Identifier.GetLocation();
            }

            return syntax.GetLocation();
        }
    }
}
