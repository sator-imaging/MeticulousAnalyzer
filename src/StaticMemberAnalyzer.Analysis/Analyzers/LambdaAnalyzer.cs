// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Linq;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LambdaAnalyzer : DiagnosticAnalyzer
    {
        private const string SuppressionComment = "// Allow allocation";

        public const string RuleId_LambdaShouldBeStatic = "SMA7000";
        private static readonly DiagnosticDescriptor Rule_LambdaShouldBeStatic = new(
            RuleId_LambdaShouldBeStatic,
            new LocalizableResourceString(nameof(Resources.SMA7000_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7000_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7000_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ImplicitConversionToDelegate = "SMA7001";
        private static readonly DiagnosticDescriptor Rule_ImplicitConversionToDelegate = new(
            RuleId_ImplicitConversionToDelegate,
            new LocalizableResourceString(nameof(Resources.SMA7001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7001_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_LambdaAllocation = "SMA7002";
        private static readonly DiagnosticDescriptor Rule_LambdaAllocation = new(
            RuleId_LambdaAllocation,
            new LocalizableResourceString(nameof(Resources.SMA7002_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7002_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7002_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_LambdaShouldBeStatic,
            Rule_ImplicitConversionToDelegate,
            Rule_LambdaAllocation
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeAnonymousFunction, OperationKind.AnonymousFunction);
            context.RegisterOperationAction(AnalyzeImplicitConversion, OperationKind.Conversion, OperationKind.DelegateCreation);
        }

        private static void AnalyzeAnonymousFunction(OperationAnalysisContext context)
        {
            if (context.Operation is not IAnonymousFunctionOperation anonFunc ||
                anonFunc.Syntax is not LambdaExpressionSyntax lambda ||
                lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            var semanticModel = anonFunc.SemanticModel ?? context.Operation.SemanticModel;
            if (semanticModel == null)
            {
                return;
            }

            if (IsEffectivelyStatic(lambda, semanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_LambdaShouldBeStatic, lambda.GetLocation()));
            }
            else
            {
                var parent = anonFunc.Parent;
                if ((parent is IConversionOperation { IsImplicit: true } conversion && IsActionOrFunc(conversion.Type)) ||
                    (parent is IDelegateCreationOperation { IsImplicit: true } delegateCreation && IsActionOrFunc(delegateCreation.Type)))
                {
                    ReportSMA7002(context, lambda, parent);
                }
            }
        }

        private static void AnalyzeImplicitConversion(OperationAnalysisContext context)
        {
            var op = context.Operation;
            bool isImplicit = (op as IConversionOperation)?.IsImplicit ?? (op as IDelegateCreationOperation)?.IsImplicit ?? false;
            if (!isImplicit)
            {
                return;
            }

            var operand = (op as IConversionOperation)?.Operand ?? (op as IDelegateCreationOperation)?.Target;
            if (operand == null)
            {
                return;
            }

            // Don't show warning if the "value" is lambda as it is handled by AnalyzeAnonymousFunction.
            var unwrapped = UnwrapConversion(operand);
            if (unwrapped.Kind == OperationKind.AnonymousFunction)
            {
                return;
            }

            // Check if target type is Action or Func
            if (!IsActionOrFunc(op.Type))
            {
                return;
            }

            // Don't show warning if the "value" side is static field, method, property or other static member.
            // EXCEPT for static methods, which we want to fix by wrapping with static lambda to avoid allocation.
            if (IsStaticMember(unwrapped) && !IsStaticMethodReference(unwrapped))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_ImplicitConversionToDelegate, operand.Syntax.GetLocation(), op.Type.ToDisplayString()));
        }

        private static void ReportSMA7002(OperationAnalysisContext context, LambdaExpressionSyntax lambda, IOperation parent)
        {
            if (Core.IsSuppressedByComment(lambda, SuppressionComment) || Core.IsSuppressedByComment(parent, SuppressionComment))
            {
                return;
            }

            Location location;
            if (lambda is SimpleLambdaExpressionSyntax simple)
            {
                location = simple.Parameter.GetLocation();
            }
            else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized)
            {
                location = parenthesized.ParameterList.GetLocation();
            }
            else
            {
                location = lambda.GetLocation();
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_LambdaAllocation, location));
        }

        private static bool IsEffectivelyStatic(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            var flow = semanticModel.AnalyzeDataFlow(lambda.Body);
            return !flow.CapturedInside.Any();
        }

        private static IOperation UnwrapConversion(IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }
            return operation;
        }

        private static bool IsStaticMember(IOperation operation)
        {
            var current = UnwrapConversion(operation);

            if (current is IMemberReferenceOperation memRef)
            {
                return memRef.Member.IsStatic;
            }

            if (current is IInvocationOperation invocation)
            {
                return invocation.TargetMethod.IsStatic;
            }

            if (current is IMethodReferenceOperation methodRef)
            {
                return methodRef.Method.IsStatic;
            }

            return false;
        }

        private static bool IsStaticMethodReference(IOperation operation)
        {
            var current = UnwrapConversion(operation);
            return current is IMethodReferenceOperation methodRef && methodRef.Method.IsStatic;
        }

        private static bool IsActionOrFunc(ITypeSymbol? type)
        {
            return type?.Name is "Action" or "Func"
                && type.ContainingNamespace is INamespaceSymbol
                {
                    Name: "System",
                    ContainingNamespace: INamespaceSymbol
                    {
                        IsGlobalNamespace: true,
                    }
                };
        }
    }
}
