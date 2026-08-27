// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private static readonly DiagnosticDescriptor Rule_MissingMoveMethod = new(
            RuleId_MissingMoveMethod,
            new LocalizableResourceString(nameof(Resources.SMA0033_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0033_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0033_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_NoCopyValueCopy = "SMA0034";
        private static readonly DiagnosticDescriptor Rule_NoCopyValueCopy = new(
            RuleId_NoCopyValueCopy,
            new LocalizableResourceString(nameof(Resources.SMA0034_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0034_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0034_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_AsyncRefOutNoCopy = "SMA0035";
        private static readonly DiagnosticDescriptor Rule_AsyncRefOutNoCopy = new(
            RuleId_AsyncRefOutNoCopy,
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
            Rule_NoCopyValueCopy,
            Rule_AsyncRefOutNoCopy
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

            context.RegisterSymbolAction(AnalyzeNoCopyType, SymbolKind.NamedType);
            context.RegisterOperationAction(AnalyzeNoCopyArgument, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeNoCopyVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzeNoCopyAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(AnalyzeNoCopyDeconstruction, OperationKind.DeconstructionAssignment);
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


        /*  NoCopy  ================================================================ */

        private static bool IsNoCopyType(ITypeSymbol? type)
        {
            if (type == null)
                return false;

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsGenericType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    namedType = (INamedTypeSymbol)namedType.TypeArguments[0];
                }

                foreach (var attr in namedType.GetAttributes())
                {
                    var name = attr.AttributeClass?.Name;
                    if (name is "NoCopy" or "NoCopyAttribute")
                        return true;
                }
            }

            return false;
        }

        private static IOperation UnwrapConversions(IOperation operation)
        {
            var current = operation;
            while (current is IConversionOperation conversion)
            {
                current = conversion.Operand;
            }
            return current;
        }

        private static bool IsAllowedNoCopyExpression(IOperation operation)
        {
            var val = UnwrapConversions(operation);

            if (val is IObjectCreationOperation or IDefaultValueOperation)
                return true;

            if (val is IInvocationOperation invocation)
            {
                if (invocation.TargetMethod.Name == "Move")
                    return true;
            }

            return false;
        }

        private static void AnalyzeNoCopyType(SymbolAnalysisContext context)
        {
            if (context.Symbol is not INamedTypeSymbol namedType)
                return;

            if (!IsNoCopyType(namedType))
                return;

            var hasValidMoveMethod = false;
            foreach (var member in namedType.GetMembers("Move"))
            {
                if (member is IMethodSymbol method
                    && !method.IsStatic
                    && method.DeclaredAccessibility == Accessibility.Public
                    && method.Parameters.IsEmpty
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, namedType))
                {
                    hasValidMoveMethod = true;
                    break;
                }
            }

            if (!hasValidMoveMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_MissingMoveMethod,
                    namedType.Locations[0],
                    namedType.ToDiagnosticMessageName()));
            }
        }

        private static bool IsAsyncContext(IOperation operation, OperationAnalysisContext context)
        {
            var syntax = operation.Syntax;
            foreach (var ancestor in syntax.Ancestors())
            {
                if (ancestor is MethodDeclarationSyntax mds)
                    return mds.Modifiers.Any(SyntaxKind.AsyncKeyword);
                if (ancestor is LocalFunctionStatementSyntax lfss)
                    return lfss.Modifiers.Any(SyntaxKind.AsyncKeyword);
                if (ancestor is AnonymousFunctionExpressionSyntax afes)
                    return afes.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword;
                if (ancestor is AccessorDeclarationSyntax)
                    return false;
            }

            if (context.ContainingSymbol is IMethodSymbol method)
                return method.IsAsync;

            return false;
        }

        private static void AnalyzeNoCopyArgument(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation arg)
                return;

            var val = UnwrapConversions(arg.Value);
            if (!IsNoCopyType(val.Type))
                return;

            var refKind = arg.Parameter?.RefKind ?? RefKind.None;

            if (refKind is RefKind.Ref or RefKind.Out)
            {
                if (IsAsyncContext(arg, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_AsyncRefOutNoCopy,
                        arg.Syntax.GetLocation(),
                        val.Type.ToDiagnosticMessageName()));
                }
            }
            else if (refKind is RefKind.In or RefKind.RefReadOnly)
            {
                // Allowed
            }
            else // RefKind.None (Pass by value)
            {
                if (!IsAllowedNoCopyExpression(val))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_NoCopyValueCopy,
                        arg.Syntax.GetLocation(),
                        val.Type.ToDiagnosticMessageName()));
                }
            }
        }

        private static void AnalyzeNoCopyVariableDeclarator(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declarator)
                return;

            if (declarator.Initializer != null)
            {
                CheckNoCopyCopy(context, declarator.Initializer.Value);
            }
        }

        private static void AnalyzeNoCopyAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not ISimpleAssignmentOperation assignment)
                return;

            if (assignment.Parent is IDeconstructionAssignmentOperation)
                return;

            CheckNoCopyCopy(context, assignment.Value);
        }

        private static void AnalyzeNoCopyDeconstruction(OperationAnalysisContext context)
        {
            if (context.Operation is not IDeconstructionAssignmentOperation deconstruction)
                return;

            CheckNoCopyCopy(context, deconstruction.Value);
        }

        private static void CheckNoCopyCopy(OperationAnalysisContext context, IOperation valueOperation)
        {
            var val = UnwrapConversions(valueOperation);
            if (val is ITupleOperation tuple)
            {
                foreach (var element in tuple.Elements)
                {
                    CheckNoCopyCopy(context, element);
                }
                return;
            }

            if (val is IDeconstructionAssignmentOperation deconstruction)
            {
                CheckNoCopyCopy(context, deconstruction.Value);
                return;
            }

            if (IsNoCopyType(val.Type) && !IsAllowedNoCopyExpression(val))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_NoCopyValueCopy,
                    valueOperation.Syntax.GetLocation(),
                    val.Type.ToDiagnosticMessageName()));
            }
        }
    }
}
