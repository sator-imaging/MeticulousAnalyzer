// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0020_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            var x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnum_CompareToSame()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public bool Test(ETest value)
        {
            return value == ETest.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnum_CompareToSame_Nullable()
        {
            var test = @"
#nullable enable

using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public bool Test(ETest? value)
        {
            return value == ETest.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnum_ToNullable()
        {
            var test = @"
#nullable enable

using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            ETest? nullable = ETest.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnum_ToNullableArgument()
        {
            var test = @"
#nullable enable

using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        private void MethodTakesNullableEnum(ETest? value) { }

        public void Test()
        {
            MethodTakesNullableEnum(ETest.Value);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_Suppression()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Allow enum conversion
            var x1 = (ETest)1;

            // allow enum conversion
            var x2 = ETest.Value.ToString();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Violation_Suppression_WorkingOnAssignment()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            ETest x;

            // Allow enum conversion
            x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_Suppression_WorkingOnDiscardAssignment()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Allow enum conversion
            _ = (ETest)1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Violation_Suppression_WhitespaceSensitive()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            //Allow enum conversion
            var x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_Suppression_FirstCommentOnly()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Some other comment
            // Allow enum conversion
            var x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_Suppression_PrecedingLineEndComment()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void DoSomething() {}
        public void Test()
        {
            DoSomething();  // Allow enum conversion
            var x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_Suppression_PrecedingLineEndComment_WithBlankLine()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void DoSomething() {}
        public void Test()
        {
            DoSomething();  // Allow enum conversion

            var x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_Suppression_MultiComments_WithBlankLineBetween()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Some comment

            // Allow enum conversion
            var x = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_Suppression_TryParse()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Allow enum conversion
            _ = Enum.TryParse<ETest>(""Value"", out _);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_Suppression_DiscardAssignment()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Allow enum conversion
            _ = Enum.GetValues(typeof(ETest));
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_Suppression_VariableDeclaration()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Allow enum conversion
            var some = Enum.GetValues(typeof(ETest));
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnumVariable()
        {
            var test = @"
using System;
public class C
{
    public void M(Enum e)
    {
        // Allow enum conversion
        var foo = (object)e;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnumValue()
        {
            var test = @"
using System.Reflection;
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        // Allow enum conversion
        var foo = (object)ETest.Value;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastFromEnumValueToInt()
        {
            var test = @"
using System.Reflection;
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        // Allow enum conversion
        var num = (int)ETest.Value;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToNullableEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            var x = {|#0:(ETest?)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_InterpolatedString_Suppression_Working()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public ETest EnumProp { get; set; }
        public void Test(CTest foo)
        {
            // Allow enum conversion
            var x = $""{foo?.EnumProp}"";
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_FromAnotherEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest2 { Value }
    public class CTest
    {
        public void Test()
        {
            var x = {|#0:(ETest)ETest2.Value|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_Expression()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            var x = {|#0:(ETest)(-310 * -1)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_ConstValue()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            var x = {|#0:(ETest)int.MaxValue|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_NewEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            var x = {|#0:new ETest()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_TargetTypedNew()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            ETest x = {|#0:new()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_DefaultKeyword()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            ETest x = {|#0:default|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_DefaultExpression()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            ETest x = {|#0:default(ETest)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_DefaultParameterClause()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#0:default|}) => value;
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MethodArgNew()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#1:default|}) => value;

        public void Test()
        {
            M("""", {|#0:new()|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MethodArgDefault()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#1:default|}) => value;

        public void Test()
        {
            M("""", {|#0:default|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MethodArgDefaultExpression()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#1:default|}) => value;

        public void Test()
        {
            M("""", {|#0:default(ETest)|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastToEnum_MethodDefaultParamOmitted()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#0:default|}) => value;

        public void Test()
        {
            var x = M("""");
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastToEnum_MethodExplicitEnumValue()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#0:default|}) => value;

        public void Test()
        {
            var x = M("""", ETest.Value);
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastToEnum_MethodNamedArg()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#0:default|}) => value;

        public void Test()
        {
            var x = M("""", value: ETest.Value);
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastToEnum_AttributeDefaultOmitted()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }

    [AttributeUsage(AttributeTargets.All)]
    public class AttrTestAttribute : Attribute
    {
        public AttrTestAttribute(ETest value = {|#0:default|}) { }
    }

    [AttrTest]
    public class CTest { }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastToEnum_EnumArrayAccess()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test()
        {
            // Allow enum conversion
            var arr = new ETest[] { ETest.Value };
            var x = arr[0];
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Compliant_CastToEnum_SwitchExpressionSameType()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value, Other }
    public class CTest
    {
        public void Test(ETest value)
        {
            ETest result = value switch
            {
                ETest.Value => ETest.Value,
                _ => ETest.Other,
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MethodArgCastFromAnotherEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest2 { Value }
    public class CTest
    {
        static ETest M(ETest value) => value;

        public void Test()
        {
            var x = M({|#0:(ETest)ETest2.Value|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_NamedArgCastFromAnotherEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest2 { Value }
    public class CTest
    {
        static ETest M(string s, ETest value = {|#1:default|}) => value;

        public void Test()
        {
            var x = M("""", value: {|#0:(ETest)ETest2.Value|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MultiParamMethodArgNew()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, StringComparison comp = {|#3:default|}, ETest value = {|#2:default|}) => value;

        public void Test()
        {
            var x = M("""", {|#0:new()|}, {|#1:new()|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("StringComparison");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 2).WithArguments("ETest");
            var expected3 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 3).WithArguments("StringComparison");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MultiParamMethodArgDefault()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, StringComparison comp = {|#3:default|}, ETest value = {|#2:default|}) => value;

        public void Test()
        {
            var x = M("""", {|#0:default|}, {|#1:default|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("StringComparison");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 2).WithArguments("ETest");
            var expected3 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 3).WithArguments("StringComparison");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0020_Violation_CastToEnum_MultiParamMethodArgDefaultExpression()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        static ETest M(string s, StringComparison comp = {|#3:default|}, ETest value = {|#2:default|}) => value;

        public void Test()
        {
            var x = M("""", {|#0:default(StringComparison)|}, {|#1:default(ETest)|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("StringComparison");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 2).WithArguments("ETest");
            var expected3 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 3).WithArguments("StringComparison");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

    }
}
