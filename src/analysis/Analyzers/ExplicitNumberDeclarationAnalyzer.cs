// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExplicitNumberDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_ExplicitNumber = "SMA8001";

        private static readonly DiagnosticDescriptor Rule_ExplicitNumber = new(
            RuleId_ExplicitNumber,
            new LocalizableResourceString(nameof(Resources.SMA8001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8001_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_ExplicitNumber);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeDeclarationExpression, SyntaxKind.DeclarationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
        }

        private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not VariableDeclarationSyntax declaration)
            {
                return;
            }

            // Only target 'var' declaration.
            if (!declaration.Type.IsVar)
            {
                return;
            }

            foreach (var variable in declaration.Variables)
            {
                ReportIfPrimitiveNumber(context, variable.Identifier, context.SemanticModel.GetDeclaredSymbol(variable));
            }
        }

        private static void AnalyzeDeclarationExpression(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not DeclarationExpressionSyntax declaration)
            {
                return;
            }

            // Only target 'var' declaration.
            if (declaration.Type is not IdentifierNameSyntax id || !id.IsVar)
            {
                return;
            }

            if (declaration.Designation is SingleVariableDesignationSyntax single)
            {
                ReportIfPrimitiveNumber(context, single.Identifier, context.SemanticModel.GetDeclaredSymbol(single));
            }
            else if (declaration.Designation is ParenthesizedVariableDesignationSyntax tuple)
            {
                ReportRecursive(context, tuple);
            }
        }

        private static void ReportRecursive(SyntaxNodeAnalysisContext context, ParenthesizedVariableDesignationSyntax tuple)
        {
            foreach (var item in tuple.Variables)
            {
                if (item is SingleVariableDesignationSyntax single)
                {
                    ReportIfPrimitiveNumber(context, single.Identifier, context.SemanticModel.GetDeclaredSymbol(single));
                }
                else if (item is ParenthesizedVariableDesignationSyntax nested)
                {
                    ReportRecursive(context, nested);
                }
            }
        }

        private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ForEachStatementSyntax forEach)
            {
                return;
            }

            // Only target 'var' declaration.
            if (!forEach.Type.IsVar)
            {
                return;
            }

            ReportIfPrimitiveNumber(context, forEach.Identifier, context.SemanticModel.GetDeclaredSymbol(forEach));
        }

        private static void ReportIfPrimitiveNumber(SyntaxNodeAnalysisContext context, SyntaxToken identifier, ISymbol? symbol)
        {
            if (identifier.Text == "_")
            {
                return;
            }

            if (symbol is ILocalSymbol local && IsSystemPrimitiveNumber(local.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ExplicitNumber,
                    identifier.GetLocation(),
                    identifier.Text));
            }
        }

        private static bool IsSystemPrimitiveNumber(ITypeSymbol type)
        {
            if (type == null) return false;

            return type.SpecialType switch
            {
                SpecialType.System_SByte or
                SpecialType.System_Byte or
                SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal or
                SpecialType.System_IntPtr or
                SpecialType.System_UIntPtr => true,
                _ => false,
            };
        }
    }
}
