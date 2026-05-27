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
        public async Task SMA0060_Conform_WhenNoConfig_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(TestCode, configContent: null);
        }

        [TestMethod]
        public async Task SMA0060_Conform_WhenConfigMissingSeverity_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(TestCode, configContent: "is_global = true\nsome_other_option = true");
        }

        [TestMethod]
        public async Task SMA0060_Conform_WhenConfigSeverityIsFalse_NoDiagnosticReported()
        {
            await VerifyWithSettingsAsync(TestCode, configContent: $"is_global = true\n{Core.Config_EnableImmutableVariable} = false");
        }

        [TestMethod]
        public async Task SMA0060_Violate_WhenConfigSeverityIsTrue_DiagnosticReported()
        {
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithSettingsAsync(TestCode, configContent: $"is_global = true\n{Core.Config_EnableImmutableVariable} = true", expected);
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
