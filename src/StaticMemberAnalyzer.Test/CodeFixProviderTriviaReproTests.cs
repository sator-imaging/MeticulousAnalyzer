// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using StaticMemberAnalyzer.Test;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class CodeFixProviderTriviaReproTests
    {
        [TestMethod]
        public async Task SMA8000_CodeFix_NamedArgumentKeywordHandling_Repro()
        {
            var test = @"
public class C
{
    void M(int @class, int x) { }
    void Call()
    {
        M(0, {|#0:1|});
    }
}
";
            var fixtest = @"
public class C
{
    void M(int @class, int x) { }
    void Call()
    {
        M(0, x: 1);
    }
}
";
            var expected = CSharpCodeFixVerifier<ArgumentAnalyzer, NamedArgumentCodeFixProvider>
                .Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("x");

            // Re-trying keyword test with nullable type
             var testKeyword = @"
public class C
{
    void M(string @class) { }
    void Call()
    {
        M({|#0:null|});
    }
}
";
            var fixtestKeyword = @"
public class C
{
    void M(string @class) { }
    void Call()
    {
        M(@class: null);
    }
}
";
            var expectedKeyword = CSharpCodeFixVerifier<ArgumentAnalyzer, NamedArgumentCodeFixProvider>
                .Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("class");

            await CSharpCodeFixVerifier<ArgumentAnalyzer, NamedArgumentCodeFixProvider>
                .VerifyCodeFixAsync(testKeyword, expectedKeyword, fixtestKeyword);
        }

        [TestMethod]
        public async Task SMA8002_CodeFix_NullSuppressionTriviaPreservation_Repro()
        {
            var test = @"
public class C
{
    void M(string s)
    {
        _ = /* leading */ {|#0:s!|} /* trailing */;
    }
}
";
            var fixtest = @"
public class C
{
    void M(string s)
    {
        _ = /* leading */ (((s)))! /* trailing */;
    }
}
";
            var expected = CSharpCodeFixVerifier<NullSuppressionAnalyzer, NullSuppressionCodeFixProvider>
                .Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: 0).WithArguments("s");
            await CSharpCodeFixVerifier<NullSuppressionAnalyzer, NullSuppressionCodeFixProvider>
                .VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
