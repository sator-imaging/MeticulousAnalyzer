// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0021_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum()
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
        public async Task SMA0021_Violation_CastFromEnumVariable()
        {
            var test = @"
using System;
public class C
{
    public void M(Enum e)
    {
        var foo = {|#0:(object)e|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("Enum");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnumValue()
        {
            var test = @"
using System.Reflection;
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var foo = {|#0:(object)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnumValueToInt()
        {
            var test = @"
using System.Reflection;
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var num = {|#0:(int)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_ExplicitCast_NullConditionalEnum()
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
            var x = {|#0:(int?)foo?.EnumProp|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_ImplicitCast_ToObject()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        object obj = {|#0:ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_ImplicitCast_ToEnumBaseType()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        Enum e = {|#0:ETest.Value|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("Enum");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToSByte()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(sbyte)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToByte()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(byte)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToShort()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(short)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToUShort()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(ushort)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToUInt()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(uint)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToLong()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(long)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToULong()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(ulong)ETest.Value|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_WithExtraParentheses()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(int)((ETest.Value))|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ToAnotherEnum()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(ETest2)(ETest.Value)|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ChainedCast()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest3 { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(ETest3){|#1:(ETest2)(ETest.Value)|}|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest3");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("ETest2");
            var expected3 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ThroughEnumBase()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(ETest2){|#1:(System.Enum)(ETest.Value)|}|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("Enum");
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 1).WithArguments("Enum");
            var expected3 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0021_Violation_CastFromEnum_ThroughObject()
        {
            var test = @"
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
public class C
{
    public void M()
    {
        var x = {|#0:(ETest2){|#1:(object)(ETest.Value)|}|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 1).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0021_Violation_ImplicitCast_SwitchExpressionToObject()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
public class C
{
    public void M(ETest value)
    {
        object x = {|#0:value switch
        {
            ETest.Value => ETest2.Value,
            _ => throw new Exception(),
        }|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Violation_ImplicitCast_SwitchExpressionToObjectField()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
public class C
{
    object ObjectField;
    public void M(ETest value)
    {
        ObjectField = {|#0:value switch
        {
            ETest.Value => ETest2.Value,
            _ => throw new Exception(),
        }|};
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("ETest2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0021_Compliant_SwitchExpression_SameEnumType()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
public class C
{
    public void M(ETest value)
    {
        ETest result = value switch
        {
            ETest.Value => ETest.Value,
            _ => ETest.Value,
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0021_Compliant_SwitchExpression_ExplicitEnumVariable()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value }
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest2 { Value }
public class C
{
    public void M(ETest value)
    {
        ETest2 val = value switch
        {
            ETest.Value => ETest2.Value,
            _ => ETest2.Value,
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0021_Compliant_SwitchExpression_ArrayElementAssignment()
        {
            var test = @"
using System;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value, Other }
public class C
{
    ETest[] EnumArray = new ETest[] { ETest.Value };
    public void M(ETest value)
    {
        EnumArray[0] = value switch
        {
            ETest.Value => ETest.Value,
            _ => throw new Exception(),
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0021_Compliant_SwitchExpression_ListIndexerAssignment()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ETest { Value, Other }
public class C
{
    List<ETest> EnumList = new List<ETest> { ETest.Value };
    public void M(ETest value)
    {
        EnumList[0] = value switch
        {
            ETest.Value => ETest.Value,
            _ => throw new Exception(),
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

    }
}
