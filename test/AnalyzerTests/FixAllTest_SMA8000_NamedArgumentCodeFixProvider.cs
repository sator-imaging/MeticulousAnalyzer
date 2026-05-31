// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NamedArgumentCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class FixAllTest_SMA8000_NamedArgumentCodeFixProvider
    {
        private const string SourceTemplate = @"
namespace Test_{0}
{{
    public class C_{0}
    {{
        void M(bool a, bool b, bool c) {{}}
        void Test() {{ M(/* Leading trivia */ {{|#{1}:true|}} // Trailing trivia
, /* Leading trivia */ {{|#{2}:false|}} // Trailing trivia
, /* Leading trivia */ {{|#{3}:true|}} // Trailing trivia
); }}
    }}
}}";

        private const string FixedTemplate = @"
namespace Test_{0}
{{
    public class C_{0}
    {{
        void M(bool a, bool b, bool c) {{}}
        void Test() {{ M(/* Leading trivia */ a: true // Trailing trivia
, /* Leading trivia */ b: false // Trailing trivia
, /* Leading trivia */ c: true // Trailing trivia
); }}
    }}
}}";

        [TestMethod]
        public async Task SMA8000_CodeFix_FixAllInSolution()
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
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: i * 3 + 0).WithArguments("a"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: i * 3 + 1).WithArguments("b"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: i * 3 + 2).WithArguments("c"));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }

        private const string ParamsSourceTemplate = @"
namespace TestParams_{0}
{{
    public class CP_{0}
    {{
        void M(string name, params int[] values) {{}}
        void N(params string[] items) {{}}
        void O(bool flag, int count) {{}}
        void Test()
        {{
            M(""hello"", {{|#{1}:1, 2, 3|}});
            N({{|#{2}:""a"", ""b"", ""c""|}});
            O({{|#{3}:true|}}, {{|#{4}:42|}});
        }}
    }}
}}";

        private const string ParamsFixedTemplate = @"
namespace TestParams_{0}
{{
    public class CP_{0}
    {{
        void M(string name, params int[] values) {{}}
        void N(params string[] items) {{}}
        void O(bool flag, int count) {{}}
        void Test()
        {{
            M(""hello"", values: new int[] {{ 1, 2, 3 }});
            N(items: new string[] {{ ""a"", ""b"", ""c"" }});
            O(flag: true, count: 42);
        }}
    }}
}}";

        [TestMethod]
        public async Task SMA8000_CodeFix_FixAllInSolution_WithParams()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("TestParams0.cs", string.Format(ParamsSourceTemplate.ReplaceLineEndings(), 0, 0, 1, 2, 3)),
                        ("TestParams1.cs", string.Format(ParamsSourceTemplate.ReplaceLineEndings(), 1, 4, 5, 6, 7)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("TestParams0.cs", string.Format(ParamsFixedTemplate.ReplaceLineEndings(), 0)),
                        ("TestParams1.cs", string.Format(ParamsFixedTemplate.ReplaceLineEndings(), 1)),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("TestParams0.cs", string.Format(ParamsFixedTemplate.ReplaceLineEndings(), 0)),
                        ("TestParams1.cs", string.Format(ParamsFixedTemplate.ReplaceLineEndings(), 1)),
                    },
                },
                NumberOfIncrementalIterations = 8,
            };

            for (int i = 0; i < 2; i++)
            {
                int offset = i * 4;
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: offset + 0).WithArguments("values"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: offset + 1).WithArguments("items"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: offset + 2).WithArguments("flag"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: offset + 3).WithArguments("count"));
            }

            await test.RunAsync();
        }
    }
}
