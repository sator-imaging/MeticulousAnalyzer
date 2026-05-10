// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

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
            await VerifyWithSettingsAsync(source: TestCode, configContent: null);
        }

        [TestMethod]
        public async Task WhenConfigMissingSeverity_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(source: TestCode, configContent: "is_global = true\nsome_other_option = true");
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsNone_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(source: TestCode, configContent: "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = none");
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsError_DiagnosticReported()
        {
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithSettingsAsync(TestCode, "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = error", expected);
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsWarning_DiagnosticReported()
        {
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithSettingsAsync(TestCode, "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = warning", expected);
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsSuggestion_DiagnosticReported()
        {
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithSettingsAsync(TestCode, "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = suggestion", expected);
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsSilent_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(source: TestCode, configContent: "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = silent");
        }

        [TestMethod]
        public async Task WhenConfigSeverityIsDefault_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(source: TestCode, configContent: "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = default");
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


            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
