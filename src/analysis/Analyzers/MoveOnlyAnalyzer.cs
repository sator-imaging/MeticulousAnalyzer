// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MoveOnlyAnalyzer : DiagnosticAnalyzer
    {
        private const string MoveMethodName = "Move";
        private static readonly ConditionalWeakTable<ITypeSymbol, StrongBox<bool>> _moveOnlyTypeCache = new();
        private static readonly ConditionalWeakTable<INamedTypeSymbol, StrongBox<bool>> _hasPublicMoveMethodCache = new();
        private static readonly ConditionalWeakTable<IMethodSymbol, StrongBox<bool>> _insidePublicMoveMethodCache = new();

        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_MissingMoveMethod = "SMA0090";
        public const string RuleId_InvalidTypeDeclaration = "SMA0093";
        private static readonly DiagnosticDescriptor Rule_MissingMoveMethod = new(
            RuleId_MissingMoveMethod,
            new LocalizableResourceString(nameof(Resources.SMA0090_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0090_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0090_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_InvalidTypeDeclaration = new(
            RuleId_InvalidTypeDeclaration,
            new LocalizableResourceString(nameof(Resources.SMA0093_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0093_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0093_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedCopy = "SMA0091";
        public const string RuleId_NoCopyValueCopy = RuleId_ProhibitedCopy;
        private static readonly DiagnosticDescriptor Rule_ProhibitedCopy = new(
            RuleId_ProhibitedCopy,
            new LocalizableResourceString(nameof(Resources.SMA0091_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0091_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0091_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedRefOutInAsync = "SMA0092";
        public const string RuleId_AsyncRefOutNoCopy = RuleId_ProhibitedRefOutInAsync;
        private static readonly DiagnosticDescriptor Rule_ProhibitedRefOutInAsync = new(
            RuleId_ProhibitedRefOutInAsync,
            new LocalizableResourceString(nameof(Resources.SMA0092_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0092_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0092_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedCast = "SMA0094";
        private static readonly DiagnosticDescriptor Rule_ProhibitedCast = new(
            RuleId_ProhibitedCast,
            new LocalizableResourceString(nameof(Resources.SMA0094_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0094_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0094_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedLambdaCapture = "SMA0095";
        private static readonly DiagnosticDescriptor Rule_ProhibitedLambdaCapture = new(
            RuleId_ProhibitedLambdaCapture,
            new LocalizableResourceString(nameof(Resources.SMA0095_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0095_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0095_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedOutParameter = "SMA0096";
        private static readonly DiagnosticDescriptor Rule_ProhibitedOutParameter = new(
            RuleId_ProhibitedOutParameter,
            new LocalizableResourceString(nameof(Resources.SMA0096_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0096_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0096_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedReturn = "SMA0097";
        private static readonly DiagnosticDescriptor Rule_ProhibitedReturn = new(
            RuleId_ProhibitedReturn,
            new LocalizableResourceString(nameof(Resources.SMA0097_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0097_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MoveOnlyAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0097_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_MissingMoveMethod,
            Rule_InvalidTypeDeclaration,
            Rule_ProhibitedCopy,
            Rule_ProhibitedRefOutInAsync,
            Rule_ProhibitedCast,
            Rule_ProhibitedLambdaCapture,
            Rule_ProhibitedOutParameter,
            Rule_ProhibitedReturn
            );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeTypeDeclaration, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeParameterDeclaration, SymbolKind.Parameter);

            context.RegisterOperationAction(AnalyzeArgumentOperation, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeAssignmentOperation, OperationKind.SimpleAssignment, OperationKind.DeconstructionAssignment);
            context.RegisterOperationAction(AnalyzeVariableDeclaratorOperation, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzeReturnOperation, OperationKind.Return);
            context.RegisterOperationAction(AnalyzeConversionOperation, OperationKind.Conversion);
            context.RegisterOperationAction(AnalyzeAnonymousFunctionOperation, OperationKind.AnonymousFunction);
        }

        /*  MoveOnly helpers & type analysis  ======================================== */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMoveOnlyType(ITypeSymbol? type)
        {
            if (type == null)
                return false;

            return _moveOnlyTypeCache.GetValue(type, static t => new StrongBox<bool>(ComputeIsMoveOnlyType(t))).Value;
        }

        private static bool ComputeIsMoveOnlyType(ITypeSymbol type)
        {
            if (type.Name.StartsWith("MoveOnly", StringComparison.Ordinal))
                return true;

            foreach (var attr in type.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "NoCopyAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFieldOrPropertyAssignmentInMoveOnlyStructCtor(IOperation? target, ISymbol? containingSymbol)
        {
            if (containingSymbol is IMethodSymbol methodSymbol &&
                methodSymbol.MethodKind == MethodKind.Constructor &&
                methodSymbol.ContainingType != null &&
                methodSymbol.ContainingType.IsValueType &&
                IsMoveOnlyType(methodSymbol.ContainingType))
            {
                if (target is IFieldReferenceOperation || target is IPropertyReferenceOperation)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasPublicMoveMethod(INamedTypeSymbol type)
        {
            return _hasPublicMoveMethodCache.GetValue(type, static t => new StrongBox<bool>(ComputeHasPublicMoveMethod(t))).Value;
        }

        private static bool ComputeHasPublicMoveMethod(INamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers(MoveMethodName))
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
            Location location = namedType.Locations.Length > 0 ? namedType.Locations[0] : Location.None;

            if (!namedType.IsValueType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_InvalidTypeDeclaration,
                    location,
                    namedType.ToDiagnosticMessageName()));
            }
            else
            {
                if (!HasPublicMoveMethod(namedType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_MissingMoveMethod,
                        location,
                        namedType.ToDiagnosticMessageName()));
                }
            }
        }

        /*  MoveOnly usage operations (SMA0091 / SMA0092)  ==================== */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInsidePublicMoveMethod(ISymbol? containingSymbol)
        {
            if (containingSymbol is not IMethodSymbol methodSymbol)
                return false;

            return _insidePublicMoveMethodCache.GetValue(methodSymbol, static m => new StrongBox<bool>(
                m.ContainingType is INamedTypeSymbol type &&
                IsMoveOnlyType(type) &&
                HasPublicMoveMethod(type) &&
                m.Name == MoveMethodName
            )).Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInAsyncContext(ISymbol? containingSymbol)
        {
            return containingSymbol is IMethodSymbol methodSymbol && methodSymbol.IsAsync;
        }

        private static void AnalyzeParameterDeclaration(SymbolAnalysisContext context)
        {
            if (context.Symbol is not IParameterSymbol { RefKind: RefKind.Out } parameter)
                return;

            if (!IsMoveOnlyType(parameter.Type))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_ProhibitedOutParameter,
                parameter.Locations[0],
                parameter.Type.ToDiagnosticMessageName()));
        }

        private static bool IsCallingMove(IOperation? expression)
        {
            if (expression == null)
                return false;

            var unwrapped = expression.UnwrapConversion();

            if (unwrapped is IInvocationOperation invocation)
            {
                if (invocation.TargetMethod.Name == MoveMethodName && invocation.TargetMethod.Parameters.Length == 0)
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

            if (argOp.Value == null)
                return;

            if (argOp.Value is IConversionOperation conv &&
                conv.Operand != null && conv.Operand.Type != null && conv.Type != null &&
                IsMoveOnlyType(conv.Operand.Type) &&
                !SymbolEqualityComparer.Default.Equals(conv.Operand.Type, conv.Type))
            {
                return;
            }

            if (!IsMoveOnlyType(argOp.Value.Type))
                return;

            if (IsInsidePublicMoveMethod(context.ContainingSymbol))
                return;

            bool isRefOutIn = argOp.Parameter != null &&
                (argOp.Parameter.RefKind == RefKind.Ref ||
                 argOp.Parameter.RefKind == RefKind.Out ||
                 argOp.Parameter.RefKind == RefKind.In);

            if (isRefOutIn)
            {
                if (IsInAsyncContext(context.ContainingSymbol))
                {
                    // Allow passing with in/ref/out in async method ONLY WHEN:
                    // 1) passing to constructor (argOp.Parent is IObjectCreationOperation)
                    // 2) passing to sync method (returns non-Task/ValueTask)
                    // 3) passing to async method (returns Task/ValueTask) that has `await`
                    bool isCtor = argOp.Parent is IObjectCreationOperation;
                    bool isAllowed = isCtor;

                    if (!isAllowed && argOp.Parent is IInvocationOperation invocationOp && invocationOp.TargetMethod is IMethodSymbol targetMethod)
                    {
                        if (!targetMethod.IsAsync)
                        {
                            var returnType = targetMethod.ReturnType;
                            bool isTaskReturning = returnType.IsTaskLikeType();

                            if (!isTaskReturning)
                            {
                                isAllowed = true; // passing to sync method
                            }
                            else if (invocationOp.Parent is IAwaitOperation)
                            {
                                isAllowed = true; // passing to async method that has await
                            }
                        }
                        else if (invocationOp.Parent is IAwaitOperation)
                        {
                            isAllowed = true; // passing to async method that has await
                        }
                    }

                    if (!isAllowed)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_ProhibitedRefOutInAsync,
                            argOp.Syntax.GetLocation(),
                            argOp.Value.Type.ToDiagnosticMessageName()));
                    }
                }
            }
            else
            {
                // Pass-by-value argument
                if (!IsCallingMove(argOp.Value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_ProhibitedCopy,
                        argOp.Value.Syntax.GetLocation(),
                        argOp.Value.Type.ToDiagnosticMessageName()));
                }
            }
        }

        private static void CheckAndReportMoveOnlyCopy(OperationAnalysisContext context, IOperation value)
        {
            var current = value;
            while (current is IConversionOperation conv)
            {
                if (conv.Operand != null && conv.Operand.Type != null && conv.Type != null &&
                    IsMoveOnlyType(conv.Operand.Type) &&
                    !SymbolEqualityComparer.Default.Equals(conv.Operand.Type, conv.Type))
                {
                    return;
                }
                current = conv.Operand;
            }

            var unwrapped = current.UnwrapConversion();
            if (unwrapped == null)
            {
                return;
            }

            if (unwrapped is ITupleOperation tupleOp)
            {
                foreach (var elem in tupleOp.Elements)
                {
                    CheckAndReportMoveOnlyCopy(context, elem);
                }
                return;
            }

            // 'new T(...)' and 'default(T)' do not copy an existing instance.
            if (unwrapped is IObjectCreationOperation || unwrapped is IDefaultValueOperation)
            {
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

            if (assignOp.Target is IParameterReferenceOperation paramRef && paramRef.Parameter.RefKind == RefKind.Out)
                return;

            if (IsFieldOrPropertyAssignmentInMoveOnlyStructCtor(assignOp.Target, context.ContainingSymbol))
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

            CheckAndReportMoveOnlyCopy(context, initializer);
        }

        private static void AnalyzeReturnOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IReturnOperation { ReturnedValue: { } returnedValue })
                return;

            if (context.ContainingSymbol is not IMethodSymbol methodSymbol ||
                methodSymbol.ReturnsByRef ||
                methodSymbol.ReturnsByRefReadonly ||
                IsInsidePublicMoveMethod(methodSymbol))
            {
                return;
            }

            if (returnedValue.Type == null || !IsMoveOnlyType(returnedValue.Type))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_ProhibitedReturn,
                returnedValue.Syntax.GetLocation(),
                returnedValue.Type.ToDiagnosticMessageName()));
        }

        private static void AnalyzeConversionOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IConversionOperation convOp)
                return;

            if (convOp.Operand == null || convOp.Operand.Type == null || convOp.Type == null)
                return;

            if (!IsMoveOnlyType(convOp.Operand.Type))
                return;

            if (SymbolEqualityComparer.Default.Equals(convOp.Operand.Type, convOp.Type))
                return;

            if (IsInsidePublicMoveMethod(context.ContainingSymbol))
                return;

            if (!IsCallingMove(convOp.Operand))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ProhibitedCast,
                    convOp.Syntax.GetLocation(),
                    convOp.Operand.Type.ToDiagnosticMessageName(),
                    convOp.Type.ToDiagnosticMessageName()));
            }
        }

        private static void AnalyzeAnonymousFunctionOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IAnonymousFunctionOperation anonFunc)
                return;

            CheckOperationForCapturedMoveOnly(context, anonFunc, anonFunc);
        }

        private static void CheckOperationForCapturedMoveOnly(OperationAnalysisContext context, IAnonymousFunctionOperation rootLambda, IOperation currentOp)
        {
            foreach (var child in currentOp.Children)
            {
                if (child == null)
                    continue;

                if (child is IAnonymousFunctionOperation)
                    continue;

                CheckCapturedMoveOnlyInNode(context, rootLambda, child);
                CheckOperationForCapturedMoveOnly(context, rootLambda, child);
            }
        }

        private static void CheckCapturedMoveOnlyInNode(OperationAnalysisContext context, IAnonymousFunctionOperation rootLambda, IOperation op)
        {
            ITypeSymbol? type = null;
            ISymbol? symbol = null;

            if (op is ILocalReferenceOperation localRef)
            {
                symbol = localRef.Local;
                type = localRef.Local.Type;
            }
            else if (op is IParameterReferenceOperation paramRef)
            {
                symbol = paramRef.Parameter;
                type = paramRef.Parameter.Type;
            }
            else if (op is IInstanceReferenceOperation instanceRef)
            {
                symbol = instanceRef.Type;
                type = instanceRef.Type;
            }
            else if (op is IFieldReferenceOperation fieldRef && !fieldRef.Field.IsStatic)
            {
                if (IsMoveOnlyType(fieldRef.Field.Type))
                {
                    if (fieldRef.Instance != null && IsOuterSymbolReference(fieldRef.Instance, rootLambda.Symbol, out var instType) && !IsMoveOnlyType(instType))
                    {
                        symbol = fieldRef.Field;
                        type = fieldRef.Field.Type;
                    }
                }
            }
            else if (op is IPropertyReferenceOperation propRef && !propRef.Property.IsStatic)
            {
                if (IsMoveOnlyType(propRef.Property.Type))
                {
                    if (propRef.Instance != null && IsOuterSymbolReference(propRef.Instance, rootLambda.Symbol, out var instType) && !IsMoveOnlyType(instType))
                    {
                        symbol = propRef.Property;
                        type = propRef.Property.Type;
                    }
                }
            }

            if (symbol == null || type == null)
                return;

            if (!IsMoveOnlyType(type))
                return;

            if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingSymbol, rootLambda.Symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ProhibitedLambdaCapture,
                    op.Syntax.GetLocation(),
                    type.ToDiagnosticMessageName()));
            }
        }

        private static bool IsOuterSymbolReference(IOperation instanceOp, ISymbol lambdaSymbol, out ITypeSymbol? instType)
        {
            instType = instanceOp.Type;
            ISymbol? sym = null;
            if (instanceOp is ILocalReferenceOperation localRef)
            {
                sym = localRef.Local;
            }
            else if (instanceOp is IParameterReferenceOperation paramRef)
            {
                sym = paramRef.Parameter;
            }
            else if (instanceOp is IInstanceReferenceOperation instanceRef)
            {
                sym = instanceRef.Type;
            }

            if (sym != null)
            {
                return !SymbolEqualityComparer.Default.Equals(sym.ContainingSymbol, lambdaSymbol);
            }

            return false;
        }
    }
}
