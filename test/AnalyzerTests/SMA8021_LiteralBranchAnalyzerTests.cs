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
        public async Task SMA8021_Compliant_LoopConditions_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(int length)
        {
            for (int i = length - 1; i >= 0; i--)
            {
            }

            while (length > 0)
            {
            }

            do
            {
            } while (length > 0);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
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
        public async Task SMA8021_Compliant_MemberAccess_CountLengthIndexOf()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class Container
    {
        public int GetCount() => 0;
        public int GetLength() => 0;
    }

    public class C
    {
        public void M(List<int> list, string str, int[] arr, Container container)
        {
            if (list.Count > 0)
            {
            }

            if (arr.Length == 0)
            {
            }

            if (container.GetCount() < 0)
            {
            }

            if (container.GetLength() <= 0)
            {
            }

            if (str.IndexOf('a') >= 0)
            {
            }

            if (str.LastIndexOf('z') >= 0)
            {
            }

            int pos;
            if ((pos = str.IndexOf('a')) > 0)
            {
            }

            int count;
            if ((count = list.Count) == 0)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Violation_LocalReference_NoMemberAccess()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(string some)
        {
            int pos = some.IndexOf('a');
            if (pos < {|#0:0|})
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
        public async Task SMA8021_Compliant_PatternMatch_LengthCountIndexOf()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class Container
    {
        public int? Length { get; set; }
    }

    public class C
    {
        public void M(int[] arr, List<int> list, string str, Container container)
        {
            if (arr?.Length is null or 0)
            {
            }

            if (arr.Length is >= 0)
            {
            }

            if (arr.Length is 0)
            {
            }

            if (list.Count is not 0)
            {
            }

            if (str.IndexOf('a') is >= 0)
            {
            }

            if (container is { Length: 0 })
            {
            }

            if (container is { Length: null or 0 })
            {
            }

            if (container is { Length: >= 0 })
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8021_Violation_PatternMatch_NoMatchingMember()
        {
            var test = @"
namespace Test
{
    public class Person
    {
        public int Age { get; set; }
    }

    public class C
    {
        public bool M(int? val, Person person)
        {
            if (val is null or {|#0:0|})
            {
            }

            if (person is { Age: {|#1:0|} })
            {
            }

            var res = val switch
            {
                null or {|#2:0|} => true,
                _ => false
            };

            return res;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 0).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 1).WithArguments(arguments: "0"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchZero).WithLocation(markupKey: 2).WithArguments(arguments: "0")
            );
        }
        [TestMethod]
        public async Task SMA8021_Violation_PropertySubpattern_ZeroLiteral()
        {
            var test = @"
namespace Test
{
    public class Target
    {
        public int Value { get; set; }
    }

    public class C
    {
        public void M(Target some)
        {
            if (some is { Value: {|#0:0|} })
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
        public async Task SMA8021_Compliant_PropertySubpattern_ZeroLiteralWithMatchingMemberOrSuppression()
        {
            var test = @"
namespace Test
{
    public class Target
    {
        public int Length { get; set; }
        public int Value { get; set; }
    }

    public class C
    {
        private const int ZeroConst = 0;

        public void M(Target some)
        {
            if (some is { Length: 0 })
            {
            }

            if (some is { Value: ZeroConst })
            {
            }

            if (some is { Value: 0 /* Why: test */ })
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
