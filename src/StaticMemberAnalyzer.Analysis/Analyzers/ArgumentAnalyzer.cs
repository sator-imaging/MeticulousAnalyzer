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

            bool requireReporting = false;

            var operation = context.SemanticModel.GetOperation(argStx.Expression);
            if (operation != null &&
                !IsPossibleOperation(operation, out requireReporting))
            {
                return;
            }

            if (argStx.Parent is not AttributeArgumentListSyntax argListStx)
            {
                return;
            }

            int argIndex = argListStx.Arguments.IndexOf(argStx);

            // string or char is allowed if it's the first argument.
            if (!requireReporting &&
                argIndex == 0 &&
                operation != null &&
                IsOmittableType(operation, isConstructor: true))
            {
                return;
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

            // Test framework methods are exempt from all checks.
            var invocationOp = argOp.Parent as IInvocationOperation;
            if (invocationOp != null && IsKnownAssertionMethod(invocationOp))
            {
                return;
            }

            var argValue = argOp.Value;

            // 'null', 'default', or 'default(T)' is not allowed at all.
            if (!IsPossibleOperation(argValue, out var requireReporting))
            {
                return;
            }

            if (!requireReporting)
            {
                var methodOrCtorContainer = invocationOp?.TargetMethod.ContainingType
                                         ?? (argOp.Parent as IObjectCreationOperation)?.Constructor.ContainingType;

                if (methodOrCtorContainer is not null)
                {
                    if (IsPervasiveSystemLib(methodOrCtorContainer))
                    {
                        return;
                    }

                    // int, string or char is allowed if it's the first argument.
                    if (argStx.Parent is ArgumentListSyntax argListStx &&
                        argListStx.Arguments.IndexOf(argStx) == 0)
                    {
                        if (IsOmittableType(argValue, isConstructor: invocationOp == null))
                        {
                            return;
                        }
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argOp.Syntax.GetLocation(),
                argOp.Parameter?.Name ?? UnknownParameterName));
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

        private static bool IsPossibleOperation(IOperation operation, out bool requireReporting)
        {
            // NOTE: 'default' is wrapped with Conversion, but 'default(T)' is not.
            if (operation.Kind is OperationKind.Conversion &&
                TryUnwrapConversion(operation, out var unwrapped))
            {
                operation = unwrapped;
            }

            // 'null' and 'default' literals (including default(T)) are not allowed to be unnamed.
            var isNullOrDefaultLiteral
                = operation is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } }
                || operation.Kind is OperationKind.DefaultValue
                ;

            // Don't allow omitting named argument for inline boolean expressions.
            //   ex. 'x == y', 'x is not 0 and not 1' or etc.
            if (!isNullOrDefaultLiteral &&
                operation
                    // BUG: The following pattern doesn't work as expected (can be compiled but result is not correct).
                    //      --> operation is (A or B) { Type: { SpecialType: Boolean} }
                    is IBinaryOperation { Type: { SpecialType: SpecialType.System_Boolean } }
                    or IUnaryOperation { Type: { SpecialType: SpecialType.System_Boolean } }
                    // NOTE: IIsPatternOperation doesn't implement IPatternOperation
                    or IIsPatternOperation { Type: { SpecialType: SpecialType.System_Boolean } }
            )
            {
                requireReporting = true;
                return true;
            }
            else
            {
                requireReporting = isNullOrDefaultLiteral;
                return operation.Kind is OperationKind.Literal
                                      // Not required: `case 1` or `x is 2` (Not IsPatternOperator)
                                      //or OperationKind.ConstantPattern
                                      or OperationKind.DefaultValue
                                      ;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOmittableType(IOperation operation, bool isConstructor)
        {
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

        private static bool IsKnownAssertionMethod(IInvocationOperation invocation)
        {
            return invocation.TargetMethod.ContainingType.Name is "Must" or "Assert" or "Debug"
                || invocation.TargetMethod.ContainingType.Name is "Mathf"; // Mathf: wierd but for Unity engine.
        }

        private static bool IsPervasiveSystemLib(INamedTypeSymbol typeSymbol)
        {
            // String, System.Math, System.Text and System.IO methods and constructors are intentionally allowed.
            return typeSymbol.SpecialType is SpecialType.System_String
                || (typeSymbol.Name is "Math" && typeSymbol.ContainingNamespace is INamespaceSymbol
                {
                    Name: "System", ContainingNamespace: INamespaceSymbol
                    {
                        IsGlobalNamespace: true,
                    }
                })
                || typeSymbol.ContainingNamespace is INamespaceSymbol
                {
                    Name: "Text" or "IO", ContainingNamespace: INamespaceSymbol
                    {
                        Name: "System", ContainingNamespace: INamespaceSymbol
                        {
                            IsGlobalNamespace: true,
                        }
                    }
                };
        }
    }
}
