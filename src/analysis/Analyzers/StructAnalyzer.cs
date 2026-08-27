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

        public const string RuleId_MissingMoveMethod = "SMA0033";
        public const string RuleId_InvalidTypeDeclaration = RuleId_MissingMoveMethod;
        private static readonly DiagnosticDescriptor Rule_MissingMoveMethod = new(
            RuleId_MissingMoveMethod,
            new LocalizableResourceString(nameof(Resources.SMA0033_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0033_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0033_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedCopy = "SMA0034";
        public const string RuleId_NoCopyValueCopy = RuleId_ProhibitedCopy;
        private static readonly DiagnosticDescriptor Rule_ProhibitedCopy = new(
            RuleId_ProhibitedCopy,
            new LocalizableResourceString(nameof(Resources.SMA0034_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0034_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0034_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ProhibitedRefOutInAsync = "SMA0035";
        public const string RuleId_AsyncRefOutNoCopy = RuleId_ProhibitedRefOutInAsync;
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
            Rule_MissingMoveMethod,
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

            context.RegisterSymbolAction(AnalyzeTypeDeclaration, SymbolKind.NamedType);

            context.RegisterOperationAction(AnalyzeArgumentOperation, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeAssignmentOperation, OperationKind.SimpleAssignment, OperationKind.DeconstructionAssignment);
            context.RegisterOperationAction(AnalyzeVariableDeclaratorOperation, OperationKind.VariableDeclarator);
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


        /*  MoveOnly usage operations (SMA0034 / SMA0035)  ==================== */

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
