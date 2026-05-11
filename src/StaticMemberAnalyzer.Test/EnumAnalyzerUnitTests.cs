using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class EnumAnalyzerUnitTests
    {
        [TestMethod]
        public async Task TestCastToEnum()
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
        public async Task TestCastFromEnum()
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
            var x = {|#0:(int)ETest.Value|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestCastToGenericEnum()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void Test<T>() where T : Enum
        {
            var x = {|#0:(T)(object)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestCastFromGenericEnum()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void Test<T>(T value) where T : Enum
        {
            var x = (int){|#0:(object)value|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestEnumToString()
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
            var x = {|#0:ETest.Value.ToString()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumToString).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestEnumMethod_ShouldNotReportExempted()
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
            var a = Enum.GetValues(typeof(ETest));
            var b = Enum.GetNames(typeof(ETest));
            var c = Enum.GetName(typeof(ETest), 0);
            var d = Enum.IsDefined(typeof(ETest), 0);
            var e = Enum.Parse(typeof(ETest), ""Value"");
            Enum.TryParse(typeof(ETest), ""Value"", out var res);
            var f = Enum.ToObject(typeof(ETest), 0);
            var g = Enum.Format(typeof(ETest), 0, ""G"");
            var h = Enum.GetUnderlyingType(typeof(ETest));
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestEnumObfuscation()
        {
            var test = @"
namespace Test
{
    public enum {|#0:ETest|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestUnusualEnum()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:byte|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
#if STMG_ENABLE_KOTLIN_ENUM
        [TestMethod]
        public async Task TestEnumLike()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public sealed class {|#0:ETest|}
    {
        public static readonly ETest Value = new ETest();
        public static ETest[] Entries => new[] { Value };
        public ETest() {}
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumLike).WithLocation(0).WithArguments("ETest", "constructor is not 'private' or 'protected'");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
#endif

        [TestMethod]
        public async Task TestCastFromEnum_CompareToSame_IsNotReported()
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
        public async Task TestCastFromEnum_CompareToSame_Nullable_IsNotReported()
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
        public async Task TestCastFromEnum_ToNullable_IsNotReported()
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
        public async Task TestCastFromEnum_ToNullableArgument_IsNotReported()
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
        public async Task TestEnumHasFlag_IsNotReported()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test(ETest value)
        {
            var x = value.HasFlag(ETest.Value);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestSuppression()
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
        public async Task TestSuppression_NotWorkingOnAssignment()
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
        public async Task TestSuppression_NotWorkingOnDiscardAssignment()
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
            _ = {|#0:(ETest)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestSuppression_WhitespaceSensitive()
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
        public async Task TestSuppression_FirstCommentOnly()
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
        public async Task TestSuppression_PrecedingLineEndComment()
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
        public async Task TestSuppression_PrecedingLineEndComment_WithBlankLine()
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
        public async Task TestSuppression_MultiComments_WithBlankLineBetween()
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
    }
}
