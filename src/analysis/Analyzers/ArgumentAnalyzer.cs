// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
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
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8000_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_LiteralArgument);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
            context.RegisterOperationAction(AnalyzeInvocationForParams, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzeObjectCreationForParams, OperationKind.ObjectCreation);
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
                IsOmittableFirstArgumentType(operation, isConstructor: true))
            {
                return;
            }

            string parameterName = UnknownParameterName;

            // Getting semantic model should be done right before emitting diagnostic for performance.
            if (argListStx.Parent != null &&
                context.SemanticModel.GetSymbolInfo(argListStx.Parent).Symbol is IMethodSymbol attrSymbol)
            {
                if (attrSymbol.Parameters.Length == 1 && IsDirectlyInSystemNamespace(attrSymbol.ContainingType))
                {
                    return;
                }

                if (unchecked((uint)argIndex < (uint)attrSymbol.Parameters.Length))
                {
                    parameterName = attrSymbol.Parameters[argIndex].ToDiagnosticMessageName();
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argStx.GetLocation(),
                parameterName));
        }

        private static void AnalyzeInvocationForParams(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
                return;

            if (IsKnownAssertionOrMathMethod(invocation))
                return;

            var method = invocation.TargetMethod;
            if (method.Parameters.Length == 0)
                return;

            var lastParam = method.Parameters[method.Parameters.Length - 1];
            if (!lastParam.IsParams)
                return;

            if (IsPervasiveSystemLib(method.ContainingType))
                return;

            if (method.Parameters.Length == 1 && IsDirectlyInSystemNamespace(method.ContainingType))
                return;

            ReportParamsArguments(context, invocation.Arguments, lastParam);
        }

        private static void AnalyzeObjectCreationForParams(OperationAnalysisContext context)
        {
            if (context.Operation is not IObjectCreationOperation creation)
                return;

            var ctor = creation.Constructor;
            if (ctor == null || ctor.Parameters.Length == 0)
                return;

            var lastParam = ctor.Parameters[ctor.Parameters.Length - 1];
            if (!lastParam.IsParams)
                return;

            if (IsPervasiveSystemLib(ctor.ContainingType))
                return;

            if (ctor.Parameters.Length == 1 && IsDirectlyInSystemNamespace(ctor.ContainingType))
                return;

            ReportParamsArguments(context, creation.Arguments, lastParam);
        }

        private static void ReportParamsArguments(OperationAnalysisContext context, ImmutableArray<IArgumentOperation> arguments, IParameterSymbol paramsParam)
        {
            IArgumentOperation? paramsArgOp = null;
            foreach (var arg in arguments)
            {
                if (SymbolEqualityComparer.Default.Equals(arg.Parameter, paramsParam))
                {
                    paramsArgOp = arg;
                    break;
                }
            }

            if (paramsArgOp == null || !paramsArgOp.IsImplicit)
                return;

            // Use the semantic IArrayCreationOperation to extract the actual params arguments.
            if (!TryUnwrapConversion(paramsArgOp.Value, out var unwrapped) ||
                unwrapped is not IArrayCreationOperation arrayCreation ||
                arrayCreation.Initializer == null)
            {
                return;
            }

            var paramsArgs = ImmutableArray.CreateBuilder<ArgumentSyntax>();
            bool hasLiteralOrRequired = false;
            foreach (var element in arrayCreation.Initializer.ElementValues)
            {
                var argSyntax = element.Syntax?.AncestorsAndSelf().FirstOrDefault(static n => n is ArgumentSyntax) as ArgumentSyntax;
                if (argSyntax != null)
                {
                    paramsArgs.Add(argSyntax);
                    if (IsPossibleOperation(element, out _))
                    {
                        hasLiteralOrRequired = true;
                    }
                }
            }

            if (paramsArgs.Count == 0 || !hasLiteralOrRequired)
                return;

            foreach (var arg in paramsArgs)
            {
                if (arg.NameColon != null)
                    return;
            }

            var firstArgStx = paramsArgs[0];
            var lastArgStx = paramsArgs[paramsArgs.Count - 1];

            // Create a location spanning from first to last params argument.
            var start = firstArgStx.SpanStart;
            var end = lastArgStx.Span.End;
            var location = Location.Create(
                firstArgStx.SyntaxTree,
                TextSpan.FromBounds(start, end));

            var properties = ImmutableDictionary.CreateBuilder<string, string?>();
            properties.Add("isParams", "true");
            properties.Add("paramsArgCount", paramsArgs.Count.ToString());

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                location,
                properties.ToImmutable(),
                paramsParam.ToDiagnosticMessageName()));
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
            if (invocationOp != null && IsKnownAssertionOrMathMethod(invocationOp))
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

                    var args = invocationOp?.Arguments ?? (argOp.Parent as IObjectCreationOperation)?.Arguments;
                    if (args?.Length == 1 && IsDirectlyInSystemNamespace(methodOrCtorContainer))
                    {
                        return;
                    }

                    // int, string or char is allowed if it's the first argument.
                    if (argStx.Parent is ArgumentListSyntax argListStx &&
                        argListStx.Arguments.IndexOf(argStx) == 0)
                    {
                        if (IsOmittableFirstArgumentType(argValue, isConstructor: invocationOp == null))
                        {
                            return;
                        }
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_LiteralArgument,
                argOp.Syntax.GetLocation(),
                argOp.Parameter?.ToDiagnosticMessageName() ?? UnknownParameterName));
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
        private static bool IsOmittableFirstArgumentType(IOperation operation, bool isConstructor)
        {
            var literalSpecialType = operation.Type?.SpecialType;

            // First string or char argument is allowed for both method and constructor.
            //   ex. throw new Exception("Message", innerError);
            if (literalSpecialType is SpecialType.System_String
                                   or SpecialType.System_Char
                                   // Most loggers take a message as an object instead of string
                                   or SpecialType.System_Object)
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

        private static bool IsKnownAssertionOrMathMethod(IInvocationOperation invocation)
        {
            return invocation.TargetMethod.ContainingType?.Name
                is "Must" or "Assert" or "Debug"
                // NOTE: 'Mathf' and 'math' for Unity engine and Burst compiler
                or "Math" or "Mathf" or "math";
        }

        private static bool IsPervasiveSystemLib(INamedTypeSymbol typeSymbol)
        {
            // String, System.Text and System.IO methods and constructors are intentionally allowed.
            return typeSymbol.SpecialType is SpecialType.System_String
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

        private static bool IsDirectlyInSystemNamespace(INamedTypeSymbol? typeSymbol)
        {
            return typeSymbol?.ContainingNamespace is INamespaceSymbol
            {
                Name: "System",
                ContainingNamespace: INamespaceSymbol { IsGlobalNamespace: true }
            };
        }
    }
}
