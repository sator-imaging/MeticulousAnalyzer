// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.Testing;
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
        private const string SourceTemplate = @"using System.Reflection;

namespace Test_{0}
{{
    /* Leading trivia */ public enum {{|#{1}:E_{0}_0|}} {{ V }} // Trailing trivia

    /* Leading trivia */ public enum {{|#{2}:E_{0}_1|}} {{ V }} // Trailing trivia

    /* Leading trivia */ public enum {{|#{3}:E_{0}_2|}} {{ V }} // Trailing trivia
}}";

        private const string FixedTemplate = @"using System.Reflection;

namespace Test_{0}
{{
    /* Leading trivia */
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E_{0}_0 {{ V }} // Trailing trivia

    /* Leading trivia */
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E_{0}_1 {{ V }} // Trailing trivia

    /* Leading trivia */
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E_{0}_2 {{ V }} // Trailing trivia
}}";

        [TestMethod]
        public async Task Test_SMA0026_FixAllInSolution()
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
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: i * 3 + 0));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: i * 3 + 1));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: i * 3 + 2));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
