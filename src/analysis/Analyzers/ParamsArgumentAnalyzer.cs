// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParamsArgumentAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_ImplicitParamsAllocation = "SMA7030";

        private static readonly DiagnosticDescriptor Rule_ImplicitParamsAllocation = new(
            RuleId_ImplicitParamsAllocation,
            new LocalizableResourceString(nameof(Resources.SMA7030_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7030_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(ParamsArgumentAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7030_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_ImplicitParamsAllocation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocationForParams, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzeObjectCreationForParams, OperationKind.ObjectCreation);
        }

        private static void AnalyzeInvocationForParams(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
                return;

            var method = invocation.TargetMethod;
            if (method.Parameters.Length == 0)
                return;

            var lastParam = method.Parameters[method.Parameters.Length - 1];
            if (!lastParam.IsParams)
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
            foreach (var element in arrayCreation.Initializer.ElementValues)
            {
                var argSyntax = element.Syntax?.AncestorsAndSelf().FirstOrDefault(static n => n is ArgumentSyntax) as ArgumentSyntax;
                if (argSyntax != null)
                {
                    paramsArgs.Add(argSyntax);
                }
            }

            if (paramsArgs.Count == 0)
                return;

            var firstArgStx = paramsArgs[0];
            var lastArgStx = paramsArgs[paramsArgs.Count - 1];

            // Create a location spanning from first to last params argument.
            var start = firstArgStx.SpanStart;
            var end = lastArgStx.Span.End;
            var location = Location.Create(
                firstArgStx.SyntaxTree,
                TextSpan.FromBounds(start, end));

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_ImplicitParamsAllocation,
                location,
                paramsParam.ToDiagnosticMessageName()));
        }

        private static bool TryUnwrapConversion(IOperation operation, out IOperation unwrapped)
        {
            var value = operation;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }

            return (unwrapped = value) != null;
        }
    }
}
