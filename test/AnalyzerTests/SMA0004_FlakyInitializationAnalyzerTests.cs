// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<FlakyInitializationAnalyzer>;

    [TestClass]
    public class SMA0004_FlakyInitializationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0004_Violation_ReadingUninitializedValue()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public static int A = {|#0:B|};
        public static int {|#1:B|} = 10;
    }
}
";
            var expected0 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_WrongInit).WithLocation(markupKey: 0).WithArguments("B");
            var expected1 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_LateDeclare).WithLocation(markupKey: 1).WithArguments("A");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
