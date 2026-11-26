// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

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
    public sealed class RequiredNamedArgumentAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_RequiredNamedArgument = "SMA8005";

        private static readonly DiagnosticDescriptor Rule_RequiredNamedArgument = new(
            RuleId_RequiredNamedArgument,
            new LocalizableResourceString("SMA8005_Title", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString("SMA8005_MessageFormat", Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(RequiredNamedArgumentAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("SMA8005_MessageFormat", Resources.ResourceManager, typeof(Resources), "capacity"));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_RequiredNamedArgument);

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
            if (parameter == null || !IsRequiredNamedArgument(parameter))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_RequiredNamedArgument,
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
            if (!IsRequiredNamedArgument(parameter))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_RequiredNamedArgument,
                argStx.GetLocation(),
                parameter.ToDiagnosticMessageName()));
        }

        private static bool IsRequiredNamedArgument(IParameterSymbol? parameter)
            => parameter != null &&
                parameter.Name.EndsWith("capacity", StringComparison.OrdinalIgnoreCase) &&
                parameter.Type.SpecialType == SpecialType.System_Int32;
    }
}
