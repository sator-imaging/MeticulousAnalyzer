// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CapacityArgumentAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_CapacityArgument = "SMA8005";

        private static readonly DiagnosticDescriptor Rule_CapacityArgument = new(
            RuleId_CapacityArgument,
            new LocalizableResourceString("SMA8005_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA8005_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(CapacityArgumentAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("SMA8005_MessageFormat", Resources.ResourceManager, typeof(Resources), "capacity"));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_CapacityArgument);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
            context.RegisterSyntaxNodeAction(AnalyzeAttributeArgument, SyntaxKind.AttributeArgument);
        }

        private static void AnalyzeArgument(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation argOp ||
                argOp.IsImplicit ||
                argOp.Syntax is not ArgumentSyntax argStx ||
                argStx.NameColon != null)
            {
                return;
            }

            var parameter = argOp.Parameter;
            if (parameter == null || !IsCapacityParameter(parameter))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_CapacityArgument,
                argStx.GetLocation(),
                parameter.ToDiagnosticMessageName()));
        }

        private static void AnalyzeAttributeArgument(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AttributeArgumentSyntax argStx ||
                argStx.NameColon != null ||
                argStx.NameEquals != null ||
                argStx.Parent is not AttributeArgumentListSyntax argListStx ||
                argListStx.Parent == null ||
                context.SemanticModel.GetSymbolInfo(argListStx.Parent).Symbol is not IMethodSymbol constructor)
            {
                return;
            }

            int argIndex = argListStx.Arguments.IndexOf(argStx);
            if (unchecked((uint)argIndex >= (uint)constructor.Parameters.Length))
            {
                return;
            }

            var parameter = constructor.Parameters[argIndex];
            if (!IsCapacityParameter(parameter))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_CapacityArgument,
                argStx.GetLocation(),
                parameter.ToDiagnosticMessageName()));
        }

        private static bool IsCapacityParameter(IParameterSymbol? parameter)
            => parameter != null &&
                parameter.Name == "capacity" &&
                parameter.Type.SpecialType == SpecialType.System_Int32;
    }
}
