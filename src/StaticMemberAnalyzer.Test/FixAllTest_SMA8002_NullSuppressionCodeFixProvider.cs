// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class FixAllTest_SMA8002_NullSuppressionCodeFixProvider
    {
        private const string SourceTemplate = @"
#nullable disable
namespace TestNamespace_{0}
{{
    public class C
    {{
        string s = null;
        void Test()
        {{
            var a = {{|#0:s!|}};
            var b = {{|#1:s!|}};
            var c = {{|#2:s!|}};
        }}
    }}
}}";

        private const string FixedTemplate = @"
#nullable disable
namespace TestNamespace_{0}
{{
    public class C
    {{
        string s = null;
        void Test()
        {{
            var a = (((s)))!;
            var b = (((s)))!;
            var c = (((s)))!;
        }}
    }}
}}";

        [TestMethod]
        public async Task TestFixAll()
        {
            var test = new VerifyCS.Test();
            test.CodeActionEquivalenceKey = "Add 3 parentheses fence";

            for (int i = 0; i < 3; i++)
            {
                var fname = $"File{i}.cs";
                int m0 = i * 3;
                int m1 = i * 3 + 1;
                int m2 = i * 3 + 2;

                test.TestState.Sources.Add((fname, string.Format(SourceTemplate, i).Replace("#0", $"#{m0}").Replace("#1", $"#{m1}").Replace("#2", $"#{m2}")));
                test.FixedState.Sources.Add((fname, string.Format(FixedTemplate, i)));

                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(m0));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(m1));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(m2));
            }

            await test.RunAsync();
        }
    }
}
