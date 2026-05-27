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
    public class SMA0021_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0021_Violate_CastFromEnum()
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
        public async Task SMA0021_Violate_CastFromEnumVariable()
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
        public async Task SMA0021_Violate_CastFromEnumValue()
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
        public async Task SMA0021_Violate_CastFromEnumValueToInt()
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
        public async Task SMA0021_Violate_ExplicitCast_NullConditionalEnum()
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

    }
}
