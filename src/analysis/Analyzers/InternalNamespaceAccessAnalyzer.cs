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
            ISymbol? symbol = operation switch
            {
                IFieldReferenceOperation fieldRef => fieldRef.Field,
                IPropertyReferenceOperation propertyRef => propertyRef.Property,
                IEventReferenceOperation eventRef => eventRef.Event,
                IMethodReferenceOperation methodRef => methodRef.Method,
                IInvocationOperation invocation => invocation.TargetMethod,
                IObjectCreationOperation objectCreation => (ISymbol?)objectCreation.Constructor ?? objectCreation.Type,
                ITypeOfOperation typeOf => typeOf.TypeOperand as INamedTypeSymbol,
                IConversionOperation conversion => conversion.Type as INamedTypeSymbol,
                _ => null
            };

            return symbol != null ? ImmutableArray.Create(symbol) : default;
        }

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

        private static string GetNamespaceDisplay(INamespaceSymbol ns) =>
            ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }
}
