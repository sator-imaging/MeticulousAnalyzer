using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0015_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0015_Violate_TSelfConstraintIsNotSelf()
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
        public async Task SMA0010_Conform_TSelfConstraintIsSelf()
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
        public async Task SMA0010_Conform_TSelfIsNotMisreported_SameLengthIdentifier()
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
