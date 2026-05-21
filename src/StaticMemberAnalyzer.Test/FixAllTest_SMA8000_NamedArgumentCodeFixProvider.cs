// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

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
namespace TestNamespace_{0}
{{
    public class C
    {{
        void M(int a, float b) {{ }}
        void Test()
        {{
            M(0, {{|#0:1.0f|}});
            M(0, {{|#1:2.0f|}});
            M(0, {{|#2:3.0f|}});
        }}
    }}
}}";

        private const string FixedTemplate = @"
namespace TestNamespace_{0}
{{
    public class C
    {{
        void M(int a, float b) {{ }}
        void Test()
        {{
            M(0, b: 1.0f);
            M(0, b: 2.0f);
            M(0, b: 3.0f);
        }}
    }}
}}";

        [TestMethod]
        public async Task TestFixAll()
        {
            var test = new VerifyCS.Test();
            test.CodeActionEquivalenceKey = "Use named argument";

            for (int i = 0; i < 3; i++)
            {
                var fname = $"File{i}.cs";
                int m0 = i * 3;
                int m1 = i * 3 + 1;
                int m2 = i * 3 + 2;

                test.TestState.Sources.Add((fname, string.Format(SourceTemplate, i, m0, m1, m2).Replace("#0", $"#{m0}").Replace("#1", $"#{m1}").Replace("#2", $"#{m2}")));
                test.FixedState.Sources.Add((fname, string.Format(FixedTemplate, i)));

                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(m0).WithArguments("b"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(m1).WithArguments("b"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(m2).WithArguments("b"));
            }

            await test.RunAsync();
        }
    }
}
