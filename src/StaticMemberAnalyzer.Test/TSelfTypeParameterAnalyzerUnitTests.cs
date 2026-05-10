using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class TSelfTypeParameterAnalyzerUnitTests
    {
        [TestMethod]
        public async Task TestTSelfIsNotSelf()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public interface IValue<TSelf> { }
    public class MyValue : IValue<{|#0:object|}> { }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfInvariant).WithLocation(markupKey: 0).WithArguments("Test.MyValue");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestTSelfIsSelf()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public interface IValue<TSelf> { }
    public class MyValue : IValue<MyValue> { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestTSelfIsNotSelfOrBase()
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
        public async Task TestTSelfIsSelfOrBase()
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

        [TestMethod]
        public async Task TestTSelfIsNotSelfOrDerived()
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
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfContravariant).WithLocation(markupKey: 0).WithArguments("Test.MyClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestTSelfIsSelfOrDerived()
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

        [TestMethod]
        public async Task TestTSelfConstraintIsNotSelf()
        {
            var test = @"
namespace Test
{
    public class SomeOtherClass { }
    public class MyClass<TSelf> {|#0:where TSelf : SomeOtherClass|} { }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfPointingOther).WithLocation(markupKey: 0).WithArguments("Test.MyClass<TSelf>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestTSelfConstraintIsSelf()
        {
            var test = @"
namespace Test
{
    public class MyClass<TSelf> where TSelf : MyClass<TSelf> { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNestedTypeDiagnostic()
        {
            var test = @"
namespace Test
{
    public interface IValue<TSelf> { }

    public class Outer<T>
    {
        public class Nested : IValue<{|#0:Outer<T>|}> { }
    }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfInvariant).WithLocation(markupKey: 0).WithArguments("Test.Outer<T>.Nested");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TSelfIsNotMisreported_SameLengthIdentifier()
        {
            var test = @"
using System.Threading.Tasks;
namespace Test
{
    public class Foo<TTask> where TTask : Task { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
