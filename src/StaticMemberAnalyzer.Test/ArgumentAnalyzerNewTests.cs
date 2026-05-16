using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerNewTests
    {
        [TestMethod]
        public async Task TestBooleanBinaryOperationAtPositionZeroIsOmittable()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x, int y)
        {
            // Position 0 is omittable for boolean binary expressions according to current IsOmittableType logic.
            Foo(x == y);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestBooleanBinaryOperationNotFirstArgumentDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int i, bool b) {}
        public void Test(int x, int y)
        {
            Foo(1, {|#0:x == y|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
