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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestKnownTestFrameworkIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Must
    {
        public static void BeTrue(bool b, string msg, int code) {}
    }

    public class CTest
    {
        public void Test()
        {
            Must.BeTrue(true, ""msg"", 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestExactMatchRequired()
        {
            var test = @"
namespace Test
{
    public static class Must
    {
        public static void BeTrue(bool b) {}
        public static void MyBeTrue(bool b) {}
    }

    public class CTest
    {
        public void Test()
        {
            Must.BeTrue(true);
            Must.MyBeTrue({|#0:true|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task TestAttributeNotExemptEvenIfNameMatches()
        {
            var test = @"
using System;
namespace Test
{
    public class BeTrueAttribute : Attribute
    {
        public BeTrueAttribute(bool b) {}
    }

    [BeTrue({|#0:true|})]
    public class CTest
    {
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }
    }
}
