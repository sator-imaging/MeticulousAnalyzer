// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    // TODO: Latest .NET already includes a Trimmer Analyzer.
    //       Consider disable this analyzer by default for latest environment.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReflectionAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_SystemReflectionUsage = "SMA7010";
        public const string RuleId_SystemReflectionVariable = "SMA7011";

        private static readonly DiagnosticDescriptor Rule_SystemReflectionUsage = new(
            RuleId_SystemReflectionUsage,
            new LocalizableResourceString(nameof(Resources.SMA7010_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7010_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7010_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_SystemReflectionVariable = new(
            RuleId_SystemReflectionVariable,
            new LocalizableResourceString(nameof(Resources.SMA7011_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7011_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7011_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_SystemReflectionUsage,
            Rule_SystemReflectionVariable);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
            context.RegisterOperationAction(AnalyzeMethodReference, OperationKind.MethodReference);
            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
            {
                return;
            }

            ReportIfReflection(
                context,
                invocation,
                FindReflectionType(invocation.TargetMethod.ReturnType) ?? GetReflectionReceiverType(invocation.Instance));

            foreach (var argument in invocation.Arguments)
            {
                if (argument.Value is IBinaryOperation)
                {
                    continue;
                }

                ReportIfReflection(context, argument, FindReflectionType(argument.Value?.Type));
            }
        }

        private static void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IPropertyReferenceOperation propertyReference)
            {
                return;
            }

            ReportIfReflection(
                context,
                propertyReference,
                FindReflectionType(propertyReference.Type) ?? GetReflectionReceiverType(propertyReference.Instance));
        }

        private static void AnalyzeFieldReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IFieldReferenceOperation fieldReference)
            {
                return;
            }

            ReportIfReflection(
                context,
                fieldReference,
                FindReflectionType(fieldReference.Type) ?? GetReflectionReceiverType(fieldReference.Instance));
        }

        private static void AnalyzeMethodReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IMethodReferenceOperation methodReference)
            {
                return;
            }

            if (methodReference.Parent is IInvocationOperation)
            {
                return;
            }

            ReportIfReflection(
                context,
                methodReference,
                FindReflectionType(methodReference.Method.ReturnType) ?? GetReflectionReceiverType(methodReference.Instance));
        }

        private static void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declarator)
            {
                return;
            }

            var reflectionType = FindReflectionType(declarator.Symbol.Type);
            if (reflectionType == null || reflectionType.TypeKind == TypeKind.Enum)
            {
                return;
            }

            // VariableDeclarator fires per name (var a, b = ...); [0] covers a and b — [1] is not required.
            var location = declarator.Symbol.Locations is { Length: > 0 } locations
                ? locations[0]
                : declarator.Syntax.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionVariable,
                location,
                declarator.Symbol.Name,
                declarator.Symbol.Type.ToDiagnosticMessageName()));
        }

        private static void ReportIfReflection(
            OperationAnalysisContext context,
            IOperation operation,
            INamedTypeSymbol? reflectionType)
        {
            if (reflectionType == null || reflectionType.TypeKind == TypeKind.Enum)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionUsage,
                operation.Syntax.GetLocation(),
                GetOperationName(operation),
                reflectionType.ToDiagnosticMessageName()));
        }

        private static string GetOperationName(IOperation operation)
        {
            return operation switch
            {
                IInvocationOperation invocation => invocation.TargetMethod.ToDiagnosticMessageName(),
                IMemberReferenceOperation member => member.Member.ToDiagnosticMessageName(),
                IArgumentOperation argument => argument.Parameter.ToDiagnosticMessageName(),
                _ => operation.Syntax.ToString(),
            };
        }

        private static INamedTypeSymbol? GetReflectionReceiverType(IOperation? instance)
        {
            return instance?.Type is INamedTypeSymbol named && IsReflectionType(named) ? named : null;
        }

        private const int MaxTypeSearchDepth = 8;

        private static INamedTypeSymbol? FindReflectionType(ITypeSymbol? type, int depth = 0)
        {
            if (type == null || depth > MaxTypeSearchDepth)
            {
                return null;
            }

            switch (type)
            {
                case IArrayTypeSymbol array:
                    return FindReflectionType(array.ElementType, depth + 1);

                case INamedTypeSymbol named:
                    if (IsReflectionType(named))
                    {
                        return named;
                    }

                    foreach (var typeArg in named.TypeArguments)
                    {
                        var found = FindReflectionType(typeArg, depth + 1);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                    return null;

                default:
                    return null;
            }
        }

        private static bool IsReflectionType(INamedTypeSymbol type)
        {
            return IsSystemReflectionNamespace(type.ContainingNamespace);
        }

        private static bool IsSystemReflectionNamespace(INamespaceSymbol? ns)
        {
            while (ns is { IsGlobalNamespace: false })
            {
                if (ns is
                    {
                        Name: nameof(System.Reflection), ContainingNamespace:
                        {
                            Name: nameof(System), ContainingNamespace:
                            {
                                IsGlobalNamespace: true,
                            }
                        }
                    })
                {
                    return true;
                }

                ns = ns.ContainingNamespace;
            }

            return false;
        }
    }
}
