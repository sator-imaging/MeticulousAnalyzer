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
    public sealed class LinqOperationAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_LinqOperation = "SMA7050";
        public const string SuppressionComment = "// Allow linq operation";

        private static readonly DiagnosticDescriptor Rule_LinqOperation = new(
            RuleId_LinqOperation,
            new LocalizableResourceString(nameof(Resources.SMA7050_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7050_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(LinqOperationAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7050_MessageFormat), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_LinqOperation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        }

        private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            if (Core.IsTestProject(context.Compilation))
            {
                return;
            }

            if (context.Node is not UsingDirectiveSyntax usingDirective)
            {
                return;
            }

            if (usingDirective.Alias != null
                || usingDirective.StaticKeyword.Kind() != SyntaxKind.None)
            {
                return;
            }

            var name = usingDirective.Name;
            if (name is not QualifiedNameSyntax qualifiedName
                || qualifiedName.Left is not IdentifierNameSyntax systemName
                || systemName.Identifier.ValueText != "System"
                || qualifiedName.Right is not IdentifierNameSyntax linqName
                || linqName.Identifier.ValueText != "Linq")
            {
                return;
            }

            if (Core.IsSuppressedByComment(usingDirective, SuppressionComment))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_LinqOperation, usingDirective.GetLocation()));
        }
    }
}
