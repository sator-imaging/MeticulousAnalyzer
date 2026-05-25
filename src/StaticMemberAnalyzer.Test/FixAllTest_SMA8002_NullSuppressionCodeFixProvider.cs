// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class FixAllTest_SMA8002_NullSuppressionCodeFixProvider
    {
        private const string SourceTemplate = @"
#nullable enable
namespace Test_{0}
{{
    public class C_{0}
    {{
        string? f;
        void M()
        {{
            var x = {{|#{1}:f!|}};
            var y = {{|#{2}:f!|}};
            var z = {{|#{3}:f!|}};
        }}
    }}
}}";

        private const string FixedTemplate = @"
#nullable enable
namespace Test_{0}
{{
    public class C_{0}
    {{
        string? f;
        void M()
        {{
            var x = (((f)))!;
            var y = (((f)))!;
            var z = (((f)))!;
        }}
    }}
}}";

        [TestMethod]
        public async Task Test_SMA8002_FixAllInSolution()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(SourceTemplate, 0, 0, 1, 2)),
                        ("Test1.cs", string.Format(SourceTemplate, 1, 3, 4, 5)),
                        ("Test2.cs", string.Format(SourceTemplate, 2, 6, 7, 8)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(FixedTemplate, 0)),
                        ("Test1.cs", string.Format(FixedTemplate, 1)),
                        ("Test2.cs", string.Format(FixedTemplate, 2)),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(FixedTemplate, 0)),
                        ("Test1.cs", string.Format(FixedTemplate, 1)),
                        ("Test2.cs", string.Format(FixedTemplate, 2)),
                    },
                },
                NumberOfIncrementalIterations = 9,
            };

            test.FixAllScope = FixAllScope.Solution;

            for (int i = 0; i < 3; i++)
            {
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: i * 3 + 0).WithArguments("f"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: i * 3 + 1).WithArguments("f"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: i * 3 + 2).WithArguments("f"));
            }

            // FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //   e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            // It's available in Roslyn version (4.4.0 or later).
            await test.RunAsync();
        }
    }
}
