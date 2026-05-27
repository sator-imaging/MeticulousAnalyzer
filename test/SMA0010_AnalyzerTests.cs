using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0010_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0010_Violate_TSelfIsNotSelf()
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
        public async Task SMA0010_Conform_TSelfIsSelf()
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
        public async Task SMA0010_Violate_NestedTypeDiagnostic()
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
    }
}
