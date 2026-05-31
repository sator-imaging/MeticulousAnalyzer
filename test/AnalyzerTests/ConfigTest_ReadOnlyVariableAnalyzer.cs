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
    public class ConfigTest_ReadOnlyVariableAnalyzer
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
        public async Task SMA0060_Config_RuleSuppression()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            foo = 1;
        }
    }
}
";

            var verifier = new VerifyCS.Test
            {
                TestCode = test,
            };

            verifier.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var compilationOptions = project?.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                var specificOptions = compilationOptions.SpecificDiagnosticOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                    ReportDiagnostic.Suppress);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter,
                    ReportDiagnostic.Suppress);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument,
                    ReportDiagnostic.Suppress);

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            await verifier.RunAsync();
        }


        [TestMethod]
        public void SMA0060_Config_RulesAreDisabledByDefault()
        {
            var analyzer = new ReadOnlyVariableAnalyzer();
            var ids = new[]
            {
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter,
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument,
                ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState,
            };

            foreach (var id in ids)
            {
                var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == id);
                Assert.IsFalse(descriptor.IsEnabledByDefault, $"{id} should be disabled by default");
            }
        }


        [TestMethod]
        public async Task SMA0060_Config_NoConfigPresent()
        {
            await VerifyWithSettingsAsync(TestCode, configContent: null);
        }


        [TestMethod]
        public async Task SMA0060_Config_MissingSeveritySetting()
        {
            await VerifyWithSettingsAsync(TestCode, configContent: "is_global = true\nsome_other_option = true");
        }


        [TestMethod]
        public async Task SMA0060_Config_SeverityIsFalse()
        {
            await VerifyWithSettingsAsync(TestCode, configContent: $"is_global = true\n{Core.Config_EnableImmutableVariable} = false");
        }


        [TestMethod]
        public async Task SMA0060_Config_SeverityIsTrue_Diagnostic()
        {
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithSettingsAsync(TestCode, configContent: $"is_global = true\n{Core.Config_EnableImmutableVariable} = true", expected);
        }


        [TestMethod]
        public void SMA0064_Config_MethodCallRuleIsDisabledByDefault()
        {
            var analyzer = new ReadOnlyVariableAnalyzer();
            var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall);
            Assert.IsFalse(descriptor.IsEnabledByDefault, $"{ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall} should be disabled by default");
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
