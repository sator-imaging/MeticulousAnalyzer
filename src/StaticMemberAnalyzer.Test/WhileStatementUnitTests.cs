using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class WhileStatementUnitTests
    {
        [TestMethod]
        public async Task SMA0060_Conform_WhileStatementCondition_SimpleAssignment_IsAllowed()
        {
            var test = @"
using System.IO;
namespace Test
{
    class Program
    {
        void M(Stream mut_stream)
        {
            int read;
            while ((read = mut_stream.Read(new byte[0], 0, 0)) > 0)
            {
            }
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task SMA0060_Violate_WhileStatementCondition_CompoundAssignment()
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
                .WithLocation(markupKey: 0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Violate_WhileStatementCondition_Increment()
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
                .WithLocation(markupKey: 0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Violate_WhileStatementBody_Assignment()
        {
            var test = @"
using System.IO;
namespace Test
{
    class Program
    {
        void M(Stream mut_stream)
        {
            int read;
            while ((read = mut_stream.Read(new byte[0], 0, 0)) > 0)
            {
                {|#0:read|} = 0;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
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

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
