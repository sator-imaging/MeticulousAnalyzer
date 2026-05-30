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
    public class SMA0027_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum()
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

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_Short()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:short|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_UShort()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:ushort|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_SByte()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:sbyte|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_Long()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:long|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_ULong()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:ulong|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_UInt()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:uint|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_ExplicitValue_WithoutFlags()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : int { Value {|#0:= 0|}, Other {|#1:= 1|} }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0027_Compliant_UnusualEnum_ExplicitValue_WithFlags()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    [Flags]
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { None = 0, Value = 1 }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0027_Compliant_UnusualEnum_IntBaseType()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : int { Value }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_ExplicitLargeValue_WithoutFlags()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:ulong|} { Value {|#1:= 10|}, Other {|#2:= 20|} }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 1);
            var expected2 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 2);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_TypeAlias()
        {
            var test = @"
using System.Reflection;
using ShortAlias = System.Int16;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : {|#0:ShortAlias|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0027_Violation_UnusualEnum_EnumMemberAttribute_ExplicitValue()
        {
            var test = @"
using System.Reflection;
using System.Runtime.Serialization;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest : int
    {
        Value {|#0:= 0|},

        [EnumMember(Value = ""Name"")]
        Other {|#1:= 1|},
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 0);
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_UnusualEnum).WithLocation(markupKey: 1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

    }
}
