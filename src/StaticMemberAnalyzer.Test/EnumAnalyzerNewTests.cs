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
        public async Task TestSystemEnumCast_Reported()
        {
            var test = @"
using System;

namespace Test
{
    public class C
    {
        public void M(Enum e)
        {
            var foo = {|#0:(object)e|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("Enum");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestSystemEnumCast_Suppressed()
        {
            var test = @"
using System;

namespace Test
{
    public class C
    {
        public void M(Enum e)
        {
            // Allow enum conversion
            var foo = (object)e;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestUserRequestedCase_Suppressed()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum Enum { Value }
    public class C
    {
        public void M()
        {
            // Allow enum conversion
            var foo = (object)Enum.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestUserRequestedCase_Reported()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum Enum { Value }
    public class C
    {
        public void M()
        {
            var foo = {|#0:(object)Enum.Value|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("Enum");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestExplicitIntCast_Reported()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E { Value }
    public class C
    {
        public void M()
        {
            var num = {|#0:(int)E.Value|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 0).WithArguments("E");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestExplicitIntCast_Suppressed()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E { Value }
    public class C
    {
        public void M()
        {
            // Allow enum conversion
            var num = (int)E.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestImplicitArgumentConversion_Reported()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum E { Value }
    public class C
    {
        public void M1(E e) {}
        public void M2(object o) {}

        public void Test()
        {
            M1({|#0:0|});        // Implicit conversion to E
            M2({|#1:E.Value|});  // Implicit conversion to object
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToEnum).WithLocation(markupKey: 0).WithArguments("E");
            var expected1 = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromEnum).WithLocation(markupKey: 1).WithArguments("E");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
