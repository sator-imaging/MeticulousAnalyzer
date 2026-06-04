// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var reportedSpans = new HashSet<TextSpan>();

                compilationContext.RegisterOperationAction(
                    operationContext => AnalyzeOperation(operationContext, reportedSpans),
                    OperationKind.FieldReference,
                    OperationKind.PropertyReference,
                    OperationKind.EventReference,
                    OperationKind.EventAssignment,
                    OperationKind.MethodReference,
                    OperationKind.Invocation,
                    OperationKind.ObjectCreation,
                    OperationKind.TypeParameterObjectCreation,
                    OperationKind.ArrayCreation,
                    OperationKind.TypeOf,
                    OperationKind.NameOf,
                    OperationKind.IsType,
                    OperationKind.IsPattern,
                    OperationKind.DefaultValue,
                    OperationKind.Conversion);
            });
        }

        private static void AnalyzeOperation(OperationAnalysisContext context, HashSet<TextSpan> reportedSpans)
        {
            if (context.Operation is not IOperation operation)
            {
                return;
            }

            if (IsUnderEventAssignment(operation))
            {
                return;
            }

            var accessedSymbols = GetAccessedSymbols(operation, context);
            if (accessedSymbols.IsDefaultOrEmpty)
            {
                return;
            }

            var useNamespace = context.ContainingSymbol?.ContainingNamespace;
            if (useNamespace == null)
            {
                return;
            }

            var syntaxTree = operation.Syntax.SyntaxTree;
            if (syntaxTree == null)
            {
                return;
            }

            foreach (var symbol in accessedSymbols)
            {
                if (symbol == null || !ShouldRestrict(symbol))
                {
                    continue;
                }

                if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, context.Compilation.Assembly))
                {
                    continue;
                }

                var declarationNamespace = symbol.ContainingNamespace;
                if (declarationNamespace == null || IsSameNamespace(useNamespace, declarationNamespace))
                {
                    continue;
                }

                var span = operation.Syntax.Span;
                if (!reportedSpans.Add(span))
                {
                    continue;
                }

                var location = Location.Create(syntaxTree, span);
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_InternalNamespaceAccess,
                    location,
                    symbol.ToDiagnosticMessageName(),
                    useNamespace.ToDiagnosticMessageName(),
                    declarationNamespace.ToDiagnosticMessageName()));
            }
        }

        private static ImmutableArray<ISymbol> GetAccessedSymbols(IOperation operation, OperationAnalysisContext context)
        {
            ISymbol? symbol = operation switch
            {
                IFieldReferenceOperation fieldRef => fieldRef.Field,
                IPropertyReferenceOperation propertyRef => propertyRef.Property,
                IEventReferenceOperation eventRef => eventRef.Event,
                IEventAssignmentOperation eventAssign => eventAssign.EventReference is IEventReferenceOperation evtRef ? evtRef.Event : null,
                IMethodReferenceOperation methodRef => methodRef.Method,
                IInvocationOperation invocation => invocation.TargetMethod,
                IObjectCreationOperation objectCreation => (ISymbol?)objectCreation.Constructor ?? objectCreation.Type,
                ITypeParameterObjectCreationOperation typeParamCreation => GetTypeParameterCreationSymbol(typeParamCreation),
                IArrayCreationOperation arrayCreation => GetArrayElementNamedType(arrayCreation),
                ITypeOfOperation typeOf => typeOf.TypeOperand as INamedTypeSymbol,
                INameOfOperation nameOf => GetNameOfSymbol(nameOf, context),
                IIsTypeOperation isType => isType.TypeOperand as INamedTypeSymbol,
                IIsPatternOperation isPattern => GetPatternTypeSymbol(isPattern.Pattern),
                IDefaultValueOperation defaultValue => defaultValue.Type,
                IConversionOperation conversion when ShouldAnalyzeConversion(conversion) =>
                    conversion.Type as INamedTypeSymbol,
                _ => null
            };

            return symbol != null ? ImmutableArray.Create(symbol) : default;
        }

        private static ISymbol? GetTypeParameterCreationSymbol(ITypeParameterObjectCreationOperation typeParamCreation)
        {
            if (typeParamCreation.Type is INamedTypeSymbol namedType && namedType.TypeKind != TypeKind.TypeParameter)
            {
                return namedType;
            }

            return null;
        }

        private static INamedTypeSymbol? GetArrayElementNamedType(IArrayCreationOperation arrayCreation)
        {
            if (arrayCreation.Type is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType as INamedTypeSymbol;
            }

            return arrayCreation.Type as INamedTypeSymbol;
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

        private static bool ShouldAnalyzeConversion(IConversionOperation conversion)
        {
            if (conversion.Operand is IObjectCreationOperation or ITypeParameterObjectCreationOperation)
            {
                return false;
            }

            return conversion.Type is INamedTypeSymbol;
        }

        private static ISymbol? GetNameOfSymbol(INameOfOperation nameOf, OperationAnalysisContext context)
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

            var semanticModel = nameOf.SemanticModel;
            if (semanticModel != null)
            {
                var symbol = semanticModel.GetSymbolInfo(nameOf.Syntax).Symbol;
                if (symbol != null)
                {
                    return symbol;
                }

                if (nameOf.Argument != null)
                {
                    var typeInfo = semanticModel.GetTypeInfo(nameOf.Argument.Syntax);
                    if (typeInfo.Type is INamedTypeSymbol type)
                    {
                        return type;
                    }
                }
            }

            return null;
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
