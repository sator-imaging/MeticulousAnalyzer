// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.OmittableArgumentAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8004_OmittableArgumentAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8004_Violation_PositionalArgumentForOmittableParameter()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Some(int value = -1) {}

        public void Test()
        {
            Some({|#0:0|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: 0).WithArguments("value");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8004_Compliant_NamedArgumentForOmittableParameter()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Some(int value = -1) {}

        public void Test()
        {
            Some(value: 0);
            Some();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8004_Violation_VariablePassedAsPositionalArgumentToOmittableParameter()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Some(int value = -1) {}

        public void Test(int x)
        {
            Some({|#0:x|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: 0).WithArguments("value");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8004_Violation_MixedRequiredAndOptionalParameters()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(string req, int opt = 10) {}

        public void Test(string s, int i)
        {
            Foo(s, {|#0:i|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: 0).WithArguments("opt");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8004_Compliant_MixedRequiredAndOptionalParametersWithNamedArgument()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(string req, int opt = 10) {}

        public void Test(string s, int i)
        {
            Foo(s, opt: i);
            Foo(s);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8004_Violation_ConstructorOmittableParameter()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(int count = 1) {}

        public void Test(int c)
        {
            var x = new CTest({|#0:c|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: 0).WithArguments("count");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8004_Violation_ConstructorMixedRequiredAndOptionalParameters()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(string name, int count = 1, bool flag = false) {}

        public void Test()
        {
            var x = new CTest(""test"", {|#0:10|}, {|#1:true|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: 0).WithArguments("count");
            var expected1 = VerifyCS.Diagnostic(OmittableArgumentAnalyzer.RuleId_OmittableArgument).WithLocation(markupKey: 1).WithArguments("flag");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA8004_Compliant_ConstructorMixedRequiredAndOptionalParametersWithNamedArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(string name, int count = 1, bool flag = false) {}

        public void Test()
        {
            var x = new CTest(""test"", count: 10, flag: true);
            var y = new CTest(""test"");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
