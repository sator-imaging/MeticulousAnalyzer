// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.TSelfTypeParameterAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0015_TSelfTypeParameterAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0015_Violation_TSelfConstraintIsNotSelf()
        {
            var test = @"
namespace Test
{
    public class SomeOtherClass { }
    public class MyClass<TSelf> {|#0:where TSelf : SomeOtherClass|} { }
}
";
            var expected = VerifyCS.Diagnostic(TSelfTypeParameterAnalyzer.RuleId_TSelfPointingOther).WithLocation(markupKey: 0).WithArguments("MyClass<TSelf>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0015_Compliant_TSelfConstraintIsSelf()
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
        public async Task SMA0015_Compliant_TSelfIsNotMis_SameLengthIdentifier()
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
