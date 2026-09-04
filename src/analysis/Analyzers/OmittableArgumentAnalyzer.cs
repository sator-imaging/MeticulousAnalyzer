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
    public sealed class OmittableArgumentAnalyzer : DiagnosticAnalyzer
    {
        private const string UnknownParameterName = "<unknown>";
        public const string RuleId_OmittableArgument = "SMA8004";

        private static readonly DiagnosticDescriptor Rule_OmittableArgument = new(
            RuleId_OmittableArgument,
            new LocalizableResourceString(nameof(Resources.SMA8004_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8004_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(OmittableArgumentAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8004_MessageFormat), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_OmittableArgument);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
            context.RegisterSyntaxNodeAction(AnalyzeAttributeArgument, SyntaxKind.AttributeArgument);
        }

        private static void AnalyzeAttributeArgument(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AttributeArgumentSyntax argStx)
            {
                return;
            }

            if (argStx.NameColon != null || argStx.NameEquals != null)
            {
                return;
            }

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx)
            {
                return;
            }

            int argIndex = argListStx.Arguments.IndexOf(argStx);

            if (argListStx.Parent != null &&
                context.SemanticModel.GetSymbolInfo(argListStx.Parent).Symbol is IMethodSymbol attrSymbol)
            {
                if (IsInSystemNamespace(attrSymbol.ContainingType))
                {
                    return;
                }

                if (unchecked((uint)argIndex < (uint)attrSymbol.Parameters.Length))
                {
                    var paramSymbol = attrSymbol.Parameters[argIndex];
                    if (paramSymbol.IsOptional || paramSymbol.HasExplicitDefaultValue)
                    {
                        if (paramSymbol.Type.Name == "CancellationToken" &&
                            argIndex == argListStx.Arguments.Count - 1)
                        {
                            return;
                        }

                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_OmittableArgument,
                            argStx.GetLocation(),
                            paramSymbol.ToDiagnosticMessageName()));
                    }
                }
            }
        }

        private static void AnalyzeArgument(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation argOp ||
                argOp.IsImplicit)
            {
                return;
            }

            if (argOp.Syntax is not ArgumentSyntax argStx ||
                argStx.NameColon != null)
            {
                return;
            }

            // Skip if it's part of an attribute, we handle that via SyntaxNodeAction.
            if (argStx.IsKind(SyntaxKind.AttributeArgument))
            {
                return;
            }

            var parameter = argOp.Parameter;
            if (parameter == null)
            {
                return;
            }

            var containingType = parameter.ContainingType
                               ?? (argOp.Parent as IInvocationOperation)?.TargetMethod.ContainingType
                               ?? (argOp.Parent as IObjectCreationOperation)?.Constructor.ContainingType;

            if (IsInSystemNamespace(containingType))
            {
                return;
            }

            if (parameter.IsOptional || parameter.HasExplicitDefaultValue)
            {
                if (parameter.Type.Name == "CancellationToken")
                {
                    if (argStx.Parent is ArgumentListSyntax argListStx &&
                        argListStx.Arguments.Count > 0 &&
                        argListStx.Arguments[argListStx.Arguments.Count - 1] == argStx)
                    {
                        return;
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_OmittableArgument,
                    argStx.GetLocation(),
                    parameter.ToDiagnosticMessageName()));
            }
        }

        private static bool IsInSystemNamespace(INamedTypeSymbol? typeSymbol)
        {
            for (var ns = typeSymbol?.ContainingNamespace; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
            {
                if (ns is INamespaceSymbol { Name: "System", ContainingNamespace: INamespaceSymbol { IsGlobalNamespace: true } })
                {
                    return true;
                }
            }
            return false;
        }
    }
}
