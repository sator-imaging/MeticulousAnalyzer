// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
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
            var x = /* Leading trivia */ {{|#{1}:f!|}} // Trailing trivia
;
            var y = /* Leading trivia */ {{|#{2}:f!|}} // Trailing trivia
;
            var z = /* Leading trivia */ {{|#{3}:f!|}} // Trailing trivia
;
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
            var x = /* Leading trivia */ (((f)))! // Trailing trivia
;
            var y = /* Leading trivia */ (((f)))! // Trailing trivia
;
            var z = /* Leading trivia */ (((f)))! // Trailing trivia
;
        }}
    }}
}}";

        [TestMethod]
        public async Task SMA8002_CodeFix_FixAllInSolution()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 0, 0, 1, 2)),
                        ("Test1.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 1, 3, 4, 5)),
                        ("Test2.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 2, 6, 7, 8)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                        ("Test2.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 2)),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                        ("Test2.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 2)),
                    },
                },
                NumberOfIncrementalIterations = 9,
            };

            for (int i = 0; i < 3; i++)
            {
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: i * 3 + 0).WithArguments("f"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: i * 3 + 1).WithArguments("f"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: i * 3 + 2).WithArguments("f"));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
