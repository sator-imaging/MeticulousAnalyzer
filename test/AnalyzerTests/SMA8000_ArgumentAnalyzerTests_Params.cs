using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA8000_ArgumentAnalyzerTests_Params
    {
        [TestMethod]
        public async Task SMA8000_Violation_ParamsLiteralArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo({|#0:1, 2, 3|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("params int[] values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_ParamsWithMixedArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            int foo = 10;
            int bar = 20;
            Foo({|#0:1, foo, 2, bar|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("params int[] values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_ParamsWithPrecedingNormalArgs()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(string name, params int[] values) {}

        public void Test()
        {
            Foo(""test"", {|#0:1, 2, 3|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("params int[] values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_ParamsSingleArgument()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo({|#0:42|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("params int[] values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ParamsAlreadyNamed()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo(values: new int[] { 1, 2, 3 });
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ParamsPassedAsArray()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            var arr = new int[] { 1, 2, 3 };
            Foo(arr);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ParamsNoArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ParamsSingleVariableArgument()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            int a = 1;
            Foo(a);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ParamsAllVariableArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}
        public void Bar(string name, params int[] values) {}

        public void Test()
        {
            int a = 1;
            int b = 2;
            int c = 3;
            Foo(a, b, c);
            Bar(""hello"", a, b, c);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Violation_ParamsStringArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params string[] values) {}

        public void Test()
        {
            Foo({|#0:""a"", ""b"", ""c""|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("params string[] values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_ParamsConstructor()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(params int[] values) {}

        public void Test()
        {
            var x = new CTest({|#0:1, 2, 3|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("params int[] values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
