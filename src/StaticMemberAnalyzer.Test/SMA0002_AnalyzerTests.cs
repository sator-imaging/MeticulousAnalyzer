// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<FlakyInitializationAnalyzer>;

    [TestClass]
    public class SMA0002_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0002_Violate_CrossRefAcrossType()
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
    }
}
