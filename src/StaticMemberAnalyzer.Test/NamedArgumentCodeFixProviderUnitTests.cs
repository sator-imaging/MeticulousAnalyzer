using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NamedArgumentCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class NamedArgumentCodeFixProviderUnitTests
    {
        [TestMethod]
        public async Task TestMethodLiteralArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index, bool strict, string message) {}

        public void Test()
        {
            Foo({|#0:1|}, {|#1:true|}, {|#2:""message""|});
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index, bool strict, string message) {}

        public void Test()
        {
            Foo(index: 1, strict: true, message: ""message"");
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyCS.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2 }, fixtest);
        }

        [TestMethod]
        public async Task TestConstructorLiteralArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(int index, bool strict, string message) {}

        public void Test()
        {
            var x = new CTest({|#0:1|}, {|#1:true|}, {|#2:""message""|});
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public CTest(int index, bool strict, string message) {}

        public void Test()
        {
            var x = new CTest(index: 1, strict: true, message: ""message"");
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyCS.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2 }, fixtest);
        }

        [TestMethod]
        public async Task TestAttributeArguments()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(int index) {}
        public string Name { get; set; }
    }

    [My({|#0:1|}, Name = ""test"")]
    public class CTest
    {
    }
}
";
            var fixtest = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(int index) {}
        public string Name { get; set; }
    }

    [My(index: 1, Name = ""test"")]
    public class CTest
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestComplexSyntax()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index, bool strict, string message) {}

        public void Test()
        {
            Foo({|#0:0|},
                {|#1:false|},
                {|#2:""bar""|}
            );
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index, bool strict, string message) {}

        public void Test()
        {
            Foo(index: 0,
                strict: false,
                message: ""bar""
            );
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyCS.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2 }, fixtest);
        }
    }
}
