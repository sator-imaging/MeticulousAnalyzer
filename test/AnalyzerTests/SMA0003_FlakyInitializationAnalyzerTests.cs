// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<FlakyInitializationAnalyzer>;

    [TestClass]
    public class SMA0003_FlakyInitializationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0003_Violation_StaticMemberInAnotherFile()
        {
            var source1 = @"
namespace Test
{
    public partial class CTest
    {
        public static int A = {|#0:B|};
    }
}
";
            var source2 = @"
namespace Test
{
    public partial class CTest
    {
        public static int B = 10;
    }
}
";
            var expected = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_AnotherFile).WithLocation(markupKey: 0).WithArguments("int CTest.B");

            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);

            test.ExpectedDiagnostics.Add(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
