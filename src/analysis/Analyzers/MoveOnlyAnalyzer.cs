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
    public sealed class MoveOnlyAnalyzer : DiagnosticAnalyzer
    {
        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_MissingMoveMethod = "SMA0090";
        public const string RuleId_InvalidTypeDeclaration = RuleId_MissingMoveMethod;
        private static readonly DiagnosticDescriptor Rule_MissingMoveMethod = new(
            RuleId_MissingMoveMethod,
            new LocalizableResourceString(nameof(Resources.SMA0090_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0090_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0090_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedCopy = "SMA0091";
        public const string RuleId_NoCopyValueCopy = RuleId_ProhibitedCopy;
        private static readonly DiagnosticDescriptor Rule_ProhibitedCopy = new(
            RuleId_ProhibitedCopy,
            new LocalizableResourceString(nameof(Resources.SMA0091_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0091_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0091_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedRefOutInAsync = "SMA0092";
        public const string RuleId_AsyncRefOutNoCopy = RuleId_ProhibitedRefOutInAsync;
        private static readonly DiagnosticDescriptor Rule_ProhibitedRefOutInAsync = new(
            RuleId_ProhibitedRefOutInAsync,
            new LocalizableResourceString(nameof(Resources.SMA0092_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0092_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0092_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_MissingMoveMethod,
            Rule_ProhibitedCopy,
            Rule_ProhibitedRefOutInAsync
            );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeTypeDeclaration, SymbolKind.NamedType);

            context.RegisterOperationAction(AnalyzeArgumentOperation, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeAssignmentOperation, OperationKind.SimpleAssignment, OperationKind.DeconstructionAssignment);
            context.RegisterOperationAction(AnalyzeVariableDeclaratorOperation, OperationKind.VariableDeclarator);
        }

        /*  MoveOnly helpers & type analysis  ======================================== */

        internal static bool IsMoveOnlyType(ITypeSymbol? type)
        {
            if (type == null)
                return false;

            if (type.Name.StartsWith("MoveOnly", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var attr in type.GetAttributes())
            {
                var name = attr.AttributeClass?.Name;
                if (name == "MoveOnlyAttribute" || name == "MoveOnly")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPublicMoveMethod(INamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers("Move"))
            {
                if (member is IMethodSymbol method &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    !method.IsStatic &&
                    method.Parameters.Length == 0 &&
                    SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AnalyzeTypeDeclaration(SymbolAnalysisContext context)
        {
            if (context.Symbol is not INamedTypeSymbol namedType)
                return;

            if (!IsMoveOnlyType(namedType))
                return;

            // Warn on type identifier if not struct (record or record struct is allowed)
            // Error if missing public Move() method
            bool isStructOrRecord = namedType.IsValueType || namedType.TypeKind == TypeKind.Struct;
            bool hasMove = HasPublicMoveMethod(namedType);

            if (!isStructOrRecord || !hasMove)
            {
                Location location = namedType.Locations.Length > 0 ? namedType.Locations[0] : Location.None;
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_MissingMoveMethod,
                    location,
                    namedType.ToDiagnosticMessageName()));
            }
        }

        /*  MoveOnly usage operations (SMA0091 / SMA0092)  ==================== */

        private static bool IsInsidePublicMoveMethod(ISymbol? containingSymbol)
        {
            if (containingSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.Name == "Move" &&
                    methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                    !methodSymbol.IsStatic &&
                    methodSymbol.Parameters.Length == 0 &&
                    IsMoveOnlyType(methodSymbol.ContainingType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInAsyncContext(ISymbol? containingSymbol)
        {
            if (containingSymbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.IsAsync;
            }

            return false;
        }

        private static bool IsCallingMove(IOperation? expression)
        {
            if (expression == null)
                return false;

            var unwrapped = expression;
            while (unwrapped is IConversionOperation conv)
            {
                unwrapped = conv.Operand;
            }

            if (unwrapped is IInvocationOperation invocation)
            {
                if (invocation.TargetMethod.Name == "Move" && invocation.TargetMethod.Parameters.Length == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AnalyzeArgumentOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation argOp)
                return;

            if (argOp.Value == null || !IsMoveOnlyType(argOp.Value.Type))
                return;

            if (IsInsidePublicMoveMethod(context.ContainingSymbol))
                return;

            if (argOp.Parameter != null)
            {
                if (argOp.Parameter.RefKind == RefKind.Ref || argOp.Parameter.RefKind == RefKind.Out)
                {
                    if (IsInAsyncContext(context.ContainingSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_ProhibitedRefOutInAsync,
                            argOp.Syntax.GetLocation(),
                            argOp.Value.Type.ToDiagnosticMessageName()));
                    }
                    return;
                }
                else if (argOp.Parameter.RefKind == RefKind.In)
                {
                    return;
                }
            }

            // Pass-by-value argument
            if (!IsCallingMove(argOp.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ProhibitedCopy,
                    argOp.Value.Syntax.GetLocation(),
                    argOp.Value.Type.ToDiagnosticMessageName()));
            }
        }

        private static bool IsOutParameterOrReturn(IOperation? target, IOperation currentOp)
        {
            if (target is IParameterReferenceOperation paramRef && paramRef.Parameter.RefKind == RefKind.Out)
            {
                return true;
            }

            var p = currentOp.Parent;
            while (p != null)
            {
                if (p is IReturnOperation)
                {
                    return true;
                }
                if (p is IArgumentOperation || p is ISimpleAssignmentOperation || p is IVariableDeclaratorOperation)
                {
                    break;
                }
                p = p.Parent;
            }

            return false;
        }

        private static void CheckAndReportMoveOnlyCopy(OperationAnalysisContext context, IOperation value)
        {
            var unwrapped = value;
            while (unwrapped is IConversionOperation conv)
            {
                unwrapped = conv.Operand;
            }

            if (unwrapped is ITupleOperation tupleOp)
            {
                foreach (var elem in tupleOp.Elements)
                {
                    CheckAndReportMoveOnlyCopy(context, elem);
                }
                return;
            }

            if (unwrapped.Type != null && IsMoveOnlyType(unwrapped.Type))
            {
                if (!IsCallingMove(unwrapped))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_ProhibitedCopy,
                        unwrapped.Syntax.GetLocation(),
                        unwrapped.Type.ToDiagnosticMessageName()));
                }
            }
        }

        private static void AnalyzeAssignmentOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IAssignmentOperation assignOp)
                return;

            if (assignOp.Value == null)
                return;

            if (IsInsidePublicMoveMethod(context.ContainingSymbol))
                return;

            if (IsOutParameterOrReturn(assignOp.Target, assignOp))
                return;

            CheckAndReportMoveOnlyCopy(context, assignOp.Value);
        }

        private static void AnalyzeVariableDeclaratorOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declOp)
                return;

            var initializer = declOp.Initializer?.Value;
            if (initializer == null)
                return;

            if (IsInsidePublicMoveMethod(context.ContainingSymbol))
                return;

            if (IsOutParameterOrReturn(null, declOp))
                return;

            CheckAndReportMoveOnlyCopy(context, initializer);
        }
    }
}
