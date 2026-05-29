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
    public class SMA0022_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum()
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
        public async Task SMA0022_Violation_CastToGenericEnum_NewTEnum()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void M<T>() where T : Enum, new()
        {
            var x = {|#0:new T()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_TargetTypedNew()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void M<T>() where T : Enum, new()
        {
            T x = {|#0:new()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_DefaultKeyword()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void M<T>() where T : Enum
        {
            T x = {|#0:default|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_DefaultExpression()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void M<T>() where T : Enum
        {
            T x = {|#0:default(T)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_CastExpression()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void M<T>() where T : Enum
        {
            var x = {|#0:(T)(object)(310 + 310)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_CastFromValue()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void M<T>(T value) where T : Enum
        {
            var x = {|#0:(T)(object)value|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_DefaultParameterClause()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        static T GetEnum<T>(string s, T value = {|#0:default|}) where T : Enum => value;
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_MethodArgNew()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum, new() => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test<T>() where T : Enum, new()
        {
            GetEnum<T>("""", {|#0:new()|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_MethodArgDefault()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test<T>() where T : Enum
        {
            GetEnum<T>("""", {|#0:default|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_MethodArgDefaultExpression()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test<T>() where T : Enum
        {
            GetEnum<T>("""", {|#0:default(T)|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_ConcreteTypeArgNew()
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
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum, new() => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test()
        {
            GetEnum<ETest>("""", {|#0:new()|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_ConcreteTypeArgDefault()
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
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test()
        {
            GetEnum<ETest>("""", {|#0:default|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum_ConcreteTypeArgDefaultExpression()
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
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test()
        {
            GetEnum<ETest>("""", {|#0:default(ETest)|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0022_Compliant_CastToGenericEnum_MethodDefaultParamOmitted()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test<T>() where T : Enum
        {
            var x = GetEnum<T>("""");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0022_Compliant_CastToGenericEnum_ConcreteTypeDefaultParamOmitted()
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
#pragma warning disable SMA0020, SMA0022, SMA0023
        static T GetEnum<T>(string s, T value = default) where T : Enum => value;
#pragma warning restore SMA0020, SMA0022, SMA0023

        public void Test()
        {
            var x = GetEnum<ETest>("""");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

    }
}
