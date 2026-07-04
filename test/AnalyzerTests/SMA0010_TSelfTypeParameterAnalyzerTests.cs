using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0010_TSelfTypeParameterAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0010_Violation_TSelfIsNotSelf()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public interface IValue<TSelf> { }
    public class MyValue : IValue<{|#0:object|}> { }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfInvariant).WithLocation(markupKey: 0).WithArguments("MyValue");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0010_Compliant_TSelfIsSelf()
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
        public async Task SMA0010_Violation_NestedTypeDiagnostic()
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
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfInvariant).WithLocation(markupKey: 0).WithArguments("Outer<T>.Nested");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
