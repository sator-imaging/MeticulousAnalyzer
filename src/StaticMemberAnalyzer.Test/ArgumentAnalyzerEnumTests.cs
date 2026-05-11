using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerEnumTests
    {
        [TestMethod]
        public async Task TestRegularMethod_ShouldReport()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int i) {}
        public void Test()
        {
            Foo({|#0:1|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("i");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestEnumMethods_ShouldNotReport()
        {
            var test = @"
using System;
namespace Test
{
    public enum ETest { Value = 0 }
    public class CTest
    {
        public void Test()
        {
            Enum.TryParse(typeof(ETest), ""Value"", out var result);
            Enum.TryParse<ETest>(""Value"", out var result2);
            Enum.IsDefined(typeof(ETest), ""Value"");
            Enum.IsDefined(typeof(ETest), 0);
            Enum.Parse(typeof(ETest), ""Value"");
            Enum.GetName(typeof(ETest), 0);
            Enum.ToObject(typeof(ETest), 0);
            Enum.Format(typeof(ETest), 0, ""G"");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestSuppression()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int i) {}
        public void Test()
        {
            // Allow enum conversion
            Foo(1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
