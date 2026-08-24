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
    public class SMA8022_LiteralBranchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8022_Violation_BinaryNotEquals_StringLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(string some)
        {
            if (some != {|#0:""hello""|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchString).WithLocation(markupKey: 0).WithArguments(arguments: "\"hello\"")
            );
        }

        [TestMethod]
        public async Task SMA8022_Violation_ConstantPattern_NotString()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(string some)
        {
            if (some is not {|#0:""Text""|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchString).WithLocation(markupKey: 0).WithArguments(arguments: "\"Text\"")
            );
        }

        [TestMethod]
        public async Task SMA8022_Violation_SwitchCase_StringLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(string some)
        {
            switch (some)
            {
                case {|#0:""abc""|}:
                    break;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchString).WithLocation(markupKey: 0).WithArguments(arguments: "\"abc\"")
            );
        }

        [TestMethod]
        public async Task SMA8022_Violation_SwitchArm_StringLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public int M(string some) => some switch
        {
            {|#0:""foo""|} => 1,
            _ => 0
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchString).WithLocation(markupKey: 0).WithArguments(arguments: "\"foo\"")
            );
        }

        [TestMethod]
        public async Task SMA8022_Compliant_ConstantString()
        {
            var test = @"
namespace Test
{
    public class C
    {
        private const string Target = ""hello"";

        public void M(string some)
        {
            if (some == Target)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8022_Violation_EmptyWhyPrefix()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(string some)
        {
            if (some == {|#0:""hello""|} /* Why: */)
            {
            }

            if (some == {|#1:""world""|} /*Why: missing leading space is not allowed */)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchString).WithLocation(markupKey: 0).WithArguments(arguments: "\"hello\""),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchString).WithLocation(markupKey: 1).WithArguments(arguments: "\"world\"")
            );
        }

        [TestMethod]
        public async Task SMA8022_Compliant_TrailingTriviaComment_StringLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(string some)
        {
            if (some == ""hello"" /* Why: suppression */)
            {
            }

            if (some == ""world"" /* why: lower case suppression */)
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
