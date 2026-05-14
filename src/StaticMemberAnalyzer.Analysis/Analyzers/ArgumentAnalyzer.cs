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
            // RegisterOperationAction(OperationKind.Argument) does not trigger for attribute arguments.
            // Using SyntaxNodeAction ensures coverage for attributes.

            if (context.Node is not AttributeArgumentSyntax argStx)
                return;

            if (argStx.NameColon != null || argStx.NameEquals != null)
                return;

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx)
                return;

            // For attribute syntax, we do not check the argument type because it's too complicated.
            if (argListStx.Arguments.Count <= 1)
                return;

            var operation = context.SemanticModel.GetOperation(argStx.Expression);
            if (operation == null)
                return;

            if (operation is not ILiteralOperation value)
            {
                value = GetLiteralOperation(operation);
                if (value is not ILiteralOperation)
                    return;
            }

            // Getting semantic model should be done right before emitting diagnostic for performance.
            string parameterName = "unknown";
            var attrSymbol = context.SemanticModel.GetSymbolInfo(argListStx.Parent).Symbol as IMethodSymbol;
            if (attrSymbol != null)
            {
                int index = argListStx.Arguments.IndexOf(argStx);
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
            if (context.Operation is not IArgumentOperation argOp)
                return;

            if (argOp.Syntax is not ArgumentSyntax argStx)
                return;

            // Skip if it's part of an attribute, we handle that via SyntaxNodeAction.
            if (argStx.Kind() == SyntaxKind.AttributeArgument)
                return;

            if (argOp.IsImplicit)
                return;

            // If it has ref/in/out keyword, literal causes compile error so don't need to proceed.
            if (argStx.RefKindKeyword.Kind() != SyntaxKind.None)
                return;

            // Skip if it's an indexer argument.
            if (argOp.Parent is IPropertyReferenceOperation)
                return;

            if (argOp.Value is not ILiteralOperation literalOp)
            {
                literalOp = GetLiteralOperation(argOp.Value);
                if (literalOp is not ILiteralOperation)
                    return;
            }

            // Null and default literals are not allowed to be unnamed.
            bool isNullOrDefaultLiteral = literalOp.ConstantValue.HasValue && literalOp.ConstantValue.Value == null;

            if (!isNullOrDefaultLiteral)
            {
                var invocationOp = argOp.Parent as IInvocationOperation;

                // int, string or char is allowed if it's the first argument.
                if (argStx.Parent is ArgumentListSyntax argListStx)
                {
                    if (argListStx.Arguments.IndexOf(argStx) == 0 &&
                        argOp.Parameter?.Type is ITypeSymbol firstArgType)
                    {
                        // First string or char argument is allowed for both method and constructor.
                        //   ex. throw new Exception("Message", innerError);
                        if (firstArgType.SpecialType is SpecialType.System_String or SpecialType.System_Char)
                        {
                            return;
                        }
                        // but, don't allow omitting first int argument for constructor.
                        //   ex. list = new(0);  // Expect: new(capacity: 0);
                        else if (firstArgType.SpecialType is SpecialType.System_Int32 && invocationOp != null)
                        {
                            return;
                        }
                    }
                }

                // String and System.IO methods and constructors are intentionally allowed.
                var containingType = invocationOp?.TargetMethod.ContainingType
                    ?? (argOp.Parent as IObjectCreationOperation)?.Constructor?.ContainingType;

                if (containingType is not null)
                {
                    if (containingType.SpecialType == SpecialType.System_String)
                        return;

                    if (containingType.ContainingNamespace is INamespaceSymbol { Name: "IO", ContainingNamespace: INamespaceSymbol { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } } })
                        return;
                }
            }

            bool isNamed = argStx.NameColon != null;

            if (!isNamed)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_LiteralArgument,
                    argOp.Syntax.GetLocation(),
                    argOp.Parameter?.Name ?? "unknown"));
            }
        }

        private static ILiteralOperation? GetLiteralOperation(IOperation operation)
        {
            var value = operation;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }
            return value as ILiteralOperation;
        }
    }
}
