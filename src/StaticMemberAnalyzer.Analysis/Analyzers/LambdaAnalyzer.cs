// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LambdaAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_LambdaShouldBeStatic = "SMA7000";
        private static readonly DiagnosticDescriptor Rule_LambdaShouldBeStatic = new(
            RuleId_LambdaShouldBeStatic,
            new LocalizableResourceString(nameof(Resources.SMA7000_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7000_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.AssistanceCategory,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7000_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ImplicitConversionToDelegate = "SMA7001";
        private static readonly DiagnosticDescriptor Rule_ImplicitConversionToDelegate = new(
            RuleId_ImplicitConversionToDelegate,
            new LocalizableResourceString(nameof(Resources.SMA7001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.AssistanceCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7001_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_LambdaShouldBeStatic,
            Rule_ImplicitConversionToDelegate
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
            context.RegisterOperationAction(AnalyzeDelegateCreation, OperationKind.DelegateCreation);
        }

        private static void AnalyzeDelegateCreation(OperationAnalysisContext context)
        {
            var delegateCreation = (IDelegateCreationOperation)context.Operation;
            if (!delegateCreation.IsImplicit) return;

            // Don't show warning if the "value" is lambda as it is handled by AnalyzeLambda.
            var unwrapped = UnwrapConversion(delegateCreation.Target);
            if (unwrapped.Kind == OperationKind.AnonymousFunction) return;

            // Check if target type is Action or Func
            if (!IsActionOrFunc(delegateCreation.Type)) return;

            // Don't show warning if the "value" side is static field, method, property or other static member.
            // EXCEPT for static methods, which we want to fix by wrapping with static lambda to avoid allocation.
            if (IsStaticMember(unwrapped) && !IsStaticMethodReference(unwrapped)) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule_ImplicitConversionToDelegate, delegateCreation.Target.Syntax.GetLocation(), delegateCreation.Type.ToDisplayString()));
        }

        private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not LambdaExpressionSyntax lambda || lambda.Modifiers.Any(SyntaxKind.StaticKeyword)) return;

            if (IsEffectivelyStatic(lambda, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_LambdaShouldBeStatic, lambda.GetLocation()));
            }
        }

        private static bool IsEffectivelyStatic(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
        {
            var flow = semanticModel.AnalyzeDataFlow(lambda.Body);
            return !flow.CapturedInside.Any();
        }

        private static void AnalyzeConversion(OperationAnalysisContext context)
        {
            var conversion = (IConversionOperation)context.Operation;
            if (!conversion.IsImplicit) return;

            // Don't show warning if the "value" is lambda as it is handled by AnalyzeLambda.
            var unwrapped = UnwrapConversion(conversion.Operand);
            if (unwrapped.Kind == OperationKind.AnonymousFunction) return;

            // Check if target type is Action or Func
            if (!IsActionOrFunc(conversion.Type)) return;

            // Don't show warning if the "value" side is static field, method, property or other static member.
            // EXCEPT for static methods, which we want to fix by wrapping with static lambda to avoid allocation.
            if (IsStaticMember(unwrapped) && !IsStaticMethodReference(unwrapped)) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule_ImplicitConversionToDelegate, conversion.Operand.Syntax.GetLocation(), conversion.Type.ToDisplayString()));
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
                return memRef.Member.IsStatic;

            if (current is IInvocationOperation invocation)
                return invocation.TargetMethod.IsStatic;

            if (current is IMethodReferenceOperation methodRef)
                return methodRef.Method.IsStatic;

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
                && type.ContainingNamespace is INamespaceSymbol { Name: "System", ContainingNamespace: INamespaceSymbol { IsGlobalNamespace: true } };
        }
    }
}
