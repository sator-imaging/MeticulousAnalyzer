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
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0090_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_InvalidTypeDeclaration = new(
            RuleId_InvalidTypeDeclaration,
            new LocalizableResourceString(nameof(Resources.SMA0093_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0093_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0093_Description), Resources.ResourceManager, typeof(Resources)));

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
            Rule_MissingMoveMethod,
            Rule_InvalidTypeDeclaration,
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMoveOnlyType(ITypeSymbol? type)
        {
            if (type == null)
                return false;

            return _moveOnlyTypeCache.GetValue(type, static t => new StrongBox<bool>(t.Name.StartsWith("MoveOnly", StringComparison.Ordinal))).Value;
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

            if (argOp.Value == null || !IsMoveOnlyType(argOp.Value.Type))
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
                    // 2) passing to sync method (returns non-Task)
                    // 3) passing to async method (returns Task/ValueTask) that has `await`
                    bool isCtor = argOp.Parent is IObjectCreationOperation;
                    bool isAllowed = isCtor;

                    if (!isAllowed && argOp.Parent is IInvocationOperation invocationOp && invocationOp.TargetMethod is IMethodSymbol targetMethod)
                    {
                        if (!targetMethod.IsAsync)
                        {
                            var returnType = targetMethod.ReturnType;
                            bool isTaskReturning = returnType is INamedTypeSymbol
                            {
                                Name: "Task" or "ValueTask", ContainingNamespace: INamespaceSymbol
                                {
                                    Name: "Tasks", ContainingNamespace: INamespaceSymbol
                                    {
                                        Name: "Threading", ContainingNamespace: INamespaceSymbol
                                        {
                                            Name: "System", ContainingNamespace: INamespaceSymbol
                                            {
                                                IsGlobalNamespace: true
                                            }
                                        }
                                    }
                                }
                            };

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
    }
}
