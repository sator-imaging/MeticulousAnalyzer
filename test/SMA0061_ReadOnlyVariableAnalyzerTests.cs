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
    public class SMA0061_ReadOnlyVariableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0061_Violation_CoalesceAssignment_Parameter()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M(int? foo)
        {
            {|#0:foo|} ??= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0061_Violation_MethodParameterAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M(int valueParam)
        {
            {|#0:valueParam|} = 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 0)
                .WithArguments("valueParam");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0061_Violation_IndexerAndSetterParameterAssignments_ReportDiagnostic()
        {
            var test = @"
namespace Test
{
    class MyType
    {
        int _x;

        public int SetterProp
        {
            set
            {
                {|#0:value|} = 123;
                _x = value;
            }
        }

        public int this[int index]
        {
            get => _x + index;
            set
            {
                {|#1:index|} += 1;
                {|#2:value|} = index;
                _x = value;
            }
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 0)
                .WithArguments("value");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 2)
                .WithArguments("value");
            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 1)
                .WithArguments("index");

            await VerifyWithRuleEnabledAsync(test, expected0, expected2, expected1);
        }


        [TestMethod]
        public async Task SMA0061_Violation_MemberAccessRootedAtParameter()
        {
            var test = @"
namespace Test
{
    class Box
    {
        public Box AutoPropNext { get; set; }
        public int AutoPropValue { get; set; }
    }

    class Program
    {
        void M(Box foo)
        {
            {|#0:foo.AutoPropNext.AutoPropValue|} = 310;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
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
