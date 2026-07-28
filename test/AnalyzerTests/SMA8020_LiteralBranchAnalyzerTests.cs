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
    public class SMA8020_LiteralBranchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8020_Violation_BinaryEquals_IntegerLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            if (some == {|#0:5|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "5")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_BinaryNotEquals_StringLiteral()
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
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "\"hello\"")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_SwitchCase_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            switch (some)
            {
                case {|#0:'a'|}:
                    break;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "'a'")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_SwitchArm_FloatLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(float some) => some switch
        {
            {|#0:1.5f|} => ""one point five"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "1.5f")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_MinusOneLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            if (some == {|#0:-1|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "-1")
            );
        }

        [TestMethod]
        public async Task SMA8020_Compliant_BinaryEquals_BooleanLiteral()
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

        [TestMethod]
        public async Task SMA8020_Compliant_BinaryEquals_NullLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(object some)
        {
            if (some == null)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8020_Violation_ConstantPattern_NotString()
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
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "\"Text\"")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_ConstantPattern_IsNumber()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            if (some is {|#0:100|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "100")
            );
        }

        [TestMethod]
        public async Task SMA8020_Compliant_VariableInitializer_IntegerLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            int x = 5;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
