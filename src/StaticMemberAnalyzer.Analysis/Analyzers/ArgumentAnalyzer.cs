// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
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
            // Analysis is centralized to ILiteralOperation-basis.
            // However, RegisterOperationAction(OperationKind.Argument) does not trigger for attribute arguments in Roslyn 3.8.0.
            // Thus, we maintain SyntaxNodeAction here to ensure coverage for attributes.

            if (context.Node is not AttributeArgumentSyntax argStx)
                return;

            // Early return to avoid unnecessary semantic lookups.
            if (argStx.NameColon != null || argStx.NameEquals != null)
                return;

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx)
                return;

            if (argListStx.Arguments.Count <= 1)
                return;

            var value = context.SemanticModel.GetOperation(argStx.Expression);
            while (value is IConversionOperation conversion) value = conversion.Operand;

            if (value is ILiteralOperation literalOp)
            {
                AnalyzeLiteral(literalOp, argStx, context.ReportDiagnostic, context.SemanticModel);
            }
        }

        private static void AnalyzeArgument(OperationAnalysisContext context)
        {
            if (context.Operation is not IArgumentOperation argOp || argOp.Syntax is not ArgumentSyntax argStx)
                return;

            // Attribute arguments are handled separately.
            if (argStx.Kind() == SyntaxKind.AttributeArgument)
                return;

            var value = argOp.Value;
            while (value is IConversionOperation conversion) value = conversion.Operand;

            if (value is ILiteralOperation literalOp)
            {
                AnalyzeLiteral(literalOp, argStx, context.ReportDiagnostic);
            }
        }

        private static void AnalyzeLiteral(ILiteralOperation literalOp, SyntaxNode argSyntax, Action<Diagnostic> reportAction, SemanticModel? semanticModel = null)
        {
            if (argSyntax is AttributeArgumentSyntax attrArg)
            {
                if (attrArg.Parent is not AttributeArgumentListSyntax argList || semanticModel == null)
                    return;

                // Resolve parameter name
                string parameterName = "unknown";
                var attrSymbol = semanticModel.GetSymbolInfo(argList.Parent).Symbol as IMethodSymbol;
                int index = argList.Arguments.IndexOf(attrArg);
                if (attrSymbol != null && index >= 0 && index < attrSymbol.Parameters.Length)
                {
                    parameterName = attrSymbol.Parameters[index].Name;
                }

                reportAction(Diagnostic.Create(Rule_LiteralArgument, argSyntax.GetLocation(), parameterName));
            }
            else if (argSyntax is ArgumentSyntax regularArg)
            {
                var argOp = literalOp.Parent;
                while (argOp != null && argOp is not IArgumentOperation) argOp = argOp.Parent;
                if (argOp is not IArgumentOperation argument)
                    return;

                if (argument.IsImplicit)
                    return;

                if (regularArg.RefKindKeyword.Kind() != SyntaxKind.None)
                    return;

                if (argument.Parent is IPropertyReferenceOperation)
                    return;

                if (regularArg.NameColon != null)
                    return;

                // Exclusion rules for literals
                bool isNullOrDefaultLiteral = literalOp.ConstantValue.HasValue && literalOp.ConstantValue.Value == null;

                if (!isNullOrDefaultLiteral)
                {
                    var invocationOp = argument.Parent as IInvocationOperation;

                    if (regularArg.Parent is ArgumentListSyntax argList && argList.Arguments.IndexOf(regularArg) == 0 &&
                        argument.Parameter?.Type is ITypeSymbol firstArgType)
                    {
                        if (firstArgType.SpecialType is SpecialType.System_String or SpecialType.System_Char)
                        {
                            return;
                        }
                        else if (firstArgType.SpecialType is SpecialType.System_Int32 && invocationOp != null)
                        {
                            return;
                        }
                    }

                    var containingType = invocationOp?.TargetMethod.ContainingType
                        ?? (argument.Parent as IObjectCreationOperation)?.Constructor?.ContainingType;

                    if (containingType is not null)
                    {
                        if (containingType.SpecialType == SpecialType.System_String)
                            return;

                        if (containingType.ContainingNamespace is INamespaceSymbol { Name: "IO", ContainingNamespace: INamespaceSymbol { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } } })
                            return;
                    }
                }

                reportAction(Diagnostic.Create(Rule_LiteralArgument, argSyntax.GetLocation(), argument.Parameter?.Name ?? "unknown"));
            }
        }
    }
}
