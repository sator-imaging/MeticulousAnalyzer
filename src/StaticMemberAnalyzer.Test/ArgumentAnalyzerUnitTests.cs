using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerUnitTests
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
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
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
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task TestNamedLiteralArguments()
        {
            var test = @"
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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestConstAndEnumArguments()
        {
            var test = @"
namespace Test
{
    public enum ETest { Value }
    public class CTest
    {
        public const int MyConstIndex = 1;
        public void Foo(int index, ETest e) {}

        public void Test()
        {
            Foo(MyConstIndex, ETest.Value);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
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
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("index");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestAttributeNamedArguments()
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

    [My(index: 1, Name = ""test"")]
    public class CTest
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestExpressionArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index) {}

        public void Test(int i)
        {
            Foo(i + 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestIndexerArguments()
        {
            var test = @"
using System.Collections.Generic;
namespace Test
{
    public class CTest
    {
        public void Test(int[] array, Dictionary<string, int> dict)
        {
            var x = array[0];
            var y = dict[""key""];
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestStringMethodsOmitNamed()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var s = string.Join("","", ""a"", ""b"");
            var s2 = string.Format(""{0} {1}"", 1, 2);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestSingleArgumentOmitNamed()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a) {}
        public void Bar(int a, int b = 0) {}
        public void Baz(int a, int b = 0, int c = 0) {}

        public void Test()
        {
            Foo(1);
            Bar(1);
            Baz(1);
            var x = new CTest(1);
        }

        public CTest(int a) {}
        public CTest(int a, int b = 0, int c = 0) {}
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMultipleArgumentsStillReported()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a, int b) {}
        public void Bar(int a, int b = 0, int c = 0) {}

        public void Test()
        {
            Foo({|#0:1|}, {|#1:2|});
            Bar({|#2:1|}, {|#3:2|});
            Bar({|#4:1|}, {|#5:2|}, {|#6:3|});
            var x = new CTest({|#7:1|}, {|#8:2|});
            var y = new CTest({|#9:1|}, {|#10:2|}, {|#11:3|});
        }

        public CTest(int a, int b) {}
        public CTest(int a, int b = 0, int c = 0) {}
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("a");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("b");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("a");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("b");
            var expected4 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 4).WithArguments("a");
            var expected5 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("b");
            var expected6 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 6).WithArguments("c");
            var expected7 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 7).WithArguments("a");
            var expected8 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 8).WithArguments("b");
            var expected9 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 9).WithArguments("a");
            var expected10 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 10).WithArguments("b");
            var expected11 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 11).WithArguments("c");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7, expected8, expected9, expected10, expected11);
        }

        [TestMethod]
        public async Task TestAttributeSingleArgumentOmitNamed()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(int a, int b = 0, int c = 0) {}
    }

    [My(1)]
    public class CTest
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestAttributeMultiplePositionalArguments()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(int a, int b) {}
    }

    [My({|#0:1|}, {|#1:2|})]
    public class CTest
    {
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("a");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
