// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        private const string UnknownParameterName = "<unknown>";
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
            // NOTE: RegisterOperationAction(OperationKind.Argument) does not trigger for attribute arguments.
            //       Using SyntaxNodeAction ensures coverage for attributes.

            if (context.Node is not AttributeArgumentSyntax argStx)
            {
                return;
            }

            if (argStx.NameColon != null || argStx.NameEquals != null)
            {
                return;
            }

            var operation = context.SemanticModel.GetOperation(argStx.Expression);
            if (operation != null &&
                !IsPossibleOperation(operation))
            {
                return;
            }

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx)
            {
                return;
            }

            // string or char is allowed if it's the first argument.
            var argIndex = argListStx.Arguments.IndexOf(argStx);
            var isFirstArgument = argIndex == 0;
            if (operation != null &&
                !IsNullOrDefaultLiteral(operation))
            {
                if (IsOmittableType(operation, isConstructor: true, isFirstArgument))
                {
                    return;
                }
            }

            string parameterName = UnknownParameterName;

            // Getting semantic model should be done right before emitting diagnostic for performance.
            if (argListStx.Parent != null &&
                context.SemanticModel.GetSymbolInfo(argListStx.Parent).Symbol is IMethodSymbol attrSymbol)
            {
                if (unchecked((uint)argIndex < (uint)attrSymbol.Parameters.Length))
                {
                    parameterName = attrSymbol.Parameters[argIndex].Name;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argStx.GetLocation(),
                parameterName));
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

            // Skip if it's an indexer argument.
            if (argOp.Parent is IPropertyReferenceOperation)
            {
                return;
            }

            // If it has ref/in/out keyword, literal causes compile error so don't need to proceed.
            if (!argStx.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                return;
            }

            var argValue = argOp.Value;

            if (!IsPossibleOperation(argValue))
            {
                return;
            }

            // int, string or char is allowed if it's the first argument.
            // But 'null', 'default', or 'default(T)' is not allowed at all.
            if (!IsNullOrDefaultLiteral(argValue))
            {
                var invocationOp = argOp.Parent as IInvocationOperation;

                var isFirstArgument = argStx.Parent is ArgumentListSyntax argListStx &&
                    argListStx.Arguments.IndexOf(argStx) == 0;

                if (IsOmittableType(argValue, isConstructor: invocationOp == null, isFirstArgument))
                {
                    return;
                }

                // String, System.Text and System.IO methods and constructors are intentionally allowed.
                var containingType = invocationOp?.TargetMethod.ContainingType
                    ?? (argOp.Parent as IObjectCreationOperation)?.Constructor.ContainingType;

                if (containingType is not null)
                {
                    if (containingType.SpecialType == SpecialType.System_String)
                    {
                        return;
                    }

                    if (containingType.ContainingNamespace is INamespaceSymbol
                        {
                            Name: "Text" or "IO", ContainingNamespace: INamespaceSymbol
                            {
                                Name: "System", ContainingNamespace: INamespaceSymbol
                                {
                                    IsGlobalNamespace: true
                                }
                            }
                        })
                    {
                        return;
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argOp.Syntax.GetLocation(),
                argOp.Parameter?.Name ?? UnknownParameterName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPossibleOperation(IOperation operation)
        {
            // Compile-time constant (number, enum, etc)
            return operation.Kind is OperationKind.Literal
                                  or OperationKind.ConstantPattern
                                  // NOTE: 'default' is wrapped with Conversion, but 'default(T)' is not.
                                  or OperationKind.Conversion
                                  or OperationKind.DefaultValue
                                  or OperationKind.Binary
                                  or OperationKind.Unary
                                  ;
        }

        private static bool TryUnwrapConversion(IOperation operation, out IOperation unwrapped)
        {
            var value = operation;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }

            // [NotNullWhen] cannot be used on Roslyn Analyzer
            return (unwrapped = value) != null;
        }

        private static bool IsNullOrDefaultLiteral(IOperation operation)
        {
            if (operation is IConversionOperation &&
                TryUnwrapConversion(operation, out var unwrapped))
            {
                operation = unwrapped;
            }

            // 'null' and 'default' literals (including default(T)) are not allowed to be unnamed.
            return operation.Kind is OperationKind.DefaultValue
                || operation.ConstantValue is { HasValue: true, Value: null }
                ;
        }

        private static bool IsOmittableType(IOperation operation, bool isConstructor, bool isFirstArgument)
        {
            if (operation.Kind is OperationKind.Binary or OperationKind.Unary)
            {
                if (operation.Type?.SpecialType == SpecialType.System_Boolean)
                {
                    return false;
                }

                return true;
            }

            if (!isFirstArgument)
            {
                return false;
            }

            var literalSpecialType = operation.Type?.SpecialType;

            // First string or char argument is allowed for both method and constructor.
            //   ex. throw new Exception("Message", innerError);
            if (literalSpecialType is SpecialType.System_String or SpecialType.System_Char)
            {
                return true;
            }

            // But, don't allow omitting first int argument for constructor.
            //   ex. list = new(0);  // Expect: new(capacity: 0);
            if (!isConstructor && literalSpecialType is SpecialType.System_Int32)
            {
                return true;
            }

            return false;
        }
    }
}
