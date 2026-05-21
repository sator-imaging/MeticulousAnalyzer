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
namespace TestNamespace_{0}
{{
    public class C_{0}
    {{
        public void M(object a, object b, object c)
        {{
            var v1 = {{|#0:a!|}};
            var v2 = {{|#1:b!|}};
            var v3 = {{|#2:c!|}};
        }}
    }}
}}";

        private const string FixedTemplate = @"
namespace TestNamespace_{0}
{{
    public class C_{0}
    {{
        public void M(object a, object b, object c)
        {{
            var v1 = (((a)))!;
            var v2 = (((b)))!;
            var v3 = (((c)))!;
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
                string fname = $"File{i}.cs";
                int m0 = i * 3;
                int m1 = i * 3 + 1;
                int m2 = i * 3 + 2;

                test.TestState.Sources.Add((fname, string.Format(SourceTemplate, i).Replace("#0", $"#{m0}").Replace("#1", $"#{m1}").Replace("#2", $"#{m2}")));

                var fixedContent = string.Format(FixedTemplate, i);
                test.FixedState.Sources.Add((fname, fixedContent));
                test.BatchFixedState.Sources.Add((fname, fixedContent));

                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: m0));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: m1));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: m2));
            }

            await test.RunAsync();
        }
    }
}
