// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0063_ReadOnlyVariableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0063_Violation_ReadWritePropertyAccess()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C _field;
        public C ReadWriteProp { get => _field; set => _field = value; }

        void M()
        {
            var self = this;
            _ = {|#0:self.ReadWriteProp|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 0)
                .WithArguments("self.ReadWriteProp", "self");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0063_Violation_ChainedAccess_MutablePropertyAndMethod()
        {
            var test = @"
namespace Test
{
    class C
    {
        public int Value { get; set; }
    }

    struct B
    {
        private C _c;
        public C Prop { get => _c; set => _c = value; }
        public C GetC() => _c;
    }

    class Program
    {
        void M()
        {
            var foo = new B();
            _ = {|#0:foo.Prop|};
            _ = {|#1:foo.GetC()|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 0)
                .WithArguments("foo.Prop", "foo");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 1)
                .WithArguments("foo.GetC()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0063_Violation_ChainedAccess_BlockBodiedMutable()
        {
            var test = @"
namespace Test
{
    class C { public int Value { get; set; } }
    struct B
    {
        private C _c;
        public C Prop
        {
            get { return _c; }
            set { _c = value; }
        }
        public C GetC()
        {
            return _c;
        }
    }

    class Program
    {
        void M()
        {
            var foo = new B();
            _ = {|#0:foo.Prop|};
            _ = {|#1:foo.GetC()|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 0)
                .WithArguments("foo.Prop", "foo");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 1)
                .WithArguments("foo.GetC()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
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
