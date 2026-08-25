// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using SatorImaging.MeticulousAnalyzer.Analysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        private static readonly DiagnosticDescriptor Rule_LiteralBranchString = new(
            RuleId_LiteralBranchString,
            new LocalizableResourceString(nameof(Resources.SMA8022_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8022_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8022_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_LiteralBranchChar = new(
            RuleId_LiteralBranchChar,
            new LocalizableResourceString(nameof(Resources.SMA8023_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8023_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
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

            AnalyzeOperandForLiteral(context, binary.LeftOperand, binary.RightOperand);
            AnalyzeOperandForLiteral(context, binary.RightOperand, binary.LeftOperand);
        }

        private static void AnalyzeConstantPattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IConstantPatternOperation pattern)
                return;

            var comparand = FindComparandForPattern(pattern);
            AnalyzeOperandForLiteral(context, pattern.Value, comparand);
        }

        private static void AnalyzeRelationalPattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IRelationalPatternOperation pattern)
                return;

            var comparand = FindComparandForPattern(pattern);
            AnalyzeOperandForLiteral(context, pattern.Value, comparand);
        }

        private static void AnalyzeSwitchCase(OperationAnalysisContext context)
        {
            if (context.Operation is not ISwitchCaseOperation switchCase)
                return;

            IOperation comparand = null;
            if (switchCase.Parent is ISwitchOperation switchStmt)
            {
                comparand = switchStmt.Value;
            }

            foreach (var clause in switchCase.Clauses)
            {
                if (clause is ISingleValueCaseClauseOperation singleValue)
                    AnalyzeOperandForLiteral(context, singleValue.Value, comparand);
            }
        }

        private static object FindComparandForPattern(IOperation pattern)
        {
            var current = pattern?.Parent;
            while (current != null)
            {
                if (current is IPropertySubpatternOperation propSub)
                {
                    if (propSub.Member is IPropertySymbol propSymbol)
                        return propSymbol;
                    return null;
                }
                if (current is ISwitchExpressionArmOperation arm)
                {
                    if (arm.Parent is ISwitchExpressionOperation switchExpr)
                        return switchExpr.Value;
                    return null;
                }
                if (current is ISwitchCaseOperation switchCase)
                {
                    if (switchCase.Parent is ISwitchOperation switchStmt)
                        return switchStmt.Value;
                    return null;
                }
                if (current is IIsPatternOperation isPattern)
                {
                    return isPattern.Value;
                }

                current = current.Parent;
            }
            return null;
        }

        private static void AnalyzeOperandForLiteral(OperationAnalysisContext context, IOperation operand, object comparand = null)
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
                if (IsExemptZeroComparand(comparand, context.Operation.SemanticModel))
                    return;

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

        private static bool IsAllowedMethodName(string methodName)
        {
            if (string.IsNullOrEmpty(methodName)) return false;
            return methodName.StartsWith("IndexOf", StringComparison.Ordinal) ||
                   methodName.StartsWith("LastIndexOf", StringComparison.Ordinal);
        }

        private static bool IsAllowedPropertyName(string propertyName)
        {
            return propertyName == "Length" || propertyName == "Count";
        }

        private static bool IsExemptZeroComparand(object comparand, SemanticModel semanticModel)
        {
            if (comparand == null)
                return false;

            if (comparand is IPropertySymbol propSymbol)
            {
                return IsAllowedPropertyName(propSymbol.Name);
            }

            if (comparand is IOperation comparandOp)
            {
                return IsExemptComparand(comparandOp, semanticModel, null);
            }

            return false;
        }

        private static bool IsExemptComparand(
            IOperation comparandOp,
            SemanticModel semanticModel,
            HashSet<ILocalSymbol> visitedLocals)
        {
            if (comparandOp == null)
                return false;

            var unwrapped = UnwrapOperation(comparandOp);
            if (unwrapped == null)
                return false;

            if (unwrapped is IInvocationOperation invocation)
            {
                var method = invocation.TargetMethod;
                if (method != null && IsAllowedMethodName(method.Name))
                    return true;
            }
            else if (unwrapped is IPropertyReferenceOperation propertyRef)
            {
                var property = propertyRef.Property;
                if (property != null && IsAllowedPropertyName(property.Name))
                    return true;
            }
            else if (unwrapped is ILocalReferenceOperation localRef)
            {
                var localSymbol = localRef.Local;
                if (localSymbol != null)
                {
                    visitedLocals ??= new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
                    if (!visitedLocals.Add(localSymbol))
                        return false;

                    if (CheckLocalVariableExemption(localSymbol, semanticModel, visitedLocals))
                        return true;
                }
            }

            return false;
        }

        private static IOperation UnwrapOperation(IOperation op)
        {
            var current = op;
            while (current != null)
            {
                if (current is IConversionOperation conv)
                    current = conv.Operand;
                else if (current is IParenthesizedOperation parenthesized)
                    current = parenthesized.Operand;
                else
                    break;
            }
            return current;
        }

        private static bool CheckLocalVariableExemption(
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            HashSet<ILocalSymbol> visitedLocals)
        {
            if (semanticModel == null)
                return false;

            foreach (var syntaxRef in localSymbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                if (syntax == null)
                    continue;

                // 1. Check declaration initializer
                if (syntax is VariableDeclaratorSyntax declarator)
                {
                    if (declarator.Initializer?.Value is ExpressionSyntax initExpr)
                    {
                        var initOp = semanticModel.GetOperation(initExpr);
                        if (initOp != null && IsExemptComparand(initOp, semanticModel, visitedLocals))
                            return true;
                    }
                }

                // 2. Check assignments in enclosing member / scope
                SyntaxNode scopeNode = null;
                foreach (var ancestor in syntax.Ancestors())
                {
                    if (ancestor is MemberDeclarationSyntax || ancestor is LocalFunctionStatementSyntax || ancestor is AnonymousFunctionExpressionSyntax)
                    {
                        scopeNode = ancestor;
                        break;
                    }
                }
                scopeNode ??= syntax.SyntaxTree.GetRoot();

                var assignments = scopeNode.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                foreach (var assignment in assignments)
                {
                    var leftOp = semanticModel.GetOperation(assignment.Left);
                    leftOp = UnwrapOperation(leftOp);
                    if (leftOp is ILocalReferenceOperation leftLocalRef &&
                        SymbolEqualityComparer.Default.Equals(leftLocalRef.Local, localSymbol))
                    {
                        var rightOp = semanticModel.GetOperation(assignment.Right);
                        if (rightOp != null && IsExemptComparand(rightOp, semanticModel, visitedLocals))
                            return true;
                    }
                }
            }

            return false;
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
    }
}
