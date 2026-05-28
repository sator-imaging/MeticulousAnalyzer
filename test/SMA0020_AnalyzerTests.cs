// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0020_AnalyzerTests
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

    }
}
