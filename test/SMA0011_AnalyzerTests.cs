using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0011_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0011_Violation_TSelfIsNotSelfOrBase()
        {
            var test = @"
using System.Reflection;
using System;

namespace Test
{
    public interface IValue<out TSelf> { }
    public class MyValue0 { }
    public class MyValueOther { }
    public class MyValue1 : MyValue0, IValue<{|#0:MyValueOther|}> { }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfCovariant).WithLocation(markupKey: 0).WithArguments("Test.MyValue1");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0011_Compliant_TSelfIsSelfOrBase()
        {
            var test = @"
using System.Reflection;
using System;

namespace Test
{
    public interface IValue<out TSelf> { }
    public class MyValue0 { }
    public class MyValue1 : MyValue0, IValue<MyValue0> { }
    public class MyValue2 : MyValue1, IValue<MyValue1> { }
    public class MyValue3 : MyValue2, IValue<MyValue0> { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
