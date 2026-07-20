// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCs = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0023_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0023_Violation_CastFromGenericEnum()
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
        public async Task SMA0023_Violation_CastFromGenericEnum_NullConditional()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void Test<T>(CTest<T> foo) where T : struct, Enum
        {
            var x = {|#0:(object)foo?.EnumProp|};
        }
    }
    public class CTest<T> where T : struct, Enum { public T EnumProp { get; set; } }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastFromGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
