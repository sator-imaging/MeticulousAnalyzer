// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DebugAssertAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_DebugAssertInPublicApi = "SMA8003";

        private static readonly DiagnosticDescriptor Rule_DebugAssertInPublicApi = new(
            RuleId_DebugAssertInPublicApi,
            new LocalizableResourceString(nameof(Resources.SMA8003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(DebugAssertAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8003_MessageFormat), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_DebugAssertInPublicApi);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
                return;

            if (!invocation.TargetMethod.Name.StartsWith("Assert", System.StringComparison.Ordinal))
                return;

            var symbol = context.ContainingSymbol;
            do
            {
                if (symbol is IMethodSymbol method)
                {
                    if (method.MethodKind is MethodKind.LambdaMethod or MethodKind.LocalFunction)
                    {
                        symbol = symbol.ContainingSymbol;
                        continue;
                    }

                    if (method.AssociatedSymbol != null)
                    {
                        symbol = method.AssociatedSymbol;
                    }
                }
                break;
            }
            while (symbol != null);

            if (symbol == null)
                return;

            if (IsPubliclyAccessible(symbol))
            {
                if (IsExempted(invocation))
                    return;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_DebugAssertInPublicApi,
                    invocation.Syntax.GetLocation()));
            }
        }

        private static bool IsPubliclyAccessible(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => true,
                Accessibility.Protected => true,
                Accessibility.ProtectedOrInternal => true,
                _ => false,
            };
        }

        private static bool IsExempted(IInvocationOperation invocation)
        {
            if (invocation.Arguments.Length == 0)
                return false;

            var argVal = Unwrap(invocation.Arguments[0].Value);
            if (argVal == null)
                return false;

            if (argVal is IIsPatternOperation isPattern)
            {
                if (isPattern.Pattern is INegatedPatternOperation)
                {
                    var valueOp = Unwrap(isPattern.Value);
                    if (valueOp is IParameterReferenceOperation paramRef)
                    {
                        return IsNonNullableParameter(paramRef.Parameter);
                    }
                }
            }

            if (argVal is IBinaryOperation binary)
            {
                if (binary.OperatorKind == BinaryOperatorKind.NotEquals)
                {
                    var left = Unwrap(binary.LeftOperand);
                    var right = Unwrap(binary.RightOperand);

                    if (left is IParameterReferenceOperation leftParamRef && IsNonNullableParameter(leftParamRef.Parameter))
                    {
                        return true;
                    }

                    if (right is IParameterReferenceOperation rightParamRef && IsNonNullableParameter(rightParamRef.Parameter))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IOperation Unwrap(IOperation op)
        {
            while (op != null)
            {
                if (op is IConversionOperation conversion)
                {
                    op = conversion.Operand;
                }
                else if (op is IParenthesizedOperation parenthesized)
                {
                    op = parenthesized.Operand;
                }
                else
                {
                    break;
                }
            }
            return op;
        }

        private static bool IsNonNullableParameter(IParameterSymbol parameter)
        {
            if (parameter == null)
                return false;

            if (parameter.NullableAnnotation == NullableAnnotation.Annotated)
                return false;

            if (parameter.Type != null)
            {
                if (parameter.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    return false;

                if (parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    return false;
            }

            return true;
        }
    }
}
