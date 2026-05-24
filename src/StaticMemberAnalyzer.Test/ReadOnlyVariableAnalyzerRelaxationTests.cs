// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ReadOnlyVariableAnalyzerRelaxationTests
    {
        [TestMethod]
        public async Task IEnumerableArgument_IsAllowed()
        {
            var test = @"
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Test
{
    class Program
    {
        static void Use(IEnumerable value) { }
        static void UseGeneric(IEnumerable<int> value) { }

        void M(IEnumerable eParam, IEnumerable<int> egParam)
        {
            IEnumerable e = null;
            IEnumerable<int> eg = null;
            Use(e);
            UseGeneric(eg);
            Use(eParam);
            UseGeneric(egParam);

            // LINQ methods
            eg.Any();
            egParam.Any();
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task EnumArgument_IsAllowed()
        {
            var test = @"
namespace Test
{
    enum E { A }
    class Program
    {
        static void Use(E value) { }

        void M(E eParam)
        {
            E e = E.A;
            Use(e);
            Use(eParam);
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task LambdaArgument_IsAllowed_ButViolationInsideReported()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        static void Use(Action action) { }

        void M()
        {
            Use(() => {
                int x = 0;
                {|#0:x|} = 1;
            });
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal).WithLocation(markupKey: 0).WithArguments("x");

            await VerifyWithRuleEnabledAsync(test, expected0);
        }

        [TestMethod]
        public async Task AnonymousMethodArgument_IsAllowed()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        static void Use(Action action) { }

        void M()
        {
            Use(delegate {
                int x = 0;
                {|#0:x|} = 1;
            });
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal).WithLocation(markupKey: 0).WithArguments("x");

            await VerifyWithRuleEnabledAsync(test, expected0);
        }

        [TestMethod]
        public async Task ActionVariable_ReportsDiagnostic()
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

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
