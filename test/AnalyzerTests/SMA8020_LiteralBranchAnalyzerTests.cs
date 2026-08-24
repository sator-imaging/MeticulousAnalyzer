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
        public async Task SMA8020_Violation_SwitchStatement_RelationalPattern()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int value)
        {
            switch (value)
            {
                case > {|#0:100|}:
                    break;
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
        public async Task SMA8020_Violation_SwitchStatement_PropertyPattern()
        {
            var test = @"
namespace Test
{
    public class Person { public int Age { get; set; } }

    public class C
    {
        public void M(Person person)
        {
            switch (person)
            {
                case { Age: {|#0:18|} }:
                    break;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "18")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_SwitchStatement_WhenClause()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(object obj, int count)
        {
            switch (obj)
            {
                case string s when count == {|#0:5|}:
                    break;
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
        public async Task SMA8020_Violation_SwitchExpression_RelationalPattern()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(int value) => value switch
        {
            > {|#0:100|} => ""large"",
            _ => ""normal""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "100")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_SwitchExpression_PropertyPattern()
        {
            var test = @"
namespace Test
{
    public class Person { public int Age { get; set; } }

    public class C
    {
        public string M(Person person) => person switch
        {
            { Age: {|#0:18|} } => ""adult"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "18")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_SwitchExpression_PositionalPattern()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M((int X, int Y) point) => point switch
        {
            ({|#0:10|}, {|#1:20|}) => ""target"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "10"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 1).WithArguments(arguments: "20")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_SwitchExpression_WhenClause()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(object obj, int count) => obj switch
        {
            string s when count == {|#0:5|} => ""five"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "5")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_ForStatement_IntegerLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            for (int i = 0; i < {|#0:10|}; i++)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "10")
            );
        }

        [TestMethod]
        public async Task SMA8020_Violation_TernaryCondition_IntegerLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public int M(int some)
        {
            return some == {|#0:5|} ? 1 : 0;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "5")
            );
        }

        [TestMethod]
        public async Task SMA8020_Compliant_ForStatement_Constant()
        {
            var test = @"
namespace Test
{
    public class C
    {
        private const int MaxCount = 10;

        public void M()
        {
            for (int i = 0; i < MaxCount; i++)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8020_Compliant_TernaryCondition_Constant()
        {
            var test = @"
namespace Test
{
    public class C
    {
        private const int Target = 5;

        public int M(int some)
        {
            return some == Target ? 1 : 0;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8020_Violation_CannotSuppressByOtherComments()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            if (some == /* cannot suppress by leading comment */ {|#0:42|}) /* cannot suppress by outside comment */
            {
            }

            if (some == {|#1:42|} /**/)
            {
            }

            if (some == {|#2:42|} /* missing why prefix */)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 0).WithArguments(arguments: "42"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 1).WithArguments(arguments: "42"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranch).WithLocation(markupKey: 2).WithArguments(arguments: "42")
            );
        }

        [TestMethod]
        public async Task SMA8020_Compliant_TrailingTriviaComment_Constructs()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(int some)
        {
            if (some == 5 /* Why: this is suppression comment */)
            {
            }
            else if (some < -1 /* Why: suppression for else-if */)
            {
            }

            while (some is > 5 /* Why: suppression */) { }

            do { }
            while (some > 5 /* Why: suppression */);

            for (int i = 0; i < 5 /* Why: suppression */; i++) { }

            var ternary = some < 5 /* Why: suppression */ ? ""Foo"" : ""Bar"";

            switch (some)
            {
                case 5 /* Why: suppression */:
                    break;
            }

            return some switch
            {
                5 /* Why: suppression */ => ""Ok"",
                _ => ""Default""
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
