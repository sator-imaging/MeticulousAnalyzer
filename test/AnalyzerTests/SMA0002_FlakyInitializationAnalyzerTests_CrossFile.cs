// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = global::MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<FlakyInitializationAnalyzer>;

    [TestClass]
    public class SMA0002_FlakyInitializationAnalyzerTests_CrossFile
    {
        [TestMethod]
        public async Task SMA0002_Violation_CrossFile_NonPartialClassCrossRef()
        {
            var source1 = @"
namespace Test
{
    public class CrazySource
    {
        public static float Value = {|#0:CrazyDestination.Value|};
    }
}
";
            var source2 = @"
namespace Test
{
    public class CrazyDestination
    {
        public static float Value = {|#1:CrazySource.Value|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_CrossRef).WithLocation(markupKey: 0).WithArguments("CrazyDestination", "CrazySource");
            var expected1 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_CrossRef).WithLocation(markupKey: 1).WithArguments("CrazySource", "CrazyDestination");

            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            test.ExpectedDiagnostics.Add(expected0);
            test.ExpectedDiagnostics.Add(expected1);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0002_Violation_CrossFile_ReadingFieldWithMutualReference()
        {
            var source1 = @"
namespace Test
{
    public class OtherClass
    {
        public readonly static double D = 3.10 + {|#0:InterFileRef.PublicDouble|};
    }
}
";
            var source2 = @"
namespace Test
{
    public class InterFileRef
    {
        public readonly static double CrossRef = 10 + {|#1:OtherClass.D|} + 20;
        public readonly static double PublicDouble = 99.99;
    }
}
";
            var expected0 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_CrossRef).WithLocation(markupKey: 0).WithArguments("InterFileRef", "OtherClass");
            var expected1 = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_CrossRef).WithLocation(markupKey: 1).WithArguments("OtherClass", "InterFileRef");

            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            test.ExpectedDiagnostics.Add(expected0);
            test.ExpectedDiagnostics.Add(expected1);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
