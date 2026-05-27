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
        public async Task SMA8000_Conform_AssertClassIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Assert
    {
        public static void AreEqual(int expected, int actual) {}
    }

    public class CTest
    {
        public void Test()
        {
            Assert.AreEqual(1, 2);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Conform_MustClassIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Must
    {
        public static void BeTrue(bool b) {}
        public static void Anything(int x, string s) {}
    }

    public class CTest
    {
        public void Test()
        {
            Must.BeTrue(true);
            Must.Anything(1, ""msg"");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Conform_DebugClassIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Debug
    {
        public static void Assert(bool condition) {}
        public static void Log(string msg, int level) {}
    }

    public class CTest
    {
        public void Test()
        {
            Debug.Assert(true);
            Debug.Log(""msg"", 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Violate_OtherClassesStillReported()
        {
            var test = @"
namespace Test
{
    public static class Other
    {
        public static void BeTrue(bool b) {}
    }

    public class CTest
    {
        public void Test()
        {
            Other.BeTrue({|#0:true|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA8000_Violate_AttributeNotExemptEvenIfNameMatches()
        {
            var test = @"
using System;
namespace Test
{
    public class MustAttribute : Attribute
    {
        public MustAttribute(bool b) {}
    }

    [Must({|#0:true|})]
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
