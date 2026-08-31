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
        void M(string req, int count = 1, bool flag = false, CancellationToken ct = default) {{}}

        void Test()
        {{
            M(""test"", {{|#{1}:10|}}, {{|#{2}:true|}}, CancellationToken.None);
        }}
    }}
}}";

        private const string FixedTemplate = @"
namespace Test_{0}
{{
    using System.Threading;

    public class C_{0}
    {{
        void M(string req, int count = 1, bool flag = false, CancellationToken ct = default) {{}}

        void Test()
        {{
            M(""test"", count: 10, flag: true, CancellationToken.None);
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
                        ("Test0.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 0, 0, 1)),
                        ("Test1.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 1, 2, 3)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("CancellationToken.cs", CancellationTokenSource.ReplaceLineEndings()),
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("CancellationToken.cs", CancellationTokenSource.ReplaceLineEndings()),
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                    },
                },
                NumberOfIncrementalIterations = 4,
            };

            for (int i = 0; i < 2; i++)
            {
                int offset = i * 2;
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: offset + 0).WithArguments("count"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: offset + 1).WithArguments("flag"));
            }

            await test.RunAsync();
        }
    }
}
