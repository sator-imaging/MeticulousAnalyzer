using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyFix = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NamedArgumentCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA8000_NamedArgumentCodeFixProviderTests_Params
    {
        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsLiteralArguments()
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
            var fixtest = @"
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
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsWithMixedArguments()
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
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            int foo = 10;
            int bar = 20;
            Foo(values: new int[] { 1, foo, 2, bar });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsWithPrecedingNormalArgs()
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
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(string name, params int[] values) {}

        public void Test()
        {
            Foo(""test"", values: new int[] { 1, 2, 3 });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsSingleArgument()
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
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo(values: new int[] { 42 });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsConstructor()
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
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public CTest(params int[] values) {}

        public void Test()
        {
            var x = new CTest(values: new int[] { 1, 2, 3 });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsStringArguments()
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
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params string[] values) {}

        public void Test()
        {
            Foo(values: new string[] { ""a"", ""b"", ""c"" });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ParamsObjectArrayMixedTypes()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params object[] values) {}

        public void Test()
        {
            Foo({|#0:1, ""hello"", true|});
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params object[] values) {}

        public void Test()
        {
            Foo(values: new object[] { 1, ""hello"", true });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
