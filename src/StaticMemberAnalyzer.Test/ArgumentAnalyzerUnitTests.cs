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
            Foo(1, {|#1:true|}, {|#2:""message""|});
        }
    }
}
";
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("message");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
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
        public async Task TestAllowedNamespacesOmitNamed()
        {
            var test = @"
using System.IO;
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var s = string.Join("","", ""a"", ""b"");
            var s2 = string.Format(""{0} {1}"", 1, 2);
            var f = File.ReadAllText(""path"");
            var p = Path.Combine(""a"", ""b"");
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
        public void Bar(string a) {}
        public void Baz(char a) {}
        public void Qux(bool a) {}
        public void Default(string a, int b = 0) {}

        public void Test()
        {
            Foo(1);
            Bar(""a"");
            Baz('a');
            Qux({|#1:true|});
            Default(""a"");
            var x = new CTest({|#2:1|});
            var y = new CTest(""a"");
        }

        public CTest(int a) {}
        public CTest(string a) {}
    }
}
";
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("a");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
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
            Foo(1, {|#1:2|});
            Bar(1, {|#3:2|});
            Bar(1, {|#5:2|}, {|#6:3|});
            var x = new CTest({|#7:1|}, {|#8:2|});
            var y = new CTest({|#9:1|}, {|#10:2|}, {|#11:3|});
        }

        public CTest(int a, int b) {}
        public CTest(int a, int b = 0, int c = 0) {}
    }
}
";
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("b");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("b");
            var expected5 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("b");
            var expected6 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 6).WithArguments("c");
            var expected7 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 7).WithArguments("a");
            var expected8 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 8).WithArguments("b");
            var expected9 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 9).WithArguments("a");
            var expected10 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 10).WithArguments("b");
            var expected11 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 11).WithArguments("c");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected3, expected5, expected6, expected7, expected8, expected9, expected10, expected11);
        }

        [TestMethod]
        public async Task TestAttributeSingleArgumentOmitNamed()
        {
            var test = @"
using System;
namespace Test
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class MyAttribute : Attribute
    {
        public MyAttribute(int a) {}
        public MyAttribute(string a) {}
    }

    [My(1)]
    [My(""a"")]
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
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class MyAttribute : Attribute
    {
        public MyAttribute(int a, int b) {}
        public MyAttribute(int a, int b, int c = 0) {}
    }

    [My({|#0:1|}, {|#1:2|})]
    [My({|#2:1|}, {|#3:2|}, {|#4:3|})]
    public class CTest
    {
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("a");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("b");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("a");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("b");
            var expected4 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 4).WithArguments("c");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4);
        }

        [TestMethod]
        public async Task TestStringConstructorAndFirstArgumentStringChar()
        {
            var test = @"
using System;
namespace Test
{
    public class CTest
    {
        public void Foo(string a, int b) {}
        public void Bar(char a, int b) {}
        public void Baz(int a, string b) {}

        public void Test()
        {
            var s1 = new string('a', 1);
            var s2 = new string(""abc"");
            var s3 = new string(new char[] { 'a' });

            Foo(""a"", {|#0:1|});
            Bar('a', {|#1:1|});
            Baz(1, {|#3:""a""|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("b");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected3);
        }

        [TestMethod]
        public async Task TestNamedArgumentsSuppressError()
        {
            var test = @"
using System.IO;
namespace Test
{
    public class CTest
    {
        public void Foo(int a, int b) {}
        public void Bar(string a) {}

        public void Test()
        {
            Foo(a: 1, b: 2);
            Bar(a: ""a"");
            var x = new CTest(a: 1);
            var s = string.Join(separator: "","", ""a"", ""b"");
            var f = File.ReadAllText(path: ""path"");
        }

        public CTest(int a) {}
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNullAndDefaultArguments()
        {
            var test = @"
using System;
namespace Test
{
    public class MyClass
    {
        public MyClass(int i, string s, char c) {}
        public void Method(int i, string s, char c) {}

        public void Test()
        {
            // 1. string method (System.String)
            // null/default literals are NOT exempt even in System.String
            string.Compare({|#0:null|}, {|#1:default(string)|});
            string.Compare({|#2:null|}, {|#3:default|});

            // 2. string constructor (System.String)
            // null and default for reference types are NOT exempt
            var s1 = new string({|#4:default(char[])|});
            // default literal for int IS exempt in System.String (wait, logic says no for default)
            var s2 = new string(new char[0], 0, {|#5:default(int)|});
            var s3 = new string(new char[0], 0, {|#6:default|});

            // 3. MyClass method
            var mc = new MyClass(i: 0, s: """", c: ' ');
            // int index 0 IS exempt for method.
            mc.Method(0, {|#7:null|}, {|#8:'\0'|});
            mc.Method({|#9:default(int)|}, {|#10:default(string)|}, {|#11:default(char)|});
            mc.Method({|#12:default|}, {|#13:default(string)|}, {|#14:default|});

            // 4. MyClass constructor
            // int index 0 is NOT exempt for constructor.
            var mc2 = new MyClass({|#15:0|}, {|#16:null|}, {|#17:'\0'|});
            var mc3 = new MyClass({|#18:default(int)|}, {|#19:default(string)|}, {|#20:default(char)|});
            var mc4 = new MyClass({|#21:default|}, {|#22:default(string)|}, {|#23:default|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("strA");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("strB");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("strA");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("strB");
            var expected4 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 4).WithArguments("value");
            var expected5 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("length");
            var expected6 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 6).WithArguments("length");
            var expected7 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 7).WithArguments("s");
            var expected8 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 8).WithArguments("c");
            var expected9 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 9).WithArguments("i");
            var expected10 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 10).WithArguments("s");
            var expected11 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 11).WithArguments("c");
            var expected12 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 12).WithArguments("i");
            var expected13 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 13).WithArguments("s");
            var expected14 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 14).WithArguments("c");
            var expected15 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 15).WithArguments("i");
            var expected16 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 16).WithArguments("s");
            var expected17 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 17).WithArguments("c");
            var expected18 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 18).WithArguments("i");
            var expected19 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 19).WithArguments("s");
            var expected20 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 20).WithArguments("c");
            var expected21 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 21).WithArguments("i");
            var expected22 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 22).WithArguments("s");
            var expected23 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 23).WithArguments("c");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7, expected8, expected9, expected10, expected11, expected12, expected13, expected14, expected15, expected16, expected17, expected18, expected19, expected20, expected21, expected22, expected23);
        }
    }
}