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
    public class C_{0}
    {{
        public void M(object p1, object p2, object p3) {{ }}
        public void Test()
        {{
            M({{|#0:null|}}, p2: null, p3: null);
            M(p1: null, {{|#1:null|}}, p3: null);
            M(p1: null, p2: null, {{|#2:null|}});
        }}
    }}
}}";

        private const string FixedTemplate = @"
namespace TestNamespace_{0}
{{
    public class C_{0}
    {{
        public void M(object p1, object p2, object p3) {{ }}
        public void Test()
        {{
            M(p1: null, p2: null, p3: null);
            M(p1: null, p2: null, p3: null);
            M(p1: null, p2: null, p3: null);
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
                string fname = $"File{i}.cs";
                int m0 = i * 3;
                int m1 = i * 3 + 1;
                int m2 = i * 3 + 2;

                test.TestState.Sources.Add((fname, string.Format(SourceTemplate, i).Replace("#0", $"#{m0}").Replace("#1", $"#{m1}").Replace("#2", $"#{m2}")));

                var fixedContent = string.Format(FixedTemplate, i);
                test.FixedState.Sources.Add((fname, fixedContent));
                test.BatchFixedState.Sources.Add((fname, fixedContent));

                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: m0).WithArguments("p1"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: m1).WithArguments("p2"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: m2).WithArguments("p3"));
            }

            await test.RunAsync();
        }
    }
}
