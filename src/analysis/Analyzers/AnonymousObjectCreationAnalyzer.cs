// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AnonymousObjectCreationAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_AnonymousObject = "SMA7040";

        private static readonly DiagnosticDescriptor Rule_AnonymousObject = new(
            RuleId_AnonymousObject,
            new LocalizableResourceString(nameof(Resources.SMA7040_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7040_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(AnonymousObjectCreationAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7040_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_AnonymousObject);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeAnonymousObjectCreation, SyntaxKind.AnonymousObjectCreationExpression);
        }

        private static void AnalyzeAnonymousObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AnonymousObjectCreationExpressionSyntax anonymousObject)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_AnonymousObject, anonymousObject.GetLocation()));
        }
    }
}
