// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InternalNamespaceAccessAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_InternalNamespaceAccess = "SMA0080";

        private static readonly DiagnosticDescriptor Rule_InternalNamespaceAccess = new(
            RuleId_InternalNamespaceAccess,
            new LocalizableResourceString(nameof(Resources.SMA0080_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0080_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0080_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule_InternalNamespaceAccess);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(
                AnalyzeTypeOperand,
                OperationKind.DefaultValue,
                OperationKind.TypeOf,
                OperationKind.IsType,
                OperationKind.Conversion,
                OperationKind.TypeParameterObjectCreation,
                OperationKind.ArrayCreation);

            context.RegisterOperationAction(
                AnalyzeSymbolMember,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.EventReference,
                OperationKind.EventAssignment,
                OperationKind.MethodReference,
                OperationKind.Invocation,
                OperationKind.ObjectCreation);

            context.RegisterOperationAction(AnalyzePattern, OperationKind.IsPattern);
            context.RegisterOperationAction(AnalyzeNameOf, OperationKind.NameOf);
        }

        private static void AnalyzeTypeOperand(OperationAnalysisContext context)
        {
            var type = TryGetNamedTypeFromOperation(context.Operation);
            if (type != null)
            {
                ReportCrossNamespaceAccess(context, context.Operation, type);
            }
        }

        private static void AnalyzeSymbolMember(OperationAnalysisContext context)
        {
            var operation = context.Operation;
            var symbol = TryGetSymbolFromMemberOperation(operation);
            if (symbol != null)
            {
                ReportCrossNamespaceAccess(context, operation, symbol);
            }
        }

        private static void AnalyzePattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IIsPatternOperation isPattern)
            {
                return;
            }

            var type = GetPatternTypeSymbol(isPattern.Pattern);
            if (type != null)
            {
                ReportCrossNamespaceAccess(context, isPattern, type);
            }
        }

        private static void AnalyzeNameOf(OperationAnalysisContext context)
        {
            if (context.Operation is not INameOfOperation nameOf)
            {
                return;
            }

            var symbol = TryGetNameOfSymbolFromOperation(nameOf);
            if (symbol == null)
            {
                symbol = TryGetNameOfSymbolFromSemanticModel(nameOf);
            }

            if (symbol != null)
            {
                ReportCrossNamespaceAccess(context, nameOf, symbol);
            }
        }

        private static INamedTypeSymbol? TryGetNamedTypeFromOperation(IOperation operation) =>
            operation switch
            {
                IDefaultValueOperation defaultValue => defaultValue.Type as INamedTypeSymbol,
                ITypeOfOperation typeOf => typeOf.TypeOperand as INamedTypeSymbol,
                IIsTypeOperation isType => isType.TypeOperand as INamedTypeSymbol,
                IConversionOperation conversion => TryGetNamedTypeFromConversion(conversion),
                ITypeParameterObjectCreationOperation typeParamCreation =>
                    typeParamCreation.Type is INamedTypeSymbol namedType && namedType.TypeKind != TypeKind.TypeParameter
                        ? namedType
                        : null,
                IArrayCreationOperation arrayCreation => TryGetArrayElementNamedType(arrayCreation),
                _ => null
            };

        private static INamedTypeSymbol? TryGetNamedTypeFromConversion(IConversionOperation conversion)
        {
            if (conversion.Operand is IObjectCreationOperation or ITypeParameterObjectCreationOperation)
            {
                return null;
            }

            return conversion.Type as INamedTypeSymbol;
        }

        private static INamedTypeSymbol? TryGetArrayElementNamedType(IArrayCreationOperation arrayCreation)
        {
            if (arrayCreation.Type is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType as INamedTypeSymbol;
            }

            return arrayCreation.Type as INamedTypeSymbol;
        }

        private static ISymbol? TryGetSymbolFromMemberOperation(IOperation operation) =>
            operation switch
            {
                IFieldReferenceOperation fieldRef when !IsUnderEventAssignment(fieldRef) => fieldRef.Field,
                IPropertyReferenceOperation propertyRef when !IsUnderEventAssignment(propertyRef) => propertyRef.Property,
                IEventReferenceOperation eventRef when !IsUnderEventAssignment(eventRef) => eventRef.Event,
                IEventAssignmentOperation eventAssign =>
                    eventAssign.EventReference is IEventReferenceOperation evtRef ? evtRef.Event : null,
                IMethodReferenceOperation methodRef => methodRef.Method,
                IInvocationOperation invocation => invocation.TargetMethod,
                IObjectCreationOperation objectCreation => (ISymbol?)objectCreation.Constructor ?? objectCreation.Type,
                _ => null
            };

        private static void ReportCrossNamespaceAccess(
            OperationAnalysisContext context,
            IOperation operation,
            ISymbol? symbol)
        {
            if (symbol == null || !ShouldRestrict(symbol))
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, context.Compilation.Assembly))
            {
                return;
            }

            var useNamespace = context.ContainingSymbol?.ContainingNamespace;
            if (useNamespace == null)
            {
                return;
            }

            var declarationNamespace = symbol.ContainingNamespace;
            if (declarationNamespace == null || IsSameNamespace(useNamespace, declarationNamespace))
            {
                return;
            }

            var location = operation.Syntax.GetLocation();
            if (location == null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_InternalNamespaceAccess,
                location,
                symbol.ToDiagnosticMessageName(),
                useNamespace.ToDiagnosticMessageName(),
                declarationNamespace.ToDiagnosticMessageName()));
        }

        private static ISymbol? TryGetNameOfSymbolFromOperation(INameOfOperation nameOf)
        {
            if (nameOf.Argument is IMemberReferenceOperation memberRef)
            {
                return memberRef.Member;
            }

            if (nameOf.Argument is ITypeOfOperation typeOf)
            {
                return typeOf.TypeOperand as INamedTypeSymbol;
            }

            if (nameOf.Argument?.Type is INamedTypeSymbol namedType)
            {
                return namedType;
            }

            return null;
        }

        private static ISymbol? TryGetNameOfSymbolFromSemanticModel(INameOfOperation nameOf)
        {
            var semanticModel = nameOf.SemanticModel;
            if (semanticModel == null)
            {
                return null;
            }

            var symbol = semanticModel.GetSymbolInfo(nameOf.Syntax).Symbol;
            if (symbol != null)
            {
                return symbol;
            }

            if (nameOf.Argument == null)
            {
                return null;
            }

            var typeInfo = semanticModel.GetTypeInfo(nameOf.Argument.Syntax);
            return typeInfo.Type as INamedTypeSymbol;
        }

        private static bool IsUnderEventAssignment(IOperation operation)
        {
            for (var parent = operation.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is IEventAssignmentOperation)
                {
                    return true;
                }
            }

            return false;
        }

        private static INamedTypeSymbol? GetPatternTypeSymbol(IPatternOperation pattern) =>
            pattern switch
            {
                ITypePatternOperation typePattern => typePattern.MatchedType as INamedTypeSymbol,
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType as INamedTypeSymbol,
                INegatedPatternOperation negated => GetPatternTypeSymbol(negated.Pattern),
                _ => null
            };

        private static bool ShouldRestrict(ISymbol symbol)
        {
            for (var current = symbol; current != null; current = current.ContainingType)
            {
                if (IsInternalOrProtectedInternal(current.DeclaredAccessibility))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInternalOrProtectedInternal(Accessibility accessibility) =>
            accessibility == Accessibility.Internal
            || accessibility == Accessibility.ProtectedOrInternal;

        private static bool IsSameNamespace(INamespaceSymbol left, INamespaceSymbol right) =>
            SymbolEqualityComparer.Default.Equals(left, right);
    }
}
