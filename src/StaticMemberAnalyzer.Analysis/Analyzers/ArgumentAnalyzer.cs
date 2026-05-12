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

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx)
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

            int index = argListStx.Arguments.IndexOf(argStx);
            if (index == 0)
            {
                var type = context.SemanticModel.GetTypeInfo(argStx.Expression).Type;
                if (type?.SpecialType is SpecialType.System_String or SpecialType.System_Char)
                    return;
            }

            // Getting semantic model should be done right before emitting diagnostic for performance.
            string parameterName = "unknown";
            if (argListStx.Parent == null) return;
            var attrSymbol = context.SemanticModel.GetSymbolInfo(argListStx.Parent).Symbol as IMethodSymbol;
            if (attrSymbol != null)
            {
                var typeSymbol = attrSymbol.ContainingType;
                if (typeSymbol != null)
                {
                    if (typeSymbol.SpecialType == SpecialType.System_String || IsException(typeSymbol) || IsSystemIONamespace(typeSymbol.ContainingNamespace))
                        return;
                }

                if (index >= 0 && index < attrSymbol.Parameters.Length)
                {
                    parameterName = attrSymbol.Parameters[index].Name;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argStx.GetLocation(),
                parameterName));
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

            // String, Exception and System.IO methods/constructors are intentionally allowed.
            ISymbol? containingType = null;
            if (op.Parent is IInvocationOperation inv) containingType = inv.TargetMethod.ContainingType;
            else if (op.Parent is IObjectCreationOperation create) containingType = create.Type;

            if (containingType is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.SpecialType == SpecialType.System_String)
                    return;

                if (IsException(typeSymbol))
                    return;

                if (IsSystemIONamespace(typeSymbol.ContainingNamespace))
                    return;
            }

            if (argStx.Parent is ArgumentListSyntax argListStx)
            {
                int index = argListStx.Arguments.IndexOf(argStx);
                if (index == 0 && op.Parameter?.Type.SpecialType is SpecialType.System_String or SpecialType.System_Char)
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

        private static bool IsException(ITypeSymbol? type)
        {
            while (type != null)
            {
                if (type.Name == "Exception" && type.ContainingNamespace?.Name == "System" && type.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true)
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        private static bool IsSystemIONamespace(INamespaceSymbol? ns)
        {
            return ns is { Name: "IO", ContainingNamespace: { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } } };
        }
    }
}
