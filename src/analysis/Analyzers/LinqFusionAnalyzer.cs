// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LinqFusionAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_LinqFusion = "SMA7051";

        private static readonly DiagnosticDescriptor Rule_LinqFusion = new(
            RuleId_LinqFusion,
            new LocalizableResourceString("SMA7051_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA7051_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(LinqFusionAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("SMA7051_MessageFormat", Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule_LinqFusion);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                AnalyzeInvocation,
                SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax outerInvocation
                || outerInvocation.Expression is not MemberAccessExpressionSyntax outerAccess
                || outerAccess.Expression is not InvocationExpressionSyntax innerInvocation)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(innerInvocation, context.CancellationToken).Symbol is not IMethodSymbol innerMethod
                || context.SemanticModel.GetSymbolInfo(outerInvocation, context.CancellationToken).Symbol is not IMethodSymbol outerMethod)
            {
                return;
            }

            innerMethod = (innerMethod.ReducedFrom ?? innerMethod).ConstructedFrom;
            outerMethod = (outerMethod.ReducedFrom ?? outerMethod).ConstructedFrom;

            if (!IsFusionMethod(innerMethod.Name, outerMethod.Name))
            {
                return;
            }

            if (!IsEnumerableOrLinqPortal(innerMethod.ContainingType)
                || !IsEnumerableOrLinqPortal(outerMethod.ContainingType))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LinqFusion,
                outerInvocation.GetLocation()));
        }

        private static bool IsFusionMethod(string innerName, string outerName)
        {
            if (innerName is "Where" or "OfType" or "Select")
            {
                return outerName is "Any" or "First" or "FirstOrDefault" or "Last"
                    or "LastOrDefault" or "Single" or "SingleOrDefault" or "All";
            }
            return false;
        }

        private static bool IsEnumerableOrLinqPortal(INamedTypeSymbol type)
        {
            // Identify supported targets by containing type name, as aliases and namespaces are not part of the fusion contract.
            return type.Name is "Enumerable" or "LinqPortal";
        }
    }
}
