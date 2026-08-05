// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ParamsArgumentAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.ParamsArgumentCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class FixAllTest_SMA7030_ParamsArgumentCodeFixProvider
    {
        private const string SourceTemplate = @"
namespace Test_{0}
{{
    public class C_{0}
    {{
        void P(string name, params int[] values) {{}}
        void Q(params string[] tags) {{}}
        void R(int x, params double[] nums) {{}}
        void Test() {{
            P(""hello"", /* Leading trivia */ {{|#{1}:1, 2, 3|}} // Trailing trivia
);
            Q(/* Leading trivia */ {{|#{2}:""a"", ""b"", ""c""|}} // Trailing trivia
);
            R(0, /* Leading trivia */ {{|#{3}:1.0, 2.0, 3.0|}} // Trailing trivia
);
        }}
    }}
}}";

        private const string FixedTemplate = @"
namespace Test_{0}
{{
    public class C_{0}
    {{
        void P(string name, params int[] values) {{}}
        void Q(params string[] tags) {{}}
        void R(int x, params double[] nums) {{}}
        void Test() {{
            P(""hello"", /* Leading trivia */ new int[] {{ 1, 2, 3 // Trailing trivia
            }});
            Q(/* Leading trivia */ new string[] {{ ""a"", ""b"", ""c"" // Trailing trivia
            }});
            R(0, /* Leading trivia */ new double[] {{ 1.0, 2.0, 3.0 // Trailing trivia
            }});
        }}
    }}
}}";

        [TestMethod]
        public async Task SMA7030_CodeFix_FixAllInSolution()
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
                int offset = i * 3;
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation).WithLocation(markupKey: offset + 0).WithArguments("values"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation).WithLocation(markupKey: offset + 1).WithArguments("tags"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation).WithLocation(markupKey: offset + 2).WithArguments("nums"));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
