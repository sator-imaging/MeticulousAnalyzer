// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.LambdaAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.LambdaStaticCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class FixAllTest_SMA7001_LambdaStaticCodeFixProvider
    {
        private const string SourceTemplate = @"using System;

namespace Test_{0}
{{
    public class C_{0}
    {{
        static void StaticMethod() {{ }}
        void M()
        {{
            Action a = /* Leading trivia */ {{|#{1}:StaticMethod|}};  // Trailing trivia
            Action b = /* Leading trivia */ {{|#{2}:StaticMethod|}};  // Trailing trivia
            Action c = /* Leading trivia */ {{|#{3}:StaticMethod|}};  // Trailing trivia
        }}
    }}
}}";

        private const string FixedTemplate = @"using System;

namespace Test_{0}
{{
    public class C_{0}
    {{
        static void StaticMethod() {{ }}
        void M()
        {{
            Action a = /* Leading trivia */ static () => StaticMethod();  // Trailing trivia
            Action b = /* Leading trivia */ static () => StaticMethod();  // Trailing trivia
            Action c = /* Leading trivia */ static () => StaticMethod();  // Trailing trivia
        }}
    }}
}}";

        [TestMethod]
        public async Task SMA7001_CodeFix_FixAllInSolution()
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
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration).WithLocation(markupKey: i * 3 + 0).WithArguments("Action"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration).WithLocation(markupKey: i * 3 + 1).WithArguments("Action"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration).WithLocation(markupKey: i * 3 + 2).WithArguments("Action"));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
