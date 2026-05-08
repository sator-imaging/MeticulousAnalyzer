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
        public async Task ChainedAccess_WithMiddleAutoProps_DoesNotReportDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int AutoProps { get; set; }
        public readonly int ReadOnlyAutoProps => 1;
    }

    struct C
    {
        public B AutoBProps { get; set; }
        public readonly B ReadOnlyBProps => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.AutoBProps.ReadOnlyAutoProps;
            _ = foo.AutoBProps.ReadOnlyAutoProps;
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
        public readonly int ReadOnlyAutoProps => 1;
    }

    struct C
    {
        public readonly B ReadOnlyBProps => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.ReadOnlyBProps.ReadOnlyAutoProps;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task ChainedAccess_WithEndAutoProps_DoesNotReportDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int AutoProps { get; set; }
    }

    struct C
    {
        public readonly B ReadOnlyBProps => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.ReadOnlyBProps.AutoProps;
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
        public int AutoProps { get; set; }
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
            _ = foo.FieldB.AutoProps;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task ChainedAccess_WithMethodInChain_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyAutoProps => 1;
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
            _ = foo.GetB().ReadOnlyAutoProps;
            _ = foo.GetB().ReadOnlyAutoProps;
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithSpan(20, 17, 20, 27)
                .WithArguments("foo.GetB()");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithSpan(20, 17, 20, 45)
                .WithArguments("foo.GetB().ReadOnlyAutoProps");

            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithSpan(21, 17, 21, 27)
                .WithArguments("foo.GetB()");
            var expected3 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithSpan(21, 17, 21, 45)
                .WithArguments("foo.GetB().ReadOnlyAutoProps");

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
        public readonly int ReadOnlyAutoProps => 1;
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
            _ = foo.GetBReadOnly().ReadOnlyAutoProps;
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
    struct B { public readonly int AutoProps => 1; }
    struct Program
    {
        public readonly B ReadOnlyBProps => new B();
        void M()
        {
            _ = this.ReadOnlyBProps.AutoProps;
            _ = ReadOnlyBProps.AutoProps;
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
    struct B { public int AutoProps { get; set; } }
    class S
    {
        public static B StaticB => new B();
    }
    class Program
    {
        void M()
        {
            _ = S.StaticB.AutoProps;
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
