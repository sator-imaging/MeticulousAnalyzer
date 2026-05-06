// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ChainingReadOnlyTests
    {
        [TestMethod]
        public async Task ChainedAccess_WithMutableMiddleProp_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int MutableProp { get; set; }
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public B MutableB { get; set; }
        public readonly B ReadOnlyB => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = {|#0:foo.MutableB.ReadOnlyProp|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("foo.MutableB.ReadOnlyProp");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task ChainedAccess_AllReadOnly_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public readonly B ReadOnlyB => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.ReadOnlyB.ReadOnlyProp;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task ChainedAccess_WithMutableEndProp_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int MutableProp { get; set; }
    }

    struct C
    {
        public readonly B ReadOnlyB => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = {|#0:foo.ReadOnlyB.MutableProp|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("foo.ReadOnlyB.MutableProp");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task ChainedAccess_WithField_IgnoresFieldReadOnlyButChecksProp_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int MutableProp { get; set; }
    }

    struct C
    {
        public B FieldB;
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = {|#0:foo.FieldB.MutableProp|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("foo.FieldB.MutableProp");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task ChainedAccess_WithMutableMethodInChain_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public B GetB() => new B();
        public readonly B GetBReadOnly() => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = {|#0:foo.GetB().ReadOnlyProp|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("foo.GetB().ReadOnlyProp");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task ChainedAccess_WithReadOnlyMethodInChain_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public readonly B GetBReadOnly() => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.GetBReadOnly().ReadOnlyProp;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        private static async Task VerifyWithRuleEnabledAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
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
