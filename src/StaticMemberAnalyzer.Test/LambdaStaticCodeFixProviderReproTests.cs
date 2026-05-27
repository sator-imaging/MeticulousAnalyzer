using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.LambdaAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.LambdaStaticCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class LambdaStaticCodeFixProviderReproTests
    {
        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodWithKeywordArgsConversionCodeFix_ReproIssue1()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod(int @class) { }
    void M()
    {
        Action<int> a = {|#0:StaticMethod|};
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod(int @class) { }
    void M()
    {
        Action<int> a = static (@class) => StaticMethod(@class);
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<int>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_WrapWithStaticLambdaPreservesTrivia_ReproIssue2()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod() { }
    void M()
    {
        Action a = /* leading */ {|#0:StaticMethod|} /* trailing */;
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod() { }
    void M()
    {
        Action a = /* leading */ static () => StaticMethod() /* trailing */;
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

    }
}
