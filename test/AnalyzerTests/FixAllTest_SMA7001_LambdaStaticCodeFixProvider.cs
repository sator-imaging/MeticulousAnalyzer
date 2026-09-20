// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.LambdaAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.LambdaStaticCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class FixAllTest_SMA7001_LambdaStaticCodeFixProvider
    {
        private const string SystemDelegates = @"namespace System
{
    public delegate void Action<T1, T2, T3>(in T1 a, ref T2 b, out T3 c);
}";

        private const string SourceTemplate = @"using System;

namespace Test_{0}
{{
    public class C_{0}
    {{
        static void StaticMethod() {{ }}
        static void RefMethod(in int a, ref string b, out double c) {{ c = 0; }}
        void M()
        {{
            Action a = /* Leading trivia */ {{|#{1}:StaticMethod|}};  // Trailing trivia
            Action b = /* Leading trivia */ {{|#{2}:StaticMethod|}};  // Trailing trivia
            Action<int, string, double> c = /* Leading trivia */ {{|#{3}:RefMethod|}};  // Trailing trivia
        }}
    }}
}}";

        private const string FixedTemplate = @"using System;

namespace Test_{0}
{{
    public class C_{0}
    {{
        static void StaticMethod() {{ }}
        static void RefMethod(in int a, ref string b, out double c) {{ c = 0; }}
        void M()
        {{
            Action a = /* Leading trivia */ static () => StaticMethod();  // Trailing trivia
            Action b = /* Leading trivia */ static () => StaticMethod();  // Trailing trivia
            Action<int, string, double> c = /* Leading trivia */ static (in int a, ref string b, out double c) => RefMethod(in a, ref b, out c);  // Trailing trivia
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
                        ("SystemDelegates.cs", SystemDelegates),
                        ("Test0.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 0, 0, 1, 2)),
                        ("Test1.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 1, 3, 4, 5)),
                        ("Test2.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 2, 6, 7, 8)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("SystemDelegates.cs", SystemDelegates),
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                        ("Test2.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 2)),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("SystemDelegates.cs", SystemDelegates),
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
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration).WithLocation(markupKey: offset + 0).WithArguments("Action"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration).WithLocation(markupKey: offset + 1).WithArguments("Action"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration).WithLocation(markupKey: offset + 2).WithArguments("Action<int, string, double>"));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
