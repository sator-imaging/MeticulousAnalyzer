using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyFix = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NamedArgumentCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8000_NamedArgumentCodeFixProviderTests
    {
        [TestMethod]
        public async Task SMA8000_CodeFix_BooleanExpression()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x, int y)
        {
            Foo([|x == y|]);
        }
    }
}
";
            var fixedCode = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x, int y)
        {
            Foo(b: x == y);
        }
    }
}
";
            await VerifyFix.VerifyCodeFixAsync(test, fixedCode);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_BooleanOperation()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x)
        {
            Foo([|x is not 0 and not 1|]);
        }
    }
}
";
            var fixedCode = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x)
        {
            Foo(b: x is not 0 and not 1);
        }
    }
}
";
            await VerifyFix.VerifyCodeFixAsync(test, fixedCode);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_NamedArgumentKeywordHandling_Repro()
        {
            var test = @"
public class C
{
    void M(int @class, int x) { }
    void Call()
    {
        M(0, {|#0:1|});
    }
}
";
            var fixtest = @"
public class C
{
    void M(int @class, int x) { }
    void Call()
    {
        M(0, x: 1);
    }
}
";
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("x");

            // Re-trying keyword test with nullable type
             var testKeyword = @"
public class C
{
    void M(string @class) { }
    void Call()
    {
        M({|#0:null|});
    }
}
";
            var fixtestKeyword = @"
public class C
{
    void M(string @class) { }
    void Call()
    {
        M(@class: null);
    }
}
";
            var expectedKeyword = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("class");

            await VerifyFix.VerifyCodeFixAsync(testKeyword, expectedKeyword, fixtestKeyword);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_MethodLiteralArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(float index, bool strict, string message) {}

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
        public void Foo(float index, bool strict, string message) {}

        public void Test()
        {
            Foo(index: 1, strict: true, message: ""message"");
        }
    }
}
";
            var expected0 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyFix.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2 }, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ConstructorLiteralArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(float index, bool strict, string message) {}

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
        public CTest(float index, bool strict, string message) {}

        public void Test()
        {
            var x = new CTest(index: 1, strict: true, message: ""message"");
        }
    }
}
";
            var expected0 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyFix.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2 }, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_AttributeArguments()
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
            var expected = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_ComplexSyntax()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(float index, bool strict, string message) {}

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
        public void Foo(float index, bool strict, string message) {}

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
            var expected0 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyFix.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2 }, fixtest);
        }

        [TestMethod]
        public async Task SMA8000_CodeFix_NestedAndComplexSyntax()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        int Foo(float a, float b) => 0;
        float Bar(float x, float y) => 0;
        float Baz(float f, string s) => 0;

        public void Test()
        {
            var x = Foo({|#0:0|}, Bar(Baz({|#1:2.2f|}, {|#2:""message""|}), {|#3:11f|}));
        }

        public void TestMultiline()
        {
            var x = Foo({|#4:0|},
                        Bar(Baz({|#5:2.2f|},
                                {|#6:""message""|}),
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
        int Foo(float a, float b) => 0;
        float Bar(float x, float y) => 0;
        float Baz(float f, string s) => 0;

        public void Test()
        {
            var x = Foo(a: 0, Bar(Baz(f: 2.2f, s: ""message""), y: 11f));
        }

        public void TestMultiline()
        {
            var x = Foo(a: 0,
                        Bar(Baz(f: 2.2f,
                                s: ""message""),
                        y: 11f));
        }
    }
}
";
            var expected0 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("a");
            var expected1 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("f");
            var expected2 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("s");
            var expected3 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("y");
            var expected4 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 4).WithArguments("a");
            var expected5 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("f");
            var expected6 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 6).WithArguments("s");
            var expected7 = VerifyFix.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 7).WithArguments("y");
            await VerifyFix.VerifyCodeFixAsync(test, new[] { expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7 }, fixtest);
        }
    }
}
