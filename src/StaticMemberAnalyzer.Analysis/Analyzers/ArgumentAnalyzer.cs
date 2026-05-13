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
            if (context.Operation is not IArgumentOperation op)
                return;

            if (op.Syntax is not ArgumentSyntax argStx)
                return;

            // Skip if it's part of an attribute, we handle that via SyntaxNodeAction because IArgumentOperation might not be reported for attributes in this Roslyn version.
            if (argStx.Kind() == SyntaxKind.AttributeArgument)
                return;

            if (op.IsImplicit)
                return;

            // If it has ref/in/out keyword, literal causes compile error so don't need to proceed.
            if (argStx.RefKindKeyword.Kind() != SyntaxKind.None)
                return;

            // Skip if it's an indexer argument.
            if (op.Parent is IPropertyReferenceOperation)
                return;

            var invocationOp = op.Parent as IInvocationOperation;

            // int, string or char is allowed if it's the first argument.
            if (argStx.Parent is ArgumentListSyntax argListStx)
            {
                if (argListStx.Arguments.IndexOf(argStx) == 0 &&
                    op.Parameter?.Type is ITypeSymbol firstArgType)
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
                ?? (op.Parent as IObjectCreationOperation)?.Constructor?.ContainingType;

            if (containingType is not null)
            {
                if (containingType.SpecialType == SpecialType.System_String)
                    return;

                if (containingType.ContainingNamespace is INamespaceSymbol { Name: "IO", ContainingNamespace: INamespaceSymbol { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } } })
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
