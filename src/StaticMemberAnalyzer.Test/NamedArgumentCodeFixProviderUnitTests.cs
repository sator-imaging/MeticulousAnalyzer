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
        public int Foo(int index, bool strict, string message) => 0;

        public void Test()
        {
            var x = Foo({|#0:1|}, {|#1:true|}, {|#2:""message""|});
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public int Foo(int index, bool strict, string message) => 0;

        public void Test()
        {
            var x = Foo(index: 1, strict: true, message: ""message"");
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
        public int Foo(int index, bool strict, string message) => 0;

        public void Test()
        {
            var x = Foo({|#0:0|},
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
        public int Foo(int index, bool strict, string message) => 0;

        public void Test()
        {
            var x = Foo(index: 0,
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

        [TestMethod]
        public async Task TestNestedAndComplexSyntax()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        int Foo(int a, float b) => 0;
        float Bar(float x, float y) => 0;
        float Baz(string s, float f) => 0;

        public void Test()
        {
            var x = Foo({|#0:0|}, Bar(Baz({|#1:""message""|}, {|#2:2.2f|}), {|#3:11f|}));

            var y = Foo({|#4:0|},
                Bar(Baz({|#5:""message""|},
                    {|#6:2.2f|}),
                {|#7:11f|}));
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        int Foo(int a, float b) => 0;
        float Bar(float x, float y) => 0;
        float Baz(string s, float f) => 0;

        public void Test()
        {
            var x = Foo(a: 0, Bar(Baz(s: ""message"", f: 2.2f), y: 11f));

            var y = Foo(a: 0,
                Bar(Baz(s: ""message"",
                    f: 2.2f),
                y: 11f));
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("a");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("s");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("f");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("y");
            var expected4 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 4).WithArguments("a");
            var expected5 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("s");
            var expected6 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 6).WithArguments("f");
            var expected7 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 7).WithArguments("y");
            await VerifyCS.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7 }, fixtest);
        }
    }
}
