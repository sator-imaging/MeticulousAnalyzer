// Licensed under the MIT License
// https://github.com/sator-imaging/CSharp-StaticFieldAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ReadOnlyVariableAnalyzerConfigTests
    {
        private const string TestCode = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            {|#0:foo|} = 1;
        }
    }
}
";

        [TestMethod]
        public async Task WhenNoConfig_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(TestCode, null);
        }

        [TestMethod]
        public async Task WhenConfigMissingSeverity_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(TestCode, "is_global = true\nsome_other_option = true");
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsNone_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(TestCode, "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = none");
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsError_DiagnosticReported()
        {
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithSettingsAsync(TestCode, "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = error", expected);
        }

        private static async Task VerifyWithSettingsAsync(string source, string configContent, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            if (configContent != null)
            {
                test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", configContent));
            }

            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var compilationOptions = project?.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                // Force enable the rules in compilation options, but the analyzer itself should check for the config.
                var specificOptions = compilationOptions.SpecificDiagnosticOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                    ReportDiagnostic.Error);

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
