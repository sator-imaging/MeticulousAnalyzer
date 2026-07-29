// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LiteralBranchAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_LiteralBranch = "SMA8020";
        public const string RuleId_LiteralBranchZero = "SMA8021";

        private static readonly DiagnosticDescriptor Rule_LiteralBranch = new(
            RuleId_LiteralBranch,
            new LocalizableResourceString(nameof(Resources.SMA8020_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8020_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8020_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_LiteralBranchZero = new(
            RuleId_LiteralBranchZero,
            new LocalizableResourceString(nameof(Resources.SMA8021_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8021_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8021_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_LiteralBranch, Rule_LiteralBranchZero);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeLiteral, OperationKind.Literal);
        }

        private static void AnalyzeLiteral(OperationAnalysisContext context)
        {
            if (context.Operation is not ILiteralOperation literalOp)
            {
                return;
            }

            if (!IsInBranchContext(literalOp, out _, out bool isNegative))
            {
                return;
            }

            if (!literalOp.ConstantValue.HasValue)
            {
                return;
            }

            var val = literalOp.ConstantValue.Value;

            // Allow true/false/null
            if (val == null || val is bool)
            {
                return;
            }

            // Find outermost syntax to report on (e.g. including unary minus for -1)
            var outermostSyntax = literalOp.Syntax;
            while (outermostSyntax.Parent is PrefixUnaryExpressionSyntax prefix &&
                   (prefix.IsKind(SyntaxKind.UnaryMinusExpression) || prefix.IsKind(SyntaxKind.UnaryPlusExpression)))
            {
                outermostSyntax = prefix;
            }

            if (IsNumericZero(literalOp))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralBranchZero,
                    outermostSyntax.GetLocation(),
                    outermostSyntax.ToString()));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralBranch,
                    outermostSyntax.GetLocation(),
                    outermostSyntax.ToString()));
            }
        }

        private static bool IsInBranchContext(ILiteralOperation literalOp, out IOperation? branchContextOp, out bool isNegative)
        {
            branchContextOp = null;
            isNegative = false;

            var current = literalOp.Parent;
            while (current != null)
            {
                if (current is IConversionOperation)
                {
                    current = current.Parent;
                }
                else if (current is IUnaryOperation unary)
                {
                    if (unary.OperatorKind == UnaryOperatorKind.Minus)
                    {
                        isNegative = !isNegative;
                        current = current.Parent;
                    }
                    else if (unary.OperatorKind == UnaryOperatorKind.Plus)
                    {
                        current = current.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            if (current == null)
            {
                return false;
            }

            if (current is IBinaryOperation binary)
            {
                if (binary.OperatorKind is
                    BinaryOperatorKind.Equals or
                    BinaryOperatorKind.NotEquals or
                    BinaryOperatorKind.LessThan or
                    BinaryOperatorKind.LessThanOrEqual or
                    BinaryOperatorKind.GreaterThan or
                    BinaryOperatorKind.GreaterThanOrEqual)
                {
                    branchContextOp = binary;
                    return true;
                }
            }
            else if (current is IConstantPatternOperation pattern)
            {
                branchContextOp = pattern;
                return true;
            }
            else if (current is ISingleValueCaseClauseOperation caseClause)
            {
                branchContextOp = caseClause;
                return true;
            }

            return false;
        }

        private static bool IsNumericZero(ILiteralOperation literalOp)
        {
            if (!literalOp.ConstantValue.HasValue) return false;
            var val = literalOp.ConstantValue.Value;
            if (val == null) return false;

            if (literalOp.Syntax.Span.Length <= 2)
            {
                var text = literalOp.Syntax.ToString();
                if (int.TryParse(text, out var parsed) && parsed == 0)
                {
                    return true;
                }
            }

            return val switch
            {
                int i => i == 0,
                long l => l == 0,
                uint u => u == 0,
                ulong ul => ul == 0,
                double d => d == 0.0,
                float f => f == 0.0f,
                decimal m => m == 0m,
                short s => s == 0,
                ushort us => us == 0,
                byte b => b == 0,
                sbyte sb => sb == 0,
                _ => false
            };
        }
    }
}
