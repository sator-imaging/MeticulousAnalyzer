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
        public const string RuleId_LiteralBranchString = "SMA8022";
        public const string RuleId_LiteralBranchChar = "SMA8023";

        private const string SuppressionCommentPrefix = "/* Why: ";

        private static readonly DiagnosticDescriptor Rule_LiteralBranch = new(
            RuleId_LiteralBranch,
            new LocalizableResourceString(nameof(Resources.SMA8020_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8020_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(LiteralBranchAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8020_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_LiteralBranchZero = new(
            RuleId_LiteralBranchZero,
            new LocalizableResourceString(nameof(Resources.SMA8021_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8021_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(LiteralBranchAnalyzer),
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8021_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_LiteralBranchString = new(
            RuleId_LiteralBranchString,
            new LocalizableResourceString(nameof(Resources.SMA8022_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8022_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(LiteralBranchAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8022_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_LiteralBranchChar = new(
            RuleId_LiteralBranchChar,
            new LocalizableResourceString(nameof(Resources.SMA8023_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8023_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(LiteralBranchAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8023_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly char[] TrimCommentChars = new[] { '/', '*', ' ' };  // Ignore TAB, CR, LF, etc.

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_LiteralBranch, Rule_LiteralBranchZero, Rule_LiteralBranchString, Rule_LiteralBranchChar);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
            context.RegisterOperationAction(AnalyzeConstantPattern, OperationKind.ConstantPattern);
            context.RegisterOperationAction(AnalyzeRelationalPattern, OperationKind.RelationalPattern);
            context.RegisterOperationAction(AnalyzeSwitchCase, OperationKind.SwitchCase);
        }

        private static void AnalyzeBinary(OperationAnalysisContext context)
        {
            if (context.Operation is not IBinaryOperation binary)
                return;

            if (binary.OperatorKind is not (
                BinaryOperatorKind.Equals or
                BinaryOperatorKind.NotEquals or
                BinaryOperatorKind.LessThan or
                BinaryOperatorKind.LessThanOrEqual or
                BinaryOperatorKind.GreaterThan or
                BinaryOperatorKind.GreaterThanOrEqual))
            {
                return;
            }

            AnalyzeOperandForLiteral(context, binary.LeftOperand);
            AnalyzeOperandForLiteral(context, binary.RightOperand, leftOperand: binary.LeftOperand);
        }

        private static void AnalyzeConstantPattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IConstantPatternOperation pattern)
                return;

            AnalyzeOperandForLiteral(context, pattern.Value);
        }

        private static void AnalyzeRelationalPattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IRelationalPatternOperation pattern)
                return;

            AnalyzeOperandForLiteral(context, pattern.Value);
        }

        private static void AnalyzeSwitchCase(OperationAnalysisContext context)
        {
            if (context.Operation is not ISwitchCaseOperation switchCase)
                return;

            foreach (var clause in switchCase.Clauses)
            {
                if (clause is ISingleValueCaseClauseOperation singleValue)
                    AnalyzeOperandForLiteral(context, singleValue.Value);
            }
        }

        private static void AnalyzeOperandForLiteral(
            OperationAnalysisContext context,
            IOperation operand,
            IOperation? leftOperand = null)
        {
            // Unwrap interleaved conversions and unary +/- to reach the literal
            var current = operand;
            while (true)
            {
                if (current is IConversionOperation conv)
                    current = conv.Operand;
                else if (current is IUnaryOperation unary &&
                         (unary.OperatorKind == UnaryOperatorKind.Minus || unary.OperatorKind == UnaryOperatorKind.Plus))
                    current = unary.Operand;
                else
                    break;
            }

            if (current is not ILiteralOperation literalOp)
                return;

            if (!literalOp.ConstantValue.HasValue)
                return;

            var val = literalOp.ConstantValue.Value;

            // Allow true/false/null
            if (val == null || val is bool)
                return;

            // Find outermost syntax to report on (e.g. including unary minus for -1)
            // Start from the literal's own syntax and walk up through any unary +/- wrappers
            var outermostSyntax = literalOp.Syntax;
            while (outermostSyntax.Parent is PrefixUnaryExpressionSyntax prefix &&
                   (prefix.IsKind(SyntaxKind.UnaryMinusExpression) || prefix.IsKind(SyntaxKind.UnaryPlusExpression)))
            {
                outermostSyntax = prefix;
            }

            foreach (var trivia in outermostSyntax.GetTrailingTrivia())
            {
                if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    var text = trivia.ToString().TrimEnd(TrimCommentChars);
                    if (text.StartsWith(SuppressionCommentPrefix, StringComparison.OrdinalIgnoreCase))
                        return;
                }
            }

            if (val is string)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralBranchString,
                    outermostSyntax.GetLocation(),
                    outermostSyntax.ToString()));
            }
            else if (val is char)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralBranchChar,
                    outermostSyntax.GetLocation(),
                    outermostSyntax.ToString()));
            }
            else if (IsNumericZero(literalOp))
            {
                if (!IsInLoopCondition(outermostSyntax.Parent) &&
                    (leftOperand == null || !LeftSideHasMatchingMemberAccessSyntax(leftOperand)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_LiteralBranchZero,
                        outermostSyntax.GetLocation(),
                        outermostSyntax.ToString()));
                }
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralBranch,
                    outermostSyntax.GetLocation(),
                    outermostSyntax.ToString()));
            }
        }

        private static bool IsInLoopCondition(SyntaxNode? node)
        {
            if (node == null)
                return false;

            return node.Parent is ForStatementSyntax forStmt && forStmt.Condition == node
                || node.Parent is WhileStatementSyntax whileStmt && whileStmt.Condition == node
                || node.Parent is DoStatementSyntax doStmt && doStmt.Condition == node;
        }

        private static bool IsNumericZero(ILiteralOperation literalOp)
        {
            if (!literalOp.ConstantValue.HasValue) return false;
            var val = literalOp.ConstantValue.Value;
            if (val == null) return false;

            return val switch
            {
                int i => i == 0,
                float f => f == 0.0f,
                double d => d == 0.0,
                long l => l == 0,
                short s => s == 0,
                byte b => b == 0,
                uint u => u == 0,
                ulong ul => ul == 0,
                ushort us => us == 0,
                sbyte sb => sb == 0,
                decimal m => m == 0m,
                _ => false
            };
        }

        private static bool IsMatchingMemberName(string name)
        {
            return name.IndexOf("Length", StringComparison.Ordinal) >= 0 ||
                   name.IndexOf("Count", StringComparison.Ordinal) >= 0 ||
                   name.IndexOf("IndexOf", StringComparison.Ordinal) >= 0;
        }

        private static bool LeftSideHasMatchingMemberAccessSyntax(IOperation leftOperand)
        {
            string? opName = leftOperand switch
            {
                IMemberReferenceOperation memberRef => memberRef.Member?.Name,
                IInvocationOperation invocation => invocation.TargetMethod?.Name,
                IDynamicMemberReferenceOperation dynamicRef => dynamicRef.MemberName,
                _ => null
            };

            if (opName != null && IsMatchingMemberName(opName))
            {
                return true;
            }

            return leftOperand.Syntax != null && HasMatchingMemberAccessSyntax(leftOperand.Syntax);
        }

        private static bool HasMatchingMemberAccessSyntax(SyntaxNode syntax)
        {
            foreach (var node in syntax.DescendantNodesAndSelf())
            {
                string? name = node switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
                    _ => null
                };

                if (name != null && IsMatchingMemberName(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
