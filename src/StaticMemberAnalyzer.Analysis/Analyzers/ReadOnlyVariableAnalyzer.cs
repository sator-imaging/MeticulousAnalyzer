// Licensed under the MIT License
// https://github.com/sator-imaging/CSharp-StaticFieldAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReadOnlyVariableAnalyzer : DiagnosticAnalyzer
    {
        const string ImmutableCategory = "ImmutableVariable";
        const bool IsEnabledByDefault = false;

        public const string RuleId_ReadOnlyLocal = "SMA0060";
        public const string RuleId_ReadOnlyParameter = "SMA0061";
        public const string RuleId_ReadOnlyArgument = "SMA0062";
        public const string RuleId_ReadOnlyPropertyArgument = "SMA0063";

        private static readonly DiagnosticDescriptor Rule_ReadOnlyLocal = new(
            RuleId_ReadOnlyLocal,
            new LocalizableResourceString("SMA0060_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA0060_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            ImmutableCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: IsEnabledByDefault,
            description: new LocalizableResourceString("SMA0060_Description", Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_ReadOnlyParameter = new(
            RuleId_ReadOnlyParameter,
            new LocalizableResourceString("SMA0061_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA0061_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            ImmutableCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: IsEnabledByDefault,
            description: new LocalizableResourceString("SMA0061_Description", Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_ReadOnlyArgument = new(
            RuleId_ReadOnlyArgument,
            new LocalizableResourceString("SMA0062_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA0062_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            ImmutableCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: IsEnabledByDefault,
            description: new LocalizableResourceString("SMA0062_Description", Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_PropertyAccessCanChangeState = new(
            RuleId_ReadOnlyPropertyArgument,
            new LocalizableResourceString("SMA0063_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA0063_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            ImmutableCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: IsEnabledByDefault,
            description: new LocalizableResourceString("SMA0063_Description", Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_ReadOnlyLocal,
            Rule_ReadOnlyParameter,
            Rule_ReadOnlyArgument,
            Rule_PropertyAccessCanChangeState
            );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(ctx =>
            {
                const string GlobalOptionsCategory = "dotnet_analyzer_diagnostic.category-" + ImmutableCategory + ".severity";

                if (!ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(GlobalOptionsCategory, out var severity))
                {
                    return;
                }

                // https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#severity-level
                if (severity.ToLowerInvariant() is "error" or "warning" or "suggestion")
                {
                    ctx.RegisterOperationAction(AnalyzeSimpleAssignment, OperationKind.SimpleAssignment);
                    ctx.RegisterOperationAction(AnalyzeCoalesceAssignment, OperationKind.CoalesceAssignment);
                    ctx.RegisterOperationAction(AnalyzeCompoundAssignment, OperationKind.CompoundAssignment);
                    ctx.RegisterOperationAction(AnalyzeIncrementOrDecrement, OperationKind.Increment, OperationKind.Decrement);
                    ctx.RegisterOperationAction(AnalyzeDeconstructionAssignment, OperationKind.DeconstructionAssignment);
                    ctx.RegisterOperationAction(AnalyzeArgumentOperation, OperationKind.Argument);
                }
            });
        }

        private static void AnalyzeSimpleAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not ISimpleAssignmentOperation op)
            {
                return;
            }

            ReportIfDisallowedMutation(context, op, op.Target);
        }

        private static void AnalyzeCompoundAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not ICompoundAssignmentOperation op)
            {
                return;
            }

            ReportIfDisallowedMutation(context, op, op.Target);
        }

        private static void AnalyzeCoalesceAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not ICoalesceAssignmentOperation op)
            {
                return;
            }

            ReportIfDisallowedMutation(context, op, op.Target);
        }

        private static void AnalyzeIncrementOrDecrement(OperationAnalysisContext context)
        {
            if (context.Operation is not IIncrementOrDecrementOperation op)
            {
                return;
            }

            ReportIfDisallowedMutation(context, op, op.Target);
        }

        private static void AnalyzeDeconstructionAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not IDeconstructionAssignmentOperation op)
            {
                return;
            }

            var target = op.Target is IConversionOperation conversion
                ? conversion.Operand
                : op.Target;

            if (target is IDeclarationExpressionOperation)
            {
                return;
            }

            ReportIfDisallowedMutation(context, op, target);
        }

        private static void AnalyzeArgumentOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation argument)
            {
                return;
            }

            AnalyzeArgument(context, argument);
        }

        private static void ReportIfDisallowedMutation(OperationAnalysisContext context, IOperation mutationOp, IOperation target)
        {
            var reported = new HashSet<string>();
            foreach (var (name, isParameter, isOutParameter, location, syntax) in EnumerateAssignedLocalsAndParameters(target))
            {
                if (HasMutableNamePrefix(name))
                {
                    continue;
                }

                if (isOutParameter)
                {
                    continue;
                }

                if (IsAllowedInStatementHeader(mutationOp, syntax))
                {
                    continue;
                }

                var key = name + "@" + location.SourceSpan.Start;
                if (!reported.Add(key))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(GetDescriptor(isParameter), location, name));
            }
        }

        private static IEnumerable<(string name, bool isParameter, bool isOutParameter, Location location, SyntaxNode syntax)> EnumerateAssignedLocalsAndParameters(IOperation op)
        {
            if (op is ILocalReferenceOperation localReference)
            {
                yield return (localReference.Local.Name, false, false, op.Syntax.GetLocation(), op.Syntax);
            }
            else if (op is IParameterReferenceOperation parameterReference)
            {
                yield return (
                    parameterReference.Parameter.Name,
                    true,
                    parameterReference.Parameter.RefKind == RefKind.Out,
                    op.Syntax.GetLocation(),
                    op.Syntax);
            }
            else if (op is IPropertyReferenceOperation or IFieldReferenceOperation)
            {
                if (TryGetRootLocalOrParameter(op, out var name, out var isParameter))
                {
                    yield return (name, isParameter, false, op.Syntax.GetLocation(), op.Syntax);
                }
            }
            else if (op is ITupleOperation tupleOperation)
            {
                foreach (var element in tupleOperation.Elements)
                {
                    foreach (var nested in EnumerateAssignedLocalsAndParameters(element))
                    {
                        yield return nested;
                    }
                }
            }
            else if (op is IVariableDeclaratorOperation variableDeclarator && variableDeclarator.Symbol is ILocalSymbol localSymbol)
            {
                yield return (localSymbol.Name, false, false, op.Syntax.GetLocation(), op.Syntax);
            }
            else if (op is IDeclarationExpressionOperation declarationExpression)
            {
                foreach (var nested in EnumerateAssignedLocalsAndParameters(declarationExpression.Expression))
                {
                    yield return nested;
                }
            }
        }

        private static bool HasMutableNamePrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return name.StartsWith("mut_");
        }

        private static void AnalyzeArgument(OperationAnalysisContext context, IArgumentOperation argument)
        {
            // The analysis precedence in this method is intentionally designed and must not be changed.
            var argumentValue = argument.Value;
            while (argumentValue is IConversionOperation conversion)
            {
                argumentValue = conversion.Operand;
            }

            if (IsAllowedArgumentValue(argumentValue))
            {
                return;
            }

            var parameter = argument.Parameter;
            if (parameter == null)
            {
                return;
            }

            // `out var x` / `out T x` declaration in call site is allowed.
            if (parameter.RefKind == RefKind.Out && argumentValue is IDeclarationExpressionOperation)
            {
                return;
            }

            var hasRoot = TryGetRootLocalOrParameter(argumentValue, out var rootName, out _);
            if (hasRoot)
            {
                if (HasMutableNamePrefix(rootName))
                {
                    return;
                }

                if (argumentValue is IFieldReferenceOperation { Field: { IsReadOnly: true } or { IsConst: true } })
                {
                    return;
                }

                if (argumentValue is IPropertyReferenceOperation propRef)
                {
                    if (propRef.Property.GetMethod?.IsReadOnly == true)
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_PropertyAccessCanChangeState,
                        argumentValue.Syntax.GetLocation(),
                        argumentValue.Syntax.ToString()));
                    return;
                }
            }

            var type = parameter.Type;
            var isString = type.SpecialType == SpecialType.System_String;

            // Relax for IEnumerable and Enum
            var isIEnumerable = type.SpecialType == SpecialType.System_Collections_IEnumerable
                || type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
            var isEnum = type.TypeKind == TypeKind.Enum;

            if (isIEnumerable || isEnum)
            {
                return;
            }

            var readOnlyStructLike = isString || (!type.IsReferenceType && type.IsReadOnly);

            if (type.IsReferenceType && !isString)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ReadOnlyArgument,
                    argumentValue.Syntax.GetLocation(),
                    hasRoot ? rootName : argumentValue.Syntax.ToString()));
                return;
            }

            if (parameter.RefKind == RefKind.In)
            {
                return;
            }

            if (parameter.RefKind == RefKind.None && readOnlyStructLike)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_ReadOnlyArgument,
                argumentValue.Syntax.GetLocation(),
                hasRoot ? rootName : argumentValue.Syntax.ToString()));
        }

        private static bool IsAllowedInStatementHeader(IOperation operation, SyntaxNode syntax)
        {
            var forSyntax = syntax.FirstAncestorOrSelf<ForStatementSyntax>();
            if (forSyntax != null)
            {
                if (forSyntax.Declaration != null && forSyntax.Declaration.Span.Contains(syntax.Span))
                {
                    return true;
                }

                if (forSyntax.Condition != null && forSyntax.Condition.Span.Contains(syntax.Span))
                {
                    return true;
                }

                foreach (var initializer in forSyntax.Initializers)
                {
                    if (initializer.Span.Contains(syntax.Span))
                    {
                        return true;
                    }
                }

                foreach (var incrementor in forSyntax.Incrementors)
                {
                    if (incrementor.Span.Contains(syntax.Span))
                    {
                        return true;
                    }
                }
            }

            if (operation.Kind == OperationKind.SimpleAssignment)
            {
                var whileSyntax = syntax.FirstAncestorOrSelf<WhileStatementSyntax>();
                if (whileSyntax != null && whileSyntax.Condition.Span.Contains(syntax.Span))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetRootLocalOrParameter(IOperation operation, out string name, out bool isParameter)
        {
            var current = operation;
            while (current != null)
            {
                if (current is IConversionOperation conversion)
                {
                    current = conversion.Operand;
                    continue;
                }

                if (current is ILocalReferenceOperation localReference)
                {
                    name = localReference.Local.Name;
                    isParameter = false;
                    return true;
                }

                if (current is IParameterReferenceOperation parameterReference)
                {
                    name = parameterReference.Parameter.Name;
                    isParameter = true;
                    return true;
                }

                if (current is IPropertyReferenceOperation propertyReference)
                {
                    current = propertyReference.Instance;
                    continue;
                }

                if (current is IFieldReferenceOperation fieldReference)
                {
                    current = fieldReference.Instance;
                    continue;
                }

                if (current is IArrayElementReferenceOperation arrayElementReference)
                {
                    current = arrayElementReference.ArrayReference;
                    continue;
                }

                break;
            }

            name = string.Empty;
            isParameter = false;
            return false;
        }

        private static DiagnosticDescriptor GetDescriptor(bool isParameter)
        {
            return isParameter ? Rule_ReadOnlyParameter : Rule_ReadOnlyLocal;
        }

        private static bool IsAllowedArgumentValue(IOperation value)
        {
            return value is IInvocationOperation
                or IObjectCreationOperation
                or IAnonymousObjectCreationOperation
                or IArrayCreationOperation
                or ILiteralOperation
                or IDefaultValueOperation
                or IAnonymousFunctionOperation
                or IDelegateCreationOperation;
        }
    }
}
