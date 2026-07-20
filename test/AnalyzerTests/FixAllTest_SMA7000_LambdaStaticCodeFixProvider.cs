// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.LambdaAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.LambdaStaticCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class FixAllTest_SMA7000_LambdaStaticCodeFixProvider
    {
        private const string SourceTemplate = @"using System;
using System.Threading.Tasks;

namespace Test_{0}
{{
    public class C_{0}
    {{
        void M()
        {{
            Func<Task> a = /* Leading trivia */ {{|#{1}:async () => {{ }}|}};  // Trailing trivia
            Func<Task> b = /* Leading trivia */ {{|#{2}:async () => {{ }}|}};  // Trailing trivia
            Func<Task> c = /* Leading trivia */ {{|#{3}:async () => {{ }}|}};  // Trailing trivia
        }}
    }}
}}";

        private const string FixedTemplate = @"using System;
using System.Threading.Tasks;

namespace Test_{0}
{{
    public class C_{0}
    {{
        void M()
        {{
            Func<Task> a = /* Leading trivia */ static async () => {{ }};  // Trailing trivia
            Func<Task> b = /* Leading trivia */ static async () => {{ }};  // Trailing trivia
            Func<Task> c = /* Leading trivia */ static async () => {{ }};  // Trailing trivia
        }}
    }}
}}";

        [TestMethod]
        public async Task SMA7000_CodeFix_FixAllInSolution()
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
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: i * 3 + 0));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: i * 3 + 1));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: i * 3 + 2));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
