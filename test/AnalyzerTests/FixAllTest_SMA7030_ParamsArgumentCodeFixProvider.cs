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
        void Test() {{ P(""hello"", {{|#{1}:1, 2, 3|}}); }}
    }}
}}";

        private const string FixedTemplate = @"
namespace Test_{0}
{{
    public class C_{0}
    {{
        void P(string name, params int[] values) {{}}
        void Test() {{ P(""hello"", new int[] {{ 1, 2, 3 }}); }}
    }}
}}";

        [TestMethod]
        public async Task SMA7030_CodeFix_ParamsImplicitAllocation_Solution()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 0, 0)),
                        ("Test1.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 1, 1)),
                        ("Test2.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 2, 2)),
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
                NumberOfIncrementalIterations = 3,
            };

            for (int i = 0; i < 3; i++)
            {
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                    .WithLocation(markupKey: i)
                    .WithArguments("values"));
            }

            await test.RunAsync();
        }
    }
}
