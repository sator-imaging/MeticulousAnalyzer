// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MethodImplAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_MethodImplAggressiveInlining = "SMA7003";

        private static readonly DiagnosticDescriptor Rule_MethodImplAggressiveInlining = new(
            RuleId_MethodImplAggressiveInlining,
            new LocalizableResourceString(nameof(Resources.SMA7003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7003_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_MethodImplAggressiveInlining);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            if (context.Symbol is not IMethodSymbol methodSymbol)
                return;

            if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
                return;

            if (methodSymbol.IsImplicitlyDeclared)
                return;

            foreach (var attribute in methodSymbol.GetAttributes())
            {
                if (attribute.AttributeClass == null)
                    continue;

                var attrName = attribute.AttributeClass.ToDisplayString();
                if (attrName == "System.Runtime.CompilerServices.MethodImplAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        var arg = attribute.ConstructorArguments[0];
                        if (arg.Value != null)
                        {
                            var val = Convert.ToInt32(arg.Value);
                            if ((val & 256) != 0) // MethodImplOptions.AggressiveInlining
                            {
                                var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation()
                                    ?? (methodSymbol.Locations.Length > 0 ? methodSymbol.Locations[0] : Location.None);

                                context.ReportDiagnostic(Diagnostic.Create(
                                    Rule_MethodImplAggressiveInlining,
                                    location,
                                    methodSymbol.ToDiagnosticMessageName()));
                            }
                        }
                    }
                }
            }
        }
    }
}
