// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0064_ReadOnlyVariableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0064_Violation_Combinations_MutableReturn_ReadOnly_AutoProp()
        {
            var test = @"
namespace Test
{
    class C { public int Value; }
    struct S
    {
        public readonly C ReadOnlyAutoProp { get; }
        public readonly C ReadOnlyMethod() => null;
        public readonly C ReadOnlyBlockMethod() { return null; }
        public readonly C ReadOnlyBlockProp { get { return null; } }
        public C MutableMethod() { return null; }
        public C MutableProp { get => null; set {} }
    }

    class Program
    {
        void M()
        {
            var s = new S();
            _ = s.ReadOnlyAutoProp;
            _ = s.ReadOnlyMethod();
            _ = s.ReadOnlyBlockMethod();
            _ = s.ReadOnlyBlockProp;
            _ = {|#0:s.MutableMethod()|};
            _ = {|#1:s.MutableProp|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("s.MutableMethod()", "s");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 1)
                .WithArguments("s.MutableProp", "s");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0064_Violation_MethodCallOnRootLocal()
        {
            var test = @"
namespace Test
{
    class C { public void Do() { } }

    class Program
    {
        void M()
        {
            var foo = new C();
            {|#0:foo.Do()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("foo.Do()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0064_Violation_MethodCallOnRootParameter()
        {
            var test = @"
namespace Test
{
    class C { public void Do() { } }

    class Program
    {
        void M(C foo)
        {
            {|#0:foo.Do()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("foo.Do()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0064_Violation_ChainedAccess_WithMethodInChain()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public B GetB() => new B();
        public readonly B GetBReadOnly() => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = {|#1:{|#0:foo.GetB()|}.ReadOnlyProp|};
            _ = {|#3:{|#2:foo.GetB()|}.ReadOnlyProp|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("foo.GetB()", "foo");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 1)
                .WithArguments("foo.GetB().ReadOnlyProp", "foo");

            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 2)
                .WithArguments("foo.GetB()", "foo");
            var expected3 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 3)
                .WithArguments("foo.GetB().ReadOnlyProp", "foo");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1, expected2, expected3);
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
