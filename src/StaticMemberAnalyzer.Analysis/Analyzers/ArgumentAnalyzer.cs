// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Linq;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_LiteralArgument = "SMA8000";

        private static readonly DiagnosticDescriptor Rule_LiteralArgument = new(
            RuleId_LiteralArgument,
            new LocalizableResourceString(nameof(Resources.SMA8000_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8000_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8000_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_LiteralArgument);

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
                return;

            if (argStx.NameColon != null || argStx.NameEquals != null)
                return;

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx || argListStx.Parent is not AttributeSyntax attrStx)
                return;

            // For attribute syntax, we do not check the argument type because it's too complicated.
            if (argListStx.Arguments.Count <= 1)
                return;

            var operation = context.SemanticModel.GetOperation(argStx.Expression);
            if (operation == null)
                return;

            var value = operation;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }

            if (value is not ILiteralOperation)
                return;

            // Getting semantic model should be done right before emitting diagnostic for performance.
            var attrSymbol = context.SemanticModel.GetSymbolInfo(attrStx).Symbol as IMethodSymbol;
            if (attrSymbol == null)
                return;

            int index = argListStx.Arguments.IndexOf(argStx);
            if (index < 0 || index >= attrSymbol.Parameters.Length)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argStx.GetLocation(),
                attrSymbol.Parameters[index].Name));
        }

        private static void AnalyzeArgument(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation op)
                return;

            if (op.Syntax is not ArgumentSyntax argStx)
                return;

            // Skip if it's part of an attribute, we handle that via SyntaxNodeAction because IArgumentOperation might not be reported for attributes in this Roslyn version.
            if (argStx.Kind() == SyntaxKind.AttributeArgument)
                return;

            if (op.IsImplicit)
                return;

            // Skip if it's an indexer argument.
            if (op.Parent is IPropertyReferenceOperation propRef && propRef.Arguments.Contains(op))
                return;

            // String and System.IO methods are intentionally allowed.
            if (op.Parent is IInvocationOperation inv)
            {
                var type = inv.TargetMethod.ContainingType;
                if (type?.SpecialType == SpecialType.System_String)
                    return;

                if (type?.ContainingNamespace is INamespaceSymbol { Name: "IO", ContainingNamespace: INamespaceSymbol { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } } })
                    return;
            }

            if (argStx.Parent is ArgumentListSyntax argListStx && argListStx.Arguments.Count == 1)
            {
                if (op.Parameter?.Type.SpecialType is SpecialType.System_String or SpecialType.System_Char)
                    return;
            }

            var value = op.Value;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }

            if (value is not ILiteralOperation)
                return;

            bool isNamed = argStx.NameColon != null;

            if (!isNamed)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralArgument,
                    op.Syntax.GetLocation(),
                    op.Parameter?.Name ?? "unknown"));
            }
        }
    }
}
