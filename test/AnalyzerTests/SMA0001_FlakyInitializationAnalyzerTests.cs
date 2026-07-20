// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCs = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<FlakyInitializationAnalyzer>;

    [TestClass]
    public class SMA0001_FlakyInitializationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0001_Compliant_OrderIsCorrect()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public static int B = 10;
        public static int A = B;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0001_Compliant_Const()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public static int A = B;
        public const int B = 10;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0001_Violation_PropertyInitialization()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public static int A { get; } = {|#0:B|};
        public static int {|#1:B|} { get; } = 10;
    }
}
";
            var expected0 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_WrongInit).WithLocation(markupKey: 0).WithArguments("B");
            var expected1 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_LateDeclare).WithLocation(markupKey: 1).WithArguments("A");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
