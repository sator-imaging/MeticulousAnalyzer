// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0064_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0064_Violate_Combinations_MutableReturn_ReadOnly_AutoProp()
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
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(markupKey: 1)
                .WithArguments("s.MutableProp", "s");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0064_Violate_MethodCallOnRootLocal()
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
        public async Task SMA0064_Violate_MethodCallOnRootParameter()
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
        public void SMA0064_Conform_MethodCallRuleIsDisabledByDefault()
        {
            var analyzer = new ReadOnlyVariableAnalyzer();
            var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall);
            Assert.IsFalse(descriptor.IsEnabledByDefault, $"{ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall} should be disabled by default");
        }


        [TestMethod]
        public async Task SMA0064_Violate_ChainedAccess_WithMethodInChain()
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
            _ = foo.GetB().ReadOnlyProp;
            _ = foo.GetB().ReadOnlyProp;
        }
    }
}
";
            // Diagnostic spans overlap and cannot use markers.
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithSpan(startLine: 20, startColumn: 17, endLine: 20, endColumn: 27)
                .WithArguments("foo.GetB()", "foo");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithSpan(startLine: 20, startColumn: 17, endLine: 20, endColumn: 40)
                .WithArguments("foo.GetB().ReadOnlyProp", "foo");

            // Diagnostic spans overlap and cannot use markers.
            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithSpan(startLine: 21, startColumn: 17, endLine: 21, endColumn: 27)
                .WithArguments("foo.GetB()", "foo");
            var expected3 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithSpan(startLine: 21, startColumn: 17, endLine: 21, endColumn: 40)
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
                ("/.globalconfig", $"is_global = true\n{Core.Config_EnableImmutableVariable} = true"));

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
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument,
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
