using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class WhileStatementUnitTests
    {
        [TestMethod]
        public async Task WhileStatementCondition_SimpleAssignment_IsAllowed()
        {
            var test = @"
using System.IO;
namespace Test
{
    class Program
    {
        void M(Stream stream)
        {
            int read;
            while ((read = stream.Read(new byte[0], 0, 0)) > 0)
            {
            }
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task WhileStatementCondition_CompoundAssignment_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int i = 0;
            while (({|#0:i|} += 1) < 10)
            {
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task WhileStatementCondition_Increment_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int i = 0;
            while ({|#0:i|}++ < 10)
            {
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task WhileStatementBody_Assignment_ReportsDiagnostic()
        {
            var test = @"
using System.IO;
namespace Test
{
    class Program
    {
        void M(Stream stream)
        {
            int read;
            while ((read = stream.Read(new byte[0], 0, 0)) > 0)
            {
                {|#0:read|} = 0;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("read");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        private static async Task VerifyWithRuleEnabledAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            test.TestState.AnalyzerConfigFiles.Add(
                ("/.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = error"));

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
