using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0012_TSelfTypeParameterAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0012_Violation_TSelfIsNotSelfOrDerived()
        {
            var test = @"
using System.Reflection;
using System;

namespace Test
{
    public interface IValue<in TSelf> { }
    public class MyValue0 { }
    public class MyValue1 : MyValue0 { }
    public class MyClass : MyValue1, IValue<{|#0:MyValue0|}> { }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfContravariant).WithLocation(markupKey: 0).WithArguments("MyClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0012_Compliant_TSelfIsSelfOrDerived()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public interface IValue<in TSelf> { }

    // Case 1: TSelf is Self. Should pass.
    public class MyClass1 : IValue<MyClass1> { }

    // Case 2: TSelf is a derived class. Should pass.
    public class MyBaseForTest : IValue<MyDerivedFromBase> { }
    public class MyDerivedFromBase : MyBaseForTest { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
