// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0060_ReadOnlyVariableAnalyzerTests_Coverage
    {
        // TARGET: IsAllowedInStatementHeader - while loop condition branch (lines 397-402)
        // Assignment inside while condition should be allowed
        [TestMethod]
        public async Task SMA0060_Compliant_AssignmentInWhileCondition()
        {
            var test = @"
namespace Test
{
    class Program
    {
        string GetNext() => null;

        void M()
        {
            string x = null;
            while ((x = GetNext()) != null)
            {
                _ = x.Length;
            }
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        // TARGET: AnalyzeDeconstructionAssignment - conversion target (lines 154)
        // Deconstruction with implicit conversion triggers the IConversionOperation branch
        [TestMethod]
        public async Task SMA0060_Violation_DeconstructionAssignment_WithConversion()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            object left = null;
            object right = null;
            ({|#0:left|}, {|#1:right|}) = (""hello"", ""world"");
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("right");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        // TARGET: IsReadOnlyChainOrVariableWithMutablePrefix - conditional access with method call
        // This tests the IConditionalAccessOperation path (lines 422-425) and mutable invocation
        [TestMethod]
        public async Task SMA0064_Violation_ConditionalAccessMutableMethod()
        {
            var test = @"
namespace Test
{
    class C { public void Mutate() { } }

    class Program
    {
        void M()
        {
            var foo = new C();
            {|#0:foo?.Mutate()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("foo?.Mutate()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        // TARGET: IsReadOnlyChainOrVariableWithMutablePrefix - conditional property access on a class
        // Tests the IConditionalAccessOperation + property reference path (lines 470-485)
        [TestMethod]
        public async Task SMA0064_Violation_ConditionalAccessMutableProperty()
        {
            var test = @"
#nullable enable
namespace Test
{
    class C
    {
        public int MutableProp { get => 0; set {} }
    }

    class Program
    {
        void M(C? c)
        {
            _ = {|#0:c?.MutableProp|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 0)
                .WithArguments("c?.MutableProp", "c");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        // TARGET: TryGetRootLocalOrParameter - IConditionalAccessInstanceOperation path (lines 567-579)
        // When the analyzer walks a conditional access chain to resolve the root variable
        [TestMethod]
        public async Task SMA0064_Violation_ConditionalAccessChainMutableMethod()
        {
            var test = @"
namespace Test
{
    class Inner { public void Change() { } }
    class Outer { public Inner GetInner() => null; }

    class Program
    {
        void M()
        {
            var foo = new Outer();
            {|#0:foo?.GetInner()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("foo?.GetInner()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        // TARGET: AnalyzeStateChange - Parent is IConditionalAccessOperation (lines 204-207)
        // Differentiates from SMA0064_Violation_ConditionalAccessMutableMethod by using a parameter
        // (not a local variable), which exercises the IParameterReferenceOperation branch in
        // TryGetRootLocalOrParameter while still traversing the conditional access parent check
        [TestMethod]
        public async Task SMA0064_Violation_StateChangeViaConditionalAccess()
        {
            var test = @"
#nullable enable
namespace Test
{
    class C
    {
        public void DoSomething() { }
    }

    class Program
    {
        void M(C? c)
        {
            {|#0:c?.DoSomething()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("c?.DoSomething()", "c");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        private static async Task VerifyWithRuleEnabledAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            test.TestState.AnalyzerConfigFiles.Add(
                ("/.globalconfig", $"is_global = true\n{Core.Config_EnableImmutableVariable} = enable"));

            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var compilationOptions = project?.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                var specificOptions = compilationOptions.SpecificDiagnosticOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall,
                    ReportDiagnostic.Error);

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
