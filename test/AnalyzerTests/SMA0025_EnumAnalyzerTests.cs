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
    public class SMA0025_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0025_Violation_EnumMethod()
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
            var x = {|#0:Enum.GetValues(typeof(ETest))|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_EnumHasFlag()
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
            var x = {|#0:value.HasFlag(ETest.Value)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_Suppression_Defined()
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
            if ({|#0:Enum.IsDefined(typeof(ETest), 0)|}) { }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_Suppression_GetValues()
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
            foreach (var v in {|#0:Enum.GetValues(typeof(ETest))|}) { }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_Suppression_NestedInvocation()
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
        public void Test(bool some)
        {
            // Allow enum conversion
            if (some && {|#0:Enum.IsDefined(typeof(ETest), 0)|}) { }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_Suppression_LinqInvocation()
        {
            var test = @"
using System;
using System.Linq;
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
    public class CTest
    {
        public void Test(int[] some)
        {
            // Allow enum conversion
            foreach (var v in some.Where(x => {|#0:Enum.IsDefined(typeof(ETest), x)|})) { }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_EnumHasFlag_NullConditional()
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
            var x = {|#0:foo?.EnumProp.HasFlag(ETest.Value)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_EnumToObject()
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
            var x = {|#0:Enum.ToObject(typeof(ETest), 0)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_EnumTryParse()
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
            var x = {|#0:Enum.TryParse<ETest>("""", out _)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0025_Violation_EnumEquals_GenericEnum()
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
            var x = {|#0:value.Equals(null)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumMethod).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0024_Violation_EnumGetName()
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
            var x = {|#0:Enum.GetName(typeof(ETest), ETest.Value)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumToString).WithLocation(markupKey: 0).WithArguments("Enum");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
