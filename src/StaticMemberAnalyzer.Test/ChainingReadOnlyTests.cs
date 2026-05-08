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
        public async Task ChainedAccess_WithMutableMiddleProp_DoesNotReportDiagnostic()
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
            _ = foo.MutableB.ReadOnlyProp;
            _ = foo.MutableB.ReadOnlyProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
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
        public async Task ChainedAccess_WithMutableEndProp_DoesNotReportDiagnostic()
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
            _ = foo.ReadOnlyB.MutableProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task ChainedAccess_WithField_IgnoresFieldReadOnlyButChecksProp_DoesNotReportDiagnostic()
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
            _ = foo.FieldB.MutableProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
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
            _ = foo.GetB().ReadOnlyProp;
            _ = foo.GetB().ReadOnlyProp;
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithSpan(20, 17, 20, 27)
                .WithArguments("foo.GetB()");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithSpan(20, 17, 20, 40)
                .WithArguments("foo.GetB().ReadOnlyProp");

            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithSpan(21, 17, 21, 27)
                .WithArguments("foo.GetB()");
            var expected3 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithSpan(21, 17, 21, 40)
                .WithArguments("foo.GetB().ReadOnlyProp");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1, expected2, expected3);
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

        [TestMethod]
        public async Task ChainedAccess_ThroughThis_NoDiagnosticIfAllReadOnly()
        {
            var test = @"
namespace Test
{
    struct B { public readonly int Prop => 1; }
    struct Program
    {
        public readonly B ReadOnlyB => new B();
        void M()
        {
            _ = this.ReadOnlyB.Prop;
            _ = ReadOnlyB.Prop;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task ChainedAccess_StaticMember_NoDiagnosticAtStartOfChain()
        {
            var test = @"
namespace Test
{
    struct B { public int MutableProp { get; set; } }
    class S
    {
        public static B StaticB => new B();
    }
    class Program
    {
        void M()
        {
            _ = S.StaticB.MutableProp;
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
                ("//.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = error"));

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
