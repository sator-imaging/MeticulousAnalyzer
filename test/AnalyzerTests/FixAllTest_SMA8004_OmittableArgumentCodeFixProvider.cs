// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.OmittableArgumentAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.NamedArgumentCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class FixAllTest_SMA8004_OmittableArgumentCodeFixProvider
    {
        private const string CancellationTokenSource = @"
namespace System.Threading
{
    public struct CancellationToken
    {
        public static CancellationToken None => default;
    }
}
";

        private const string SourceTemplate = @"
namespace Test_{0}
{{
    using System.Threading;

    public class C_{0}
    {{
        void M(string req, int a = 1, bool b = false, CancellationToken ct = default) {{}}
        void N(string s = ""default"", CancellationToken ct = default) {{}}
        void P(CancellationToken cancellationToken = default) {{}}

        void Test()
        {{
            M(""test"", /* Leading trivia */ {{|#{1}:10|}} // Trailing trivia
, /* Leading trivia */ {{|#{2}:true|}} // Trailing trivia
, CancellationToken.None);
            N(/* Leading trivia */ {{|#{3}:""hello""|}} /* Trailing trivia */, CancellationToken.None);
            P(// Leading trivia
              CancellationToken.None
              // Trailing trivia
);
        }}
    }}
}}";

        private const string FixedTemplate = @"
namespace Test_{0}
{{
    using System.Threading;

    public class C_{0}
    {{
        void M(string req, int a = 1, bool b = false, CancellationToken ct = default) {{}}
        void N(string s = ""default"", CancellationToken ct = default) {{}}
        void P(CancellationToken cancellationToken = default) {{}}

        void Test()
        {{
            M(""test"", /* Leading trivia */ a: 10 // Trailing trivia
, /* Leading trivia */ b: true // Trailing trivia
, CancellationToken.None);
            N(/* Leading trivia */ s: ""hello"" /* Trailing trivia */, CancellationToken.None);
            P(// Leading trivia
              CancellationToken.None
              // Trailing trivia
);
        }}
    }}
}}";

        [TestMethod]
        public async Task SMA8004_CodeFix_FixAllInSolution()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("CancellationToken.cs", CancellationTokenSource.ReplaceLineEndings()),
                        ("Test0.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 0, 0, 1, 2)),
                        ("Test1.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 1, 3, 4, 5)),
                        ("Test2.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 2, 6, 7, 8)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("CancellationToken.cs", CancellationTokenSource.ReplaceLineEndings()),
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                        ("Test2.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 2)),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("CancellationToken.cs", CancellationTokenSource.ReplaceLineEndings()),
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
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: offset + 0).WithArguments("a"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: offset + 1).WithArguments("b"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: offset + 2).WithArguments("s"));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
