// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReflectionAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_SystemReflectionUsage = "SMA7003";

        private static readonly DiagnosticDescriptor Rule_SystemReflectionUsage = new(
            RuleId_SystemReflectionUsage,
            new LocalizableResourceString(nameof(Resources.SMA7003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7003_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_SystemReflectionUsage);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
            context.RegisterOperationAction(AnalyzeMethodReference, OperationKind.MethodReference);
            context.RegisterOperationAction(AnalyzeVariableDeclaration, OperationKind.VariableDeclaration);
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

        private static void AnalyzeVariableDeclaration(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclarationOperation declaration)
            {
                return;
            }

            ReportIfReflection(context, declaration, FindReflectionType(declaration.Type));
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
                GetReportLocation(operation),
                GetOperationName(operation),
                reflectionType.ToDisplayString()));
        }

        private static Location GetReportLocation(IOperation operation)
        {
            return operation.Syntax.GetLocation();
        }

        private static string GetOperationName(IOperation operation)
        {
            if (operation is IVariableDeclarationOperation declaration)
            {
                return declaration.Type.ToDiagnosticMessageName();
            }

            ISymbol? symbol = operation switch
            {
                IInvocationOperation invocation => invocation.TargetMethod,
                IPropertyReferenceOperation property => property.Property,
                IFieldReferenceOperation field => field.Field,
                IMethodReferenceOperation method => method.Method,
                IArgumentOperation argument => argument.Parameter,
                _ => null,
            };

            return symbol?.ToDiagnosticMessageName() ?? operation.Kind.ToString();
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
