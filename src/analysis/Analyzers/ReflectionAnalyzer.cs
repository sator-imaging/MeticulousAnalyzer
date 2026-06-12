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
            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
            context.RegisterOperationAction(AnalyzeMemberReference,
                OperationKind.PropertyReference,
                OperationKind.FieldReference,
                OperationKind.EventReference,
                OperationKind.MethodReference);
            context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.Binary);
            context.RegisterOperationAction(AnalyzeUnaryOperator, OperationKind.Unary);
            context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
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
                GetReflectionReceiverType(invocation.Instance) ??
                FindReflectionType(invocation.TargetMethod.ReturnType) ??
                (invocation.Instance == null ? FindReflectionType(invocation.TargetMethod.ContainingType) : null));
        }

        private static void AnalyzeBinaryOperator(OperationAnalysisContext context)
        {
            if (context.Operation is not IBinaryOperation binary)
            {
                return;
            }

            ReportIfReflection(
                context,
                binary,
                FindReflectionType(binary.Type) ??
                (binary.OperatorMethod != null ? FindReflectionType(binary.OperatorMethod.ContainingType) : null) ??
                FindReflectionType(binary.LeftOperand.Type) ??
                FindReflectionType(binary.RightOperand.Type));
        }

        private static void AnalyzeUnaryOperator(OperationAnalysisContext context)
        {
            if (context.Operation is not IUnaryOperation unary)
            {
                return;
            }

            ReportIfReflection(
                context,
                unary,
                FindReflectionType(unary.Type) ??
                (unary.OperatorMethod != null ? FindReflectionType(unary.OperatorMethod.ContainingType) : null) ??
                FindReflectionType(unary.Operand.Type));
        }

        private static void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            if (context.Operation is not IObjectCreationOperation creation)
            {
                return;
            }

            ReportIfReflection(
                context,
                creation,
                FindReflectionType(creation.Constructor?.ContainingType) ?? FindReflectionType(creation.Type));
        }

        private static void AnalyzeMemberReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IMemberReferenceOperation memberReference)
            {
                return;
            }

            if (memberReference is IMethodReferenceOperation { Parent: IInvocationOperation })
            {
                return;
            }

            var member = memberReference.Member;
            var reflectionType = GetReflectionReceiverType(memberReference.Instance);

            if (reflectionType == null)
            {
                reflectionType = member switch
                {
                    IFieldSymbol field => FindReflectionType(field.Type),
                    IPropertySymbol prop => FindReflectionType(prop.Type),
                    IMethodSymbol method => FindReflectionType(method.ReturnType),
                    IEventSymbol ev => FindReflectionType(ev.Type),
                    _ => null,
                };
            }

            if (reflectionType == null && memberReference.Instance == null)
            {
                reflectionType = FindReflectionType(member.ContainingType);
            }

            ReportIfReflection(context, memberReference, reflectionType);
        }

        private static void AnalyzeArgument(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation argument)
            {
                return;
            }

            // Skip if the value is an operation that is already analyzed and reported as SMA7010.
            if (argument.Value is IInvocationOperation
                or IObjectCreationOperation
                or IMemberReferenceOperation
                or IBinaryOperation
                or IUnaryOperation
                or IConversionOperation)
            {
                return;
            }

            ReportIfReflection(context, argument, FindReflectionType(argument.Value?.Type));
        }

        private static void AnalyzeConversion(OperationAnalysisContext context)
        {
            if (context.Operation is not IConversionOperation conversion ||
                conversion.IsImplicit)
            {
                return;
            }

            ReportIfReflection(context, conversion, FindReflectionType(conversion.Type));
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

            // Skip if it's inside an attribute.
            var syntax = operation.Syntax;
            while (syntax != null)
            {
                if (syntax is Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax)
                {
                    return;
                }
                syntax = syntax.Parent;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionUsage,
                operation.Syntax.GetLocation(),
                GetOperationName(operation),
                reflectionType.ToDiagnosticMessageName()));
        }

        private static string GetOperationName(IOperation operation)
        {
            var target = operation switch
            {
                IInvocationOperation invocation => invocation.TargetMethod,
                IObjectCreationOperation creation => creation.Constructor,
                IMemberReferenceOperation member => member.Member,
                IArgumentOperation argument => argument.Parameter,
                IBinaryOperation binary => binary.OperatorMethod,
                IUnaryOperation unary => unary.OperatorMethod,
                _ => null,
            };
            
            return target?.ToDiagnosticMessageName() ?? operation.Kind.ToString();
        }

        private static INamedTypeSymbol? GetReflectionReceiverType(IOperation? instance)
        {
            return FindReflectionType(instance?.Type);
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
            var ns = type.ContainingNamespace;
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
