// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.LiteralBranchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8021_LiteralBranchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8021_Violation_BinaryEquals_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            if (some == {|#0:0|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_SwitchCase_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            switch (some)
            {
                case {|#0:0|}:
                    break;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_SwitchArm_ZeroFloatLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(float some) => some switch
        {
            {|#0:0.0f|} => ""zero"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0.0f")
            );
        }

        [TestMethod]
        public async Task SMA8021_Compliant_BinaryEquals_BooleanLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(bool? some)
        {
            if (some == true)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
