using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerExtraAllowedTests
    {
        [TestMethod]
        public async Task SMA8000_Conform_SystemMathAllowed()
        {
            var test = @"
using System;
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var x = Math.Min(1, 2);
            var y = Math.Abs(-1);
            var z = Math.Max(1.0, 2.0);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Conform_MathfAllowed()
        {
            var test = @"
namespace Test
{
    public class Mathf
    {
        public static float Clamp(float value, float min, float max) => 0;
    }

    public class CTest
    {
        public void Test()
        {
            var x = Mathf.Clamp(0.5f, 0, 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
