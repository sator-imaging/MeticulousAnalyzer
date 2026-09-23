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
            Core.CategoryPrefix + nameof(StructAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0030_MessageFormat), Resources.ResourceManager, typeof(Resources), "$type"));

        public const string RuleId_InvalidReadOnlyField = "SMA0031";
        private static readonly DiagnosticDescriptor Rule_InvalidReadOnlyField = new(
            RuleId_InvalidReadOnlyField,
            new LocalizableResourceString(nameof(Resources.SMA0031_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0031_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(StructAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0031_MessageFormat), Resources.ResourceManager, typeof(Resources), "$type"));

        public const string RuleId_ImplicitBoxing = "SMA0032";
        private static readonly DiagnosticDescriptor Rule_ImplicitBoxing = new(
            RuleId_ImplicitBoxing,
            new LocalizableResourceString(nameof(Resources.SMA0032_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0032_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(StructAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0032_MessageFormat), Resources.ResourceManager, typeof(Resources), "$type1", "$type2"));

        public const string RuleId_InArgumentDefensiveCopy = "SMA0033";
        private static readonly DiagnosticDescriptor Rule_InArgumentDefensiveCopy = new(
            RuleId_InArgumentDefensiveCopy,
            new LocalizableResourceString(nameof(Resources.SMA0033_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0033_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(StructAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0033_MessageFormat), Resources.ResourceManager, typeof(Resources), "$type"));

        public const string RuleId_RefReadonlyDefensiveCopy = "SMA0034";
        private static readonly DiagnosticDescriptor Rule_RefReadonlyDefensiveCopy = new(
            RuleId_RefReadonlyDefensiveCopy,
            new LocalizableResourceString(nameof(Resources.SMA0034_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0034_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(StructAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0034_MessageFormat), Resources.ResourceManager, typeof(Resources), "$type"));


        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_InvalidStructCtor,
            Rule_InvalidReadOnlyField,
            Rule_ImplicitBoxing,
            Rule_InArgumentDefensiveCopy,
            Rule_RefReadonlyDefensiveCopy
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

            context.RegisterOperationAction(AnalyzeInArgumentDefensiveCopy, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeRefReadonlyDefensiveCopy, OperationKind.Invocation, OperationKind.PropertyReference);
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


        /*  in argument defensive copy  ================================================================ */

        private static void AnalyzeInArgumentDefensiveCopy(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation op)
                return;

            if (op.Parameter == null || op.Parameter.RefKind != RefKind.In)
                return;

            var valueOp = op.Value;
            if (valueOp == null)
                return;

            var type = valueOp.Type;
            if (type == null || type.TypeKind != TypeKind.Struct || type.IsReadOnly || Core.IsKnownImmutableType(type))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_InArgumentDefensiveCopy,
                valueOp.Syntax.GetLocation(),
                type.ToDiagnosticMessageName()));
        }


        /*  ref readonly defensive copy  ================================================================ */

        private static void AnalyzeRefReadonlyDefensiveCopy(OperationAnalysisContext context)
        {
            IOperation? instanceOp = null;

            if (context.Operation is IInvocationOperation invocation)
            {
                if (invocation.TargetMethod == null || invocation.TargetMethod.IsStatic || invocation.TargetMethod.IsReadOnly)
                    return;

                instanceOp = invocation.Instance;
            }
            else if (context.Operation is IPropertyReferenceOperation propRef)
            {
                if (propRef.Property == null || propRef.Property.IsStatic)
                    return;

                var getter = propRef.Property.GetMethod;
                if (getter != null && getter.IsReadOnly)
                    return;

                instanceOp = propRef.Instance;
            }

            if (instanceOp == null)
                return;

            instanceOp = instanceOp.UnwrapConversion();
            var type = instanceOp.Type;

            if (type == null || type.TypeKind != TypeKind.Struct || type.IsReadOnly || Core.IsKnownImmutableType(type))
                return;

            if (!IsRefReadonlyReference(instanceOp, context.ContainingSymbol, out var location))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_RefReadonlyDefensiveCopy,
                location,
                type.ToDiagnosticMessageName()));
        }

        private static bool IsRefReadonlyReference(IOperation instanceOp, ISymbol containingSymbol, out Location location)
        {
            location = instanceOp.Syntax.GetLocation();

            if (instanceOp is ILocalReferenceOperation localRef)
            {
                var local = localRef.Local;
                if (local != null && local.IsRef && (local.RefKind == RefKind.RefReadOnly || local.RefKind == RefKind.In))
                {
                    location = GetRefReadonlyLocation(local.DeclaringSyntaxReferences) ?? location;
                    return true;
                }
            }
            else if (instanceOp is IParameterReferenceOperation paramRef)
            {
                var param = paramRef.Parameter;
                if (param != null && (param.RefKind == RefKind.RefReadOnly || param.RefKind == RefKind.In))
                {
                    location = GetRefReadonlyLocation(param.DeclaringSyntaxReferences) ?? location;
                    return true;
                }
            }
            else if (instanceOp is IInvocationOperation methodCall)
            {
                if (methodCall.TargetMethod != null && (methodCall.TargetMethod.RefKind == RefKind.RefReadOnly || methodCall.TargetMethod.RefKind == RefKind.In))
                {
                    location = GetRefReadonlyLocation(methodCall.TargetMethod.DeclaringSyntaxReferences) ?? location;
                    return true;
                }
            }
            else if (instanceOp is IPropertyReferenceOperation propRef)
            {
                if (propRef.Property != null && (propRef.Property.RefKind == RefKind.RefReadOnly || propRef.Property.RefKind == RefKind.In))
                {
                    location = GetRefReadonlyLocation(propRef.Property.DeclaringSyntaxReferences) ?? location;
                    return true;
                }
            }
            else if (instanceOp is IFieldReferenceOperation fieldRef)
            {
                if (fieldRef.Field != null && fieldRef.Field.IsReadOnly)
                {
                    location = GetRefReadonlyLocation(fieldRef.Field.DeclaringSyntaxReferences) ?? location;
                    return true;
                }
            }
            else if (instanceOp is IInstanceReferenceOperation)
            {
                if (containingSymbol is IMethodSymbol containingMethod && containingMethod.IsReadOnly)
                {
                    return true;
                }
            }

            return false;
        }

        private static Location? GetRefReadonlyLocation(ImmutableArray<SyntaxReference> syntaxReferences)
        {
            if (syntaxReferences.IsDefaultOrEmpty)
                return null;

            var syntaxNode = syntaxReferences[0].GetSyntax();
            if (syntaxNode == null)
                return null;

            var current = syntaxNode;
            for (int i = 0; i < 3 && current != null; i++)
            {
                foreach (var refType in current.DescendantNodesAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.RefTypeSyntax>())
                {
                    if (!refType.RefKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None) &&
                        !refType.ReadOnlyKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None))
                    {
                        var start = refType.RefKeyword.SpanStart;
                        var end = refType.ReadOnlyKeyword.Span.End;
                        return Location.Create(current.SyntaxTree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, end));
                    }
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
