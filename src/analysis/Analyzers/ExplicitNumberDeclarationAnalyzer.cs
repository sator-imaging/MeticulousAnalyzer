// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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
            if (context.Node is not VariableDeclarationSyntax declaration || !declaration.Type.IsVar)
            {
                return;
            }

            ITypeSymbol? type = null;
            foreach (var variable in declaration.Variables)
            {
                if (variable.Identifier.Text == "_")
                {
                    if (context.SemanticModel.GetDeclaredSymbol(variable) is ILocalSymbol local)
                    {
                        ReportIfPrimitiveNumber(context, variable.Identifier, local.Type);
                    }
                    continue;
                }

                type ??= context.SemanticModel.GetTypeInfo(declaration.Type).Type;
                ReportIfPrimitiveNumber(context, variable.Identifier, type);
            }
        }

        private static void AnalyzeDeclarationExpression(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not DeclarationExpressionSyntax declaration || declaration.Type is not IdentifierNameSyntax id || !id.IsVar)
            {
                return;
            }

            AnalyzeVariableDesignation(context, declaration.Designation, context.SemanticModel.GetTypeInfo(declaration.Type).Type ?? context.SemanticModel.GetTypeInfo(declaration).Type);
        }

        private static void AnalyzeVariableDesignation(SyntaxNodeAnalysisContext context, VariableDesignationSyntax designation, ITypeSymbol? inferredType)
        {
            if (designation is SingleVariableDesignationSyntax single)
            {
                if (single.Identifier.Text == "_")
                {
                    if (context.SemanticModel.GetDeclaredSymbol(single) is ILocalSymbol local)
                    {
                        ReportIfPrimitiveNumber(context, single.Identifier, local.Type);
                    }
                    return;
                }

                ReportIfPrimitiveNumber(context, single.Identifier, inferredType);
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

                ReportIfPrimitiveNumber(context, discard.UnderscoreToken, type);
            }
            else if (designation is ParenthesizedVariableDesignationSyntax tuple)
            {
                var elements = (inferredType as INamedTypeSymbol)?.TupleElements;
                for (var i = 0; i < tuple.Variables.Count; i++)
                {
                    AnalyzeVariableDesignation(context, tuple.Variables[i], elements?.Length > i ? elements.Value[i].Type : null);
                }
            }
        }

        private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ForEachStatementSyntax forEach || !forEach.Type.IsVar)
            {
                return;
            }

            if (forEach.Identifier.Text == "_")
            {
                if (context.SemanticModel.GetDeclaredSymbol(forEach) is ILocalSymbol local)
                {
                    ReportIfPrimitiveNumber(context, forEach.Identifier, local.Type);
                }
                return;
            }

            ReportIfPrimitiveNumber(context, forEach.Identifier, context.SemanticModel.GetTypeInfo(forEach.Type).Type);
        }


        private static void ReportIfPrimitiveNumber(SyntaxNodeAnalysisContext context, SyntaxToken identifier, ITypeSymbol? type)
        {
            if (type != null && IsSystemPrimitiveNumber(type))
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
