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
        public const string RuleId_InternalNamespaceAccess = "SMA7003";

        private static readonly DiagnosticDescriptor Rule_InternalNamespaceAccess = new(
            RuleId_InternalNamespaceAccess,
            new LocalizableResourceString(nameof(Resources.SMA7003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7003_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule_InternalNamespaceAccess);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(
                AnalyzeOperation,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.EventReference,
                OperationKind.MethodReference,
                OperationKind.Invocation,
                OperationKind.ObjectCreation,
                OperationKind.TypeOf,
                OperationKind.Conversion);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            if (context.Operation is not IOperation operation)
            {
                return;
            }

            var accessedSymbols = GetAccessedSymbols(operation);
            if (accessedSymbols.IsDefaultOrEmpty)
            {
                return;
            }

            var useNamespace = context.ContainingSymbol?.ContainingNamespace;
            if (useNamespace == null)
            {
                return;
            }

            foreach (var symbol in accessedSymbols)
            {
                if (!ShouldRestrict(symbol))
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

                var location = operation.Syntax.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_InternalNamespaceAccess,
                    location,
                    symbol.Name,
                    GetNamespaceDisplay(useNamespace),
                    GetNamespaceDisplay(declarationNamespace)));
            }
        }

        private static ImmutableArray<ISymbol> GetAccessedSymbols(IOperation operation)
        {
            switch (operation)
            {
                case IFieldReferenceOperation fieldRef:
                    return ImmutableArray.Create<ISymbol>(fieldRef.Field);
                case IPropertyReferenceOperation propertyRef:
                    return ImmutableArray.Create<ISymbol>(propertyRef.Property);
                case IEventReferenceOperation eventRef:
                    return ImmutableArray.Create<ISymbol>(eventRef.Event);
                case IMethodReferenceOperation methodRef:
                    return ImmutableArray.Create<ISymbol>(methodRef.Method);
                case IInvocationOperation invocation:
                    return ImmutableArray.Create<ISymbol>(invocation.TargetMethod);
                case IObjectCreationOperation objectCreation:
                    if (objectCreation.Constructor is { } ctor)
                    {
                        return ImmutableArray.Create<ISymbol>(ctor);
                    }

                    if (objectCreation.Type is INamedTypeSymbol namedType)
                    {
                        return ImmutableArray.Create<ISymbol>(namedType);
                    }

                    return default;
                case ITypeOfOperation typeOf when typeOf.TypeOperand is INamedTypeSymbol typeOperand:
                    return ImmutableArray.Create<ISymbol>(typeOperand);
                case IConversionOperation conversion when conversion.Type is INamedTypeSymbol conversionType:
                    return ImmutableArray.Create<ISymbol>(conversionType);
                default:
                    return default;
            }
        }

        private static bool ShouldRestrict(ISymbol symbol)
        {
            if (IsInternalOrProtectedInternal(symbol.DeclaredAccessibility))
            {
                return true;
            }

            return symbol.ContainingType is { } containingType
                && IsInternalOrProtectedInternal(containingType.DeclaredAccessibility);
        }

        private static bool IsInternalOrProtectedInternal(Accessibility accessibility) =>
            accessibility == Accessibility.Internal
            || accessibility == Accessibility.ProtectedOrInternal;

        private static bool IsSameNamespace(INamespaceSymbol left, INamespaceSymbol right) =>
            SymbolEqualityComparer.Default.Equals(left, right);

        private static string GetNamespaceDisplay(INamespaceSymbol ns) =>
            ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }
}
