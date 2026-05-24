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
    public class EnumAnalyzerNewTests
    {
        [TestMethod]
        public async Task Test1()
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
        public async Task Test2()
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
        public async Task Test4()
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
        public async Task Test3()
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
        public async Task Test5()
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
        public async Task Test6()
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
    }
}
