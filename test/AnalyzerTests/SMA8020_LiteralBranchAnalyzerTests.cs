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
        public async Task SMA8020_Compliant_EnumAndConstants()
        {
            var test = @"
namespace Test
{
    public enum MyEnum { Value1 = 1, Value2 = 2 }

    public class C
    {
        private const int MyConstInt = 100;
        private const string MyConstString = ""ConstText"";

        public void M(MyEnum e, int i, string s)
        {
            if (e == MyEnum.Value1)
            {
            }

            if (i == MyConstInt)
            {
            }

            if (s == MyConstString)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8020_Compliant_SwitchArmCase_EnumAndConstants()
        {
            var test = @"
namespace Test
{
    public enum MyEnum { Value1 = 1, Value2 = 2 }

    public class C
    {
        private const int MyConstInt = 100;
        private const string MyConstString = ""ConstText"";

        public void M(MyEnum e, int i, string s)
        {
            switch (e)
            {
                case MyEnum.Value1:
                    break;
            }

            switch (i)
            {
                case MyConstInt:
                    break;
            }

            switch (s)
            {
                case MyConstString:
                    break;
            }

            var res1 = e switch
            {
                MyEnum.Value1 => 1,
                _ => 2
            };

            var res2 = i switch
            {
                MyConstInt => 1,
                _ => 2
            };

            var res3 = s switch
            {
                MyConstString => 1,
                _ => 2
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
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

        [TestMethod]
        public async Task SMA8020_Violation_BinaryEquals_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some == {|#0:'a'|})
            {
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
        public async Task SMA8020_Violation_ConstantPattern_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is {|#0:'a'|})
            {
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
        public async Task SMA8020_Violation_BinaryAnd_CharLiterals()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some >= {|#0:'a'|} && some <= {|#1:'z'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "'a'"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 1).WithArguments(arguments: "'z'")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_RelationalPattern_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is >= {|#0:'a'|} and <= {|#1:'z'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "'a'"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 1).WithArguments(arguments: "'z'")
            );
        }
    }
}
