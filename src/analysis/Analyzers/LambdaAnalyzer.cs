// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LambdaAnalyzer : DiagnosticAnalyzer
    {
        private const string SuppressionComment = "// Allow allocation";

        public const string RuleId_LambdaCanBeStatic = "SMA7000";
        private static readonly DiagnosticDescriptor Rule_LambdaCanBeStatic = new(
            RuleId_LambdaCanBeStatic,
            new LocalizableResourceString(nameof(Resources.SMA7000_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7000_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7000_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_InefficientDelegateDeclaration = "SMA7001";
        private static readonly DiagnosticDescriptor Rule_InefficientDelegateDeclaration = new(
            RuleId_InefficientDelegateDeclaration,
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
            Rule_LambdaCanBeStatic,
            Rule_InefficientDelegateDeclaration,
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
                context.ReportDiagnostic(Diagnostic.Create(Rule_LambdaCanBeStatic, lambda.GetLocation()));
            }
            else
            {
                var op = UnwrapConversion(anonFunc);

                // NOTE: For lambda, additionally allow placing comment on declaration.
                //       --> DoSomething(
                //               // Allow allocation
                //               () => { },
                //               // Allow allocation
                //               (x) => x * x
                //           );
                if (Core.IsSuppressedByComment(op.Syntax, SuppressionComment) ||
                    // Also check variable declaration.
                    Core.IsSuppressedByComment(op.Parent, SuppressionComment))
                {
                    return;
                }

                // NOTE: To support both SimpleLambdaExpressionSyntax and ParenthesizedLambdaExpressionSyntax.
                //       --> ([optional args...]) => { }
                //           ~~~~~~~~~~~~~~~~~~~~~~~
                //           ^ lambda start        ^ arrow end
                var start = lambda.SpanStart;
                var end = lambda.ArrowToken.Span.End;
                var length = end - start;
                var location = length > 0
                    ? Location.Create(lambda.SyntaxTree, new(start, length))
                    : lambda.GetLocation();

                context.ReportDiagnostic(Diagnostic.Create(Rule_LambdaAllocation, location));
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

            // Check if target type is Action or Func, or any other delegate.
            bool isActionOrFunc = IsActionOrFunc(op.Type);
            if (!isActionOrFunc && op.Type?.TypeKind != TypeKind.Delegate)
            {
                return;
            }

            // Don't show warning if the "value" side is static field, method, property or other static member.
            // EXCEPT for static methods of Action/Func, which we want to fix by wrapping with static lambda to avoid allocation.
            if (IsStaticMember(unwrapped))
            {
                if (!isActionOrFunc || !IsStaticMethodReference(unwrapped))
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_InefficientDelegateDeclaration, operand.Syntax.GetLocation(), op.Type.ToDisplayString()));
        }

        private static bool IsEffectivelyStatic(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            var flow = semanticModel.AnalyzeDataFlow(lambda.Body);
            return flow != null && !flow.CapturedInside.Any();
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
                    Name: "System", ContainingNamespace: INamespaceSymbol
                    {
                        IsGlobalNamespace: true,
                    }
                };
        }
    }
}
