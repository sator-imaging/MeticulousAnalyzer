// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StructAnalyzer : DiagnosticAnalyzer
    {
        private const string SuppressionComment = "// Allow boxing";


        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_InvalidStructCtor = "SMA0030";
        private static readonly DiagnosticDescriptor Rule_InvalidStructCtor = new(
            RuleId_InvalidStructCtor,
            new LocalizableResourceString(nameof(Resources.SMA0030_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0030_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0030_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_InvalidReadOnlyField = "SMA0031";
        private static readonly DiagnosticDescriptor Rule_InvalidReadOnlyField = new(
            RuleId_InvalidReadOnlyField,
            new LocalizableResourceString(nameof(Resources.SMA0031_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0031_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0031_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ImplicitBoxing = "SMA0032";
        private static readonly DiagnosticDescriptor Rule_ImplicitBoxing = new(
            RuleId_ImplicitBoxing,
            new LocalizableResourceString(nameof(Resources.SMA0032_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0032_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0032_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_InvalidNoCopyType = "SMA0033";
        private static readonly DiagnosticDescriptor Rule_InvalidNoCopyType = new(
            RuleId_InvalidNoCopyType,
            new LocalizableResourceString(nameof(Resources.SMA0033_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0033_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0033_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedCopy = "SMA0034";
        private static readonly DiagnosticDescriptor Rule_ProhibitedCopy = new(
            RuleId_ProhibitedCopy,
            new LocalizableResourceString(nameof(Resources.SMA0034_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0034_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0034_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedRefOutInAsync = "SMA0035";
        private static readonly DiagnosticDescriptor Rule_ProhibitedRefOutInAsync = new(
            RuleId_ProhibitedRefOutInAsync,
            new LocalizableResourceString(nameof(Resources.SMA0035_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0035_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0035_Description), Resources.ResourceManager, typeof(Resources)));


        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_InvalidStructCtor,
            Rule_InvalidReadOnlyField,
            Rule_ImplicitBoxing,
            Rule_InvalidNoCopyType,
            Rule_ProhibitedCopy,
            Rule_ProhibitedRefOutInAsync
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            context.RegisterOperationAction(AnalyzeUsualConstructor, OperationKind.ObjectCreation);
            context.RegisterOperationAction(AnalyzeAnonymousConstructor, OperationKind.AnonymousObjectCreation);

            context.RegisterSymbolAction(AnalyzeMutableStructField, SymbolKind.Field);

            context.RegisterOperationAction(AnalyzeImplicitBoxing, OperationKind.Conversion);

            context.RegisterSymbolAction(AnalyzeNoCopyTypeDeclaration, SymbolKind.NamedType);
            context.RegisterOperationAction(AnalyzeNoCopyUsage, OperationKind.Argument, OperationKind.VariableDeclarator, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.Tuple);
        }


        /*  MoveOnly / NoCopy  =================================================== */

        private static bool IsMoveOnlyType(ITypeSymbol? type)
        {
            if (type == null)
                return false;

            if (type is INamedTypeSymbol named)
            {
                if (named.Name.Contains("MoveOnly") || named.Name.Contains("NoCopy"))
                    return true;

                foreach (var attr in named.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.Name;
                    if (attrName == "NoCopy" || attrName == "NoCopyAttribute" || attrName == "MoveOnly" || attrName == "MoveOnlyAttribute")
                        return true;
                }
            }

            return false;
        }

        private static void AnalyzeNoCopyTypeDeclaration(SymbolAnalysisContext context)
        {
            if (context.Symbol is not INamedTypeSymbol namedSymbol)
                return;

            if (!IsMoveOnlyType(namedSymbol))
                return;

            if (!namedSymbol.IsValueType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_InvalidNoCopyType,
                    namedSymbol.Locations[0],
                    namedSymbol.ToDiagnosticMessageName(),
                    "is not a struct or record struct"));
                return;
            }

            bool hasValidMove = false;
            foreach (var member in namedSymbol.GetMembers("Move"))
            {
                if (member is IMethodSymbol method &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    !method.IsStatic &&
                    method.Parameters.Length == 0 &&
                    SymbolEqualityComparer.Default.Equals(method.ReturnType, namedSymbol))
                {
                    hasValidMove = true;
                    break;
                }
            }

            if (!hasValidMove)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_InvalidNoCopyType,
                    namedSymbol.Locations[0],
                    namedSymbol.ToDiagnosticMessageName(),
                    "does not have a public parameterless Move() method returning itself"));
            }
        }

        private static bool IsInsideAsyncContext(OperationAnalysisContext context)
        {
            if (context.ContainingSymbol is IMethodSymbol method && method.IsAsync)
                return true;

            var current = context.Operation;
            while (current != null)
            {
                if (current is IAnonymousFunctionOperation lambda && lambda.Symbol.IsAsync)
                    return true;
                if (current is ILocalFunctionOperation localFunc && localFunc.Symbol.IsAsync)
                    return true;
                current = current.Parent;
            }

            return false;
        }

        private static IOperation UnwrapConversion(IOperation op)
        {
            var current = op;
            while (current is IConversionOperation conv)
            {
                current = conv.Operand;
            }
            return current;
        }

        private static bool IsMoveMethodCall(IOperation operation)
        {
            var unwrapped = UnwrapConversion(operation);
            if (unwrapped is IDefaultValueOperation || unwrapped is IObjectCreationOperation)
            {
                return true;
            }

            if (unwrapped is IInvocationOperation invocation)
            {
                if (invocation.TargetMethod.Name == "Move" &&
                    invocation.TargetMethod.Parameters.Length == 0 &&
                    IsMoveOnlyType(invocation.TargetMethod.ContainingType))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsReturnContext(IOperation operation)
        {
            var current = operation.Parent;
            while (current != null)
            {
                if (current is IReturnOperation)
                    return true;
                if (current is IBlockOperation || current is IMethodBodyBaseOperation)
                    break;
                current = current.Parent;
            }
            return false;
        }

        private static void AnalyzeNoCopyUsage(OperationAnalysisContext context)
        {
            // DO NOT check inside public Move() method on the move-only type
            if (context.ContainingSymbol is IMethodSymbol containingMethod &&
                containingMethod.Name == "Move" &&
                IsMoveOnlyType(containingMethod.ContainingType))
            {
                return;
            }

            if (context.Operation is IArgumentOperation arg)
            {
                if (arg.Value == null || !IsMoveOnlyType(arg.Value.Type))
                    return;

                bool isRefOrOut = arg.Parameter != null &&
                    (arg.Parameter.RefKind == RefKind.Ref || arg.Parameter.RefKind == RefKind.Out);

                if (!isRefOrOut)
                {
                    var syntaxStr = arg.Syntax.ToString();
                    if (syntaxStr.StartsWith("ref ") || syntaxStr.StartsWith("out "))
                        isRefOrOut = true;
                }

                if (isRefOrOut)
                {
                    if (IsInsideAsyncContext(context))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_ProhibitedRefOutInAsync,
                            arg.Syntax.GetLocation(),
                            arg.Value.Type!.ToDiagnosticMessageName()));
                    }
                    return;
                }

                bool isPassByRef = arg.Parameter != null && arg.Parameter.RefKind != RefKind.None;
                if (!isPassByRef)
                {
                    var syntaxStr = arg.Syntax.ToString();
                    if (syntaxStr.StartsWith("in "))
                        isPassByRef = true;
                }

                if (!isPassByRef)
                {
                    if (!IsMoveMethodCall(arg.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_ProhibitedCopy,
                            arg.Value.Syntax.GetLocation(),
                            arg.Value.Type!.ToDiagnosticMessageName()));
                    }
                }

                return;
            }

            if (context.Operation is IVariableDeclaratorOperation declarator)
            {
                var initializer = declarator.Initializer?.Value;
                if (initializer == null)
                    return;

                var unwrapped = UnwrapConversion(initializer);
                if (!IsMoveOnlyType(unwrapped.Type))
                    return;

                if (!IsMoveMethodCall(unwrapped))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_ProhibitedCopy,
                        unwrapped.Syntax.GetLocation(),
                        unwrapped.Type!.ToDiagnosticMessageName()));
                }
                return;
            }

            if (context.Operation is IAssignmentOperation assignment)
            {
                if (IsReturnContext(assignment))
                    return;

                var unwrapped = UnwrapConversion(assignment.Value);
                if (!IsMoveOnlyType(unwrapped.Type))
                    return;

                if (!IsMoveMethodCall(unwrapped))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_ProhibitedCopy,
                        unwrapped.Syntax.GetLocation(),
                        unwrapped.Type!.ToDiagnosticMessageName()));
                }
                return;
            }

            if (context.Operation is ITupleOperation tupleOp)
            {
                if (IsReturnContext(tupleOp))
                    return;

                foreach (var element in tupleOp.Elements)
                {
                    var unwrapped = UnwrapConversion(element);
                    if (IsMoveOnlyType(unwrapped.Type) && !IsMoveMethodCall(unwrapped))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_ProhibitedCopy,
                            element.Syntax.GetLocation(),
                            unwrapped.Type!.ToDiagnosticMessageName()));
                    }
                }
                return;
            }
        }


        /*  ctor  ================================================================ */

        private static void AnalyzeUsualConstructor(OperationAnalysisContext context)
        {
            if (context.Operation is not IObjectCreationOperation op || !op.Type.IsValueType)
                return;

            if (op.Arguments.Length == 0 && op.Type is INamedTypeSymbol namedSymbol)
            {
                AnalyzeConstructor_Impl(context, namedSymbol);
            }
        }

        private static void AnalyzeAnonymousConstructor(OperationAnalysisContext context)
        {
            if (context.Operation is not IAnonymousObjectCreationOperation op || !op.Type.IsValueType)
                return;

            if (!op.Children.OfType_Any<IArgumentOperation>() && op.Type is INamedTypeSymbol namedSymbol)
            {
                AnalyzeConstructor_Impl(context, namedSymbol);
            }
        }


        private static void AnalyzeConstructor_Impl(OperationAnalysisContext context,
                                                    INamedTypeSymbol structSymbol
            )
        {
            var hasCtor = structSymbol.InstanceConstructors
                .Where_Any(static x => x.Parameters.Length > 0)
                //.Where(static x => (x.DeclaredAccessibility & ~(Accessibility.Private | Accessibility.NotApplicable)) != 0)
                ;

            if (!hasCtor)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_InvalidStructCtor, context.Operation.Syntax.GetLocation(), structSymbol.ToDiagnosticMessageName()));
        }


        /*  mutable struct  ================================================================ */

        private static void AnalyzeMutableStructField(SymbolAnalysisContext context)
        {
            if (context.Symbol is not IFieldSymbol symbol)
                return;

            if (!symbol.IsReadOnly || symbol.IsImplicitlyDeclared || !symbol.Type.IsValueType)
                return;

            AnalyzeMutableStructField_Impl(context, symbol);
        }

        private static void AnalyzeMutableStructField_Impl(SymbolAnalysisContext context, IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.Type is not ITypeSymbol typeSymbol)
                return;

            // if it is Nullable<T>, check T instead.
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType
                && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                typeSymbol = namedType.TypeArguments[0];
            }

            // NOTE: int or other elder primitive types are NOT readonly struct.
            if (Core.IsKnownImmutableType(typeSymbol))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_InvalidReadOnlyField, fieldSymbol.Locations[0], typeSymbol.ToDiagnosticMessageName()));
        }


        /*  implicit boxing  ================================================================ */

        private static void AnalyzeImplicitBoxing(OperationAnalysisContext context)
        {
            if (context.Operation is not IConversionOperation op)
                return;

            if (!op.IsImplicit || op.Type == null || op.Operand.Type == null)
                return;

            // Boxing conversion from value type to reference type (including interface)
            if (op.Operand.Type.IsValueType && op.Type.IsReferenceType)
            {
                if (Core.IsSuppressedByComment(op, SuppressionComment))
                    return;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ImplicitBoxing, op.Syntax.GetLocation(),
                    op.Operand.Type.ToDiagnosticMessageName(),
                    op.Type.ToDiagnosticMessageName()));
            }
        }
    }
}
