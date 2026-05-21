// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.EnumAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.EnumObfuscationCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class FixAllTest_SMA0026_EnumObfuscationCodeFixProvider
    {
        private const string SourceTemplate = @"
using System.Reflection;
namespace TestNamespace_{0}
{{
    public enum {{|#0:E1_{0}|}} {{ Value }}
    public enum {{|#1:E2_{0}|}} {{ Value }}
    public enum {{|#2:E3_{0}|}} {{ Value }}
}}";

        private const string FixedTemplate = @"
using System.Reflection;
namespace TestNamespace_{0}
{{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E1_{0} {{ Value }}

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E2_{0} {{ Value }}

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E3_{0} {{ Value }}
}}";

        [TestMethod]
        public async Task TestFixAll()
        {
            var test = new VerifyCS.Test();
            test.CodeActionEquivalenceKey = "Exclude Enum Member from Obfuscation";

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

                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: m0).WithArguments($"E1_{i}"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: m1).WithArguments($"E2_{i}"));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: m2).WithArguments($"E3_{i}"));
            }

            await test.RunAsync();
        }
    }
}
