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
    public sealed class ExplicitNumberDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_ExplicitNumber = "SMA8001";

        private static readonly DiagnosticDescriptor Rule_ExplicitNumber = new(
            RuleId_ExplicitNumber,
            new LocalizableResourceString(nameof(Resources.SMA8001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(ExplicitNumberDeclarationAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8001_MessageFormat), Resources.ResourceManager, typeof(Resources), "$variable", "$type"));

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
            if (context.Node is not VariableDeclarationSyntax declaration || !declaration.Type.IsVar)
            {
                return;
            }

            foreach (var variable in declaration.Variables)
            {
                if (context.SemanticModel.GetDeclaredSymbol(variable) is ILocalSymbol local)
                {
                    ReportIfPrimitiveNumber(context, declaration.Type, variable.Identifier, local.Type);
                }
            }
        }

        private static void AnalyzeDeclarationExpression(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not DeclarationExpressionSyntax declaration || declaration.Type is not IdentifierNameSyntax id || !id.IsVar)
            {
                return;
            }

            AnalyzeVariableDesignation(context, declaration.Type, declaration.Designation);
        }

        private static void AnalyzeVariableDesignation(SyntaxNodeAnalysisContext context, TypeSyntax varType, VariableDesignationSyntax designation)
        {
            if (designation is SingleVariableDesignationSyntax single)
            {
                if (context.SemanticModel.GetDeclaredSymbol(single) is ILocalSymbol local)
                {
                    ReportIfPrimitiveNumber(context, varType, single.Identifier, local.Type);
                }
            }
            else if (designation is DiscardDesignationSyntax discard)
            {
                ITypeSymbol? type = context.SemanticModel.GetSymbolInfo(discard).Symbol switch
                {
                    IDiscardSymbol discardSymbol => discardSymbol.Type,
                    _ => (context.SemanticModel.GetOperation(discard) as IDiscardOperation)?.Type
                };

                if (type == null && discard.Parent is DeclarationExpressionSyntax decl)
                {
                    type = context.SemanticModel.GetTypeInfo(decl).ConvertedType;
                }

                ReportIfPrimitiveNumber(context, varType, discard.UnderscoreToken, type);
            }
            else if (designation is ParenthesizedVariableDesignationSyntax tuple)
            {
                foreach (var variable in tuple.Variables)
                {
                    AnalyzeVariableDesignation(context, varType, variable);
                }
            }
        }

        private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ForEachStatementSyntax forEach || !forEach.Type.IsVar)
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(forEach) is ILocalSymbol local)
            {
                ReportIfPrimitiveNumber(context, forEach.Type, forEach.Identifier, local.Type);
            }
        }


        private static void ReportIfPrimitiveNumber(SyntaxNodeAnalysisContext context, TypeSyntax typeNode, SyntaxToken identifier, ITypeSymbol? type)
        {
            if (type != null && IsSystemPrimitiveNumber(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_ExplicitNumber,
                    typeNode.GetLocation(),
                    identifier.Text,
                    type.ToDiagnosticMessageName()));
            }
        }

        private static bool IsSystemPrimitiveNumber(ITypeSymbol type)
        {
            if (type == null) return false;

            return type.SpecialType switch
            {
                SpecialType.System_SByte => true,
                SpecialType.System_Byte => true,
                SpecialType.System_Int16 => true,
                SpecialType.System_UInt16 => true,
                SpecialType.System_Int32 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_Int64 => true,
                SpecialType.System_UInt64 => true,
                SpecialType.System_Single => true,
                SpecialType.System_Double => true,
                SpecialType.System_Decimal => true,
                SpecialType.System_IntPtr => true,
                SpecialType.System_UIntPtr => true,
                _ => false,
            };
        }
    }
}
