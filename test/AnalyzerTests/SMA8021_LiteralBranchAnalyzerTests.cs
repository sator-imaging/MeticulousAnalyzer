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
        public async Task SMA8021_Violation_SwitchStatement_RelationalPatternZero()
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
                case > {|#0:0|}:
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
        public async Task SMA8021_Violation_SwitchStatement_PropertyPatternZero()
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
                case { Age: {|#0:0|} }:
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
        public async Task SMA8021_Violation_SwitchStatement_WhenClauseZero()
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
                case string s when count == {|#0:0|}:
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
        public async Task SMA8021_Violation_SwitchExpression_RelationalPatternZero()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(int value) => value switch
        {
            > {|#0:0|} => ""positive"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_SwitchExpression_PropertyPatternZero()
        {
            var test = @"
namespace Test
{
    public class Person { public int Age { get; set; } }

    public class C
    {
        public string M(Person person) => person switch
        {
            { Age: {|#0:0|} } => ""baby"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_SwitchExpression_PositionalPatternZero()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M((int X, int Y) point) => point switch
        {
            ({|#0:0|}, {|#1:0|}) => ""origin"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 1).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_SwitchExpression_WhenClauseZero()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(object obj, int count) => obj switch
        {
            string s when count == {|#0:0|} => ""zero"",
            _ => ""other""
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0")
            );
        }


        [TestMethod]
        public async Task SMA8021_Violation_ForStatement_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            for (int i = 10; i > {|#0:0|}; i--)
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
        public async Task SMA8021_Violation_TernaryCondition_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public int M(int some)
        {
            return some == {|#0:0|} ? 1 : 0;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_CannotSuppressByOtherComments()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int some)
        {
            if (some == /* cannot suppress by leading comment */ {|#0:0|}) /* cannot suppress by outside comment */
            {
            }

            if (some == {|#1:0|} /**/)
            {
            }

            if (some == {|#2:0|} /* missing why prefix */)
            {
            }

            if (some == {|#3:0|} /* Why: */)
            {
            }

            if (some == {|#4:0|} /*Why: missing leading space is not allowed */)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 1).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 2).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 3).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 4).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Compliant_TrailingTriviaComment_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public string M(int some)
        {
            if (some == 0 /* Why: this is suppression comment */)
            {
            }
            else if (some == 00 /* why: lower case suppression */)
            {
            }
            else if (some < 0 /* Why: suppression for else-if */)
            {
            }

            while (some is > 0 /* Why: suppression */) { }

            do { }
            while (some > 0 /* Why: suppression */);

            for (int i = 0; i < 0 /* Why: suppression */; i++) { }

            var ternary = some < 0 /* Why: suppression */ ? ""Foo"" : ""Bar"";

            switch (some)
            {
                case 0 /* Why: suppression */:
                    break;
            }

            return some switch
            {
                0 /* Why: suppression */ => ""Ok"",
                _ => ""Default""
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Compliant_MethodInvocation_IndexOf()
        {
            var test = @"
namespace Test
{
    public class Custom
    {
        public int IndexOfSomething() => 0;
        public int LastIndexOfSomething() => 0;
    }

    public class C
    {
        public void M(string text, Custom custom)
        {
            if (text.IndexOf(""a"") == 0) { }
            if (text.LastIndexOf(""b"") >= 0) { }
            if (0 == custom.IndexOfSomething()) { }
            if (0 < custom.LastIndexOfSomething()) { }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Compliant_PropertyAccess_LengthAndCount()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class Custom
    {
        public int Length { get; set; }
        public int Count => 0;
    }

    public class C
    {
        public void M(string text, List<int> list, Custom custom)
        {
            if (text.Length == 0) { }
            if (0 == list.Count) { }
            if (custom.Length > 0) { }
            if (0 < custom.Count) { }

            switch (text.Length)
            {
                case 0:
                    break;
            }

            var res = list.Count switch
            {
                0 => ""empty"",
                _ => ""items""
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Compliant_LocalVariable_ExemptedMethodOrProperty()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class C
    {
        public void M(string text, List<int> list)
        {
            int idx = text.IndexOf(""a"");
            if (idx == 0) { }

            int len;
            len = text.Length;
            if (0 == len) { }

            int cnt = list.Count;
            switch (cnt)
            {
                case 0:
                    break;
            }

            int lastIdx;
            lastIdx = text.LastIndexOf(""b"");
            var res = lastIdx switch
            {
                0 => ""first"",
                _ => ""other""
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Violation_MethodInvocation_NonExemptName()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public int FindIndex() => 0;
        public int indexOf() => 0;

        public void M()
        {
            if (FindIndex() == {|#0:0|}) { }
            if (indexOf() == {|#1:0|}) { }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 1).WithArguments(arguments: "0")
            );
        }

        [TestMethod]
        public async Task SMA8021_Violation_PropertyAccess_NonExemptName()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public int Size => 0;
        public int length => 0;

        public void M()
        {
            if (Size == {|#0:0|}) { }
            if (length == {|#1:0|}) { }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 1).WithArguments(arguments: "0")
            );
        }
    }
}
