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

        [TestMethod]
        public async Task SMA8021_Compliant_EnumAndConstants()
        {
            var test = @"
namespace Test
{
    public enum MyEnum { Value1 = 0, Value2 = 1 }

    public class C
    {
        private const int MyConstInt = 0;

        public void M(MyEnum e, int i)
        {
            if (e == MyEnum.Value1)
            {
            }

            if (i == MyConstInt)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Compliant_SwitchArmCase_EnumAndConstants()
        {
            var test = @"
namespace Test
{
    public enum MyEnum { Value1 = 0, Value2 = 1 }

    public class C
    {
        private const int MyConstInt = 0;

        public void M(MyEnum e, int i)
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
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Violation_BinaryEquals_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some == {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_ConstantPattern_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_RelationalPattern_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is > {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }
    }
}
