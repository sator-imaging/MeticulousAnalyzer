// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DebugAssertAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_DebugAssertInPublicApi = "SMA8003";

        private static readonly DiagnosticDescriptor Rule_DebugAssertInPublicApi = new(
            RuleId_DebugAssertInPublicApi,
            new LocalizableResourceString(nameof(Resources.SMA8003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8003_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_DebugAssertInPublicApi);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
                return;

            if (invocation.TargetMethod.Name != "Assert")
                return;

            var containingMember = context.ContainingSymbol;
            if (containingMember == null)
                return;

            if (IsPubliclyAccessible(containingMember))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_DebugAssertInPublicApi,
                    invocation.Syntax.GetLocation()));
            }
        }

        private static bool IsPubliclyAccessible(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => true,
                Accessibility.Protected => true,
                Accessibility.ProtectedOrInternal => true,
                _ => false,
            };
        }
    }
}
