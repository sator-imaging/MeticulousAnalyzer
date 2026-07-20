// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCs = CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0062_ReadOnlyVariableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0062_Violation_MutableMembers_NonStringType()
        {
            var test = @"
namespace Test
{
    class C { public int Value; }
    class B
    {
        public C Field;
        public C Prop { get => Field; set => Field = value; }
        public void Do() { }
    }

    class Program
    {
        static void Use(C c) { }
        void M()
        {
            var foo = new B();
            Use({|#0:foo.Field|});
            _ = {|#1:foo.Prop|};
            {|#2:foo.Do()|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState)
                .WithLocation(markupKey: 1)
                .WithArguments("foo.Prop", "foo");
            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 2)
                .WithArguments("foo.Do()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1, expected2);
        }


        [TestMethod]
        public async Task SMA0062_Violation_MethodCall_ReferenceTypeArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        static void Use(C value) { }

        void M()
        {
            var foo = new C();
            Use({|#0:foo|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0062_Violation_StructArgument_MutableByValue()
        {
            var test = @"
namespace Test
{
    struct S { public int X; }

    class Program
    {
        static void Use(S value) { }

        void M()
        {
            var s = new S();
            Use({|#0:s|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(markupKey: 0)
                .WithArguments("s");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0062_Violation_FieldArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C _field = new C();
        static void Use(C value) { }

        void M()
        {
            var self = this;
            Use({|#0:self._field|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(markupKey: 0)
                .WithArguments("self");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0062_Violation_ActionVariable()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        static void Use(Action action) { }

        void M(Action aParam)
        {
            Action a = () => { };
            Use({|#0:a|});
            Use({|#1:aParam|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument).WithLocation(markupKey: 0).WithArguments("a");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument).WithLocation(markupKey: 1).WithArguments("aParam");

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
