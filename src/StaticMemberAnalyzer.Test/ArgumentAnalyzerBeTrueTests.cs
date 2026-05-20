using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerBeTrueTests
    {
        [TestMethod]
        public async Task TestMustBeTrueBeFalse()
        {
            var test = @"
namespace Test
{
    public static class Must
    {
        public static void BeTrue(bool b) {}
        public static void BeFalse(bool b) {}
    }

    public class CTest
    {
        public void Test()
        {
            Must.BeTrue(true);
            Must.BeFalse(false);
        }
    }
}
";
            // Currently this should fail if the analyzer reports it.
            // We expect NO diagnostics after the fix.
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMustBeTrueBeFalseMixed()
        {
            var test = @"
namespace Test
{
    public static class Must
    {
        public static void BeTrue(bool b, string msg) {}
    }

    public class CTest
    {
        public void Test()
        {
            Must.BeTrue(true, {|#0:""msg""|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("msg");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }
    }
}
