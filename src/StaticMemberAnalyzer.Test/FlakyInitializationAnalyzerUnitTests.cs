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
    public class FlakyInitializationAnalyzerUnitTests
    {
        [TestMethod]
        public async Task SMA0001_SMA0004_ReadingUninitializedValue()
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

        [TestMethod]
        public async Task SMA0002_CrossRefAcrossType()
        {
            var test = @"
namespace Test
{
    public class C1
    {
        public static int A = {|#0:C2.B|};
    }
    public class C2
    {
        public static int B = {|#1:C1.A|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_CrossRef).WithLocation(markupKey: 0).WithArguments("C2", "C1");
            var expected1 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_CrossRef).WithLocation(markupKey: 1).WithArguments("C1", "C2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0003_StaticMemberInAnotherFile()
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
            var expected = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_AnotherFile).WithLocation(markupKey: 0).WithArguments("B");

            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);

            test.ExpectedDiagnostics.Add(expected);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task NoDiagnosticWhenOrderIsCorrect()
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
        public async Task NoDiagnosticForConst()
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
        public async Task SMA0001_PropertyInitialization()
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
