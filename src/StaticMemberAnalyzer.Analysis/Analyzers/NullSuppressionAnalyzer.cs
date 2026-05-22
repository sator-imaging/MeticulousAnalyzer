// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullSuppressionAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_NullSuppression = "SMA8002";

        private static readonly DiagnosticDescriptor Rule_NullSuppression = new(
            RuleId_NullSuppression,
            new LocalizableResourceString(nameof(Resources.SMA8002_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8002_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8002_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_NullSuppression);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSuppressNullableWarning, SyntaxKind.SuppressNullableWarningExpression);
        }

        private static void AnalyzeSuppressNullableWarning(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not PostfixUnaryExpressionSyntax node || !node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                return;
            }

            // (((foo)))! is allowed.
            // Check if operand is parenthesized at least 3 times.
            var operand = node.Operand;
            int depth = 0;
            while (operand is ParenthesizedExpressionSyntax parenthesized)
            {
                depth++;
                operand = parenthesized.Expression;
            }

            if (depth < 3)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_NullSuppression,
                    node.GetLocation(),
                    operand.ToString()));
            }
        }
    }
}
