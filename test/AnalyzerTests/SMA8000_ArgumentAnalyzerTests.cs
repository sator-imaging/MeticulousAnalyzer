using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA8000_ArgumentAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8000_Violation_MethodLiteralArguments()
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
        public async Task SMA8000_Violation_ConstructorLiteralArguments()
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
        public async Task SMA8000_Compliant_NamedLiteralArguments()
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
        public async Task SMA8000_Violation_SystemNamespaceConstructorStillReported()
        {
            var test = @"
using System;
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            // First string argument is allowed for constructors by default.
            var g1 = new Guid(""00000000-0000-0000-0000-000000000000"");

            // Int/Long argument for constructor is not allowed to be unnamed.
            var t1 = new TimeSpan({|#1:1000|});
        }
    }
}
";
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("ticks");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ConstAndEnumArguments()
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
        public async Task SMA8000_Violation_AttributeArguments()
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
        public async Task SMA8000_Compliant_AttributeNamedArguments()
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
        public async Task SMA8000_Compliant_ExpressionArguments()
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
        public async Task SMA8000_Compliant_IndexerArguments()
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
        public async Task SMA8000_Compliant_AllowedNamespacesOmitNamed()
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
        public async Task SMA8000_Violation_SingleArgumentOmitNamed()
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
        public async Task SMA8000_Violation_MultipleArgumentsStill()
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
        public async Task SMA8000_Compliant_AttributeSingleArgumentOmitNamed()
        {
            var test = @"
using System;
namespace Test
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class MyAttribute : Attribute
    {
        public MyAttribute(string a) {}
    }

    [My(""a"")]
    public class CTest
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Violation_AttributeMultiplePositionalArguments()
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
        public MyAttribute(int a) {}
    }

    [My({|#0:1|}, {|#1:2|})]
    [My({|#2:1|}, {|#3:2|}, {|#4:3|})]
    [My({|#5:1|})]
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
            var expected5 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("a");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5);
        }

        [TestMethod]
        public async Task SMA8000_Violation_StringConstructorAndFirstArgumentStringChar()
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
        public async Task SMA8000_Compliant_NamedArgumentsSuppressError()
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
        public async Task SMA8000_Compliant_ObjectFirstArgumentLoggerMessage()
        {
            var test = @"
namespace Test
{
    public class Logger
    {
        public void Log(object message) {}
    }

    public class CTest
    {
        public void Test()
        {
            var logger = new Logger();
            logger.Log(""message"");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Violation_NullAndDefaultArguments()
        {
            var test = @"
using System;
namespace Test
{
    public class MyClassInt { public MyClassInt(int i) {} public void M(int i) {} }
    public class MyClassStr { public MyClassStr(string s) {} public void M(string s) {} }
    public class MyClassChar { public MyClassChar(char c) {} public void M(char c) {} }

    public class CTest
    {
        public void Test()
        {
            var mci = new MyClassInt(i: 0);
            var mcs = new MyClassStr("""");
            var mcc = new MyClassChar(' ');

            // 1. string method (System.String)
            // int
            """".Substring({|#0:default|});
            """".Substring({|#1:default(int)|});
            // string
            string.Intern({|#2:null|});
            string.Intern({|#3:default|});
            string.Intern({|#4:default(string)|});
            // char
            """".Trim({|#5:(char)default|});
            """".Trim({|#6:default(char)|});

            // 2. string constructor (System.String)
            // int (count)
            new string('a', {|#7:default|});
            new string('a', {|#8:default(int)|});
            // string (value)
            new string({|#9:(char[])null|});
            new string({|#10:default(char[])|});
            new string({|#11:(char[])default|});
            // char (c)
            new string({|#12:default|}, 1);
            new string({|#13:default(char)|}, 1);

            // 3. MyClass method
            // int
            mci.M({|#14:default|});
            mci.M({|#15:default(int)|});
            // string
            mcs.M({|#16:null|});
            mcs.M({|#17:default|});
            mcs.M({|#18:default(string)|});
            // char
            mcc.M({|#19:default|});
            mcc.M({|#20:default(char)|});

            // 4. MyClass constructor
            // int
            new MyClassInt({|#21:default|});
            new MyClassInt({|#22:default(int)|});
            // string
            new MyClassStr({|#23:null|});
            new MyClassStr({|#24:default|});
            new MyClassStr({|#25:default(string)|});
            // char
            new MyClassChar({|#26:default|});
            new MyClassChar({|#27:default(char)|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("startIndex");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 1).WithArguments("startIndex");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 2).WithArguments("str");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 3).WithArguments("str");
            var expected4 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 4).WithArguments("str");
            var expected5 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 5).WithArguments("trimChar");
            var expected6 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 6).WithArguments("trimChar");

            var expected7 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 7).WithArguments("count");
            var expected8 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 8).WithArguments("count");
            var expected9 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 9).WithArguments("value");
            var expected10 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 10).WithArguments("value");
            var expected11 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 11).WithArguments("value");
            var expected12 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 12).WithArguments("c");
            var expected13 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 13).WithArguments("c");

            var expected14 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 14).WithArguments("i");
            var expected15 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 15).WithArguments("i");
            var expected16 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 16).WithArguments("s");
            var expected17 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 17).WithArguments("s");
            var expected18 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 18).WithArguments("s");
            var expected19 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 19).WithArguments("c");
            var expected20 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 20).WithArguments("c");

            var expected21 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 21).WithArguments("i");
            var expected22 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 22).WithArguments("i");
            var expected23 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 23).WithArguments("s");
            var expected24 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 24).WithArguments("s");
            var expected25 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 25).WithArguments("s");
            var expected26 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 26).WithArguments("c");
            var expected27 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 27).WithArguments("c");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7, expected8, expected9, expected10, expected11, expected12, expected13, expected14, expected15, expected16, expected17, expected18, expected19, expected20, expected21, expected22, expected23, expected24, expected25, expected26, expected27);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_ImplicitConversion()
        {
            var test = @"
#nullable enable
namespace Test
{
    interface IMyClass { }
    class MyClass : IMyClass
    {
        public MyClass(MyClass? other) { }
        public MyClass(IMyClass? other) { }

        public IMyClass Instance { get; } = new MyClass(other: null);

        static void Foo(MyClass value) { }
        static void Foo(IMyClass value) { }

        public void PropertyAccessTest(MyClass other)
        {
            Foo(this.Instance);
            Foo(other.Instance);
            Foo(new(this.Instance));
            Foo(new(other.Instance));
            Foo(new MyClass(this.Instance));
            Foo(new MyClass(other.Instance));

            new MyClass(this.Instance);
            new MyClass(other.Instance);
            new MyClass(new(this.Instance));
            new MyClass(new(other.Instance));
            new MyClass(new MyClass(this.Instance));
            new MyClass(new MyClass(other.Instance));

            MyClass x1 = new(this.Instance);
            MyClass x2 = new(other.Instance);
            MyClass x3 = new(new(this.Instance));
            MyClass x4 = new(new(other.Instance));
            MyClass x5 = new(new MyClass(this.Instance));
            MyClass x6 = new(new MyClass(other.Instance));
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_AssertClassIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Assert
    {
        public static void AreEqual(int expected, int actual) {}
    }

    public class CTest
    {
        public void Test()
        {
            Assert.AreEqual(1, 2);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_MustClassIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Must
    {
        public static void BeTrue(bool b) {}
        public static void Anything(int x, string s) {}
    }

    public class CTest
    {
        public void Test()
        {
            Must.BeTrue(true);
            Must.Anything(1, ""msg"");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_DebugClassIgnoresAllArguments()
        {
            var test = @"
namespace Test
{
    public static class Debug
    {
        public static void Assert(bool condition) {}
        public static void Log(string msg, int level) {}
    }

    public class CTest
    {
        public void Test()
        {
            Debug.Assert(true);
            Debug.Log(""msg"", 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Violation_OtherClassesStill()
        {
            var test = @"
namespace Test
{
    public static class Other
    {
        public static void BeTrue(bool b) {}
    }

    public class CTest
    {
        public void Test()
        {
            Other.BeTrue({|#0:true|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA8000_Violation_AttributeNotExemptEvenIfNameMatches()
        {
            var test = @"
using System;
namespace Test
{
    public class MustAttribute : Attribute
    {
        public MustAttribute(bool b) {}
    }

    [Must({|#0:true|})]
    public class CTest
    {
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA8000_Violation_BooleanBinaryOperationDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x, int y)
        {
            Foo({|#0:x == y|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_BooleanExpressionNotFirstArgument()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int i, bool b) {}
        public void Test(int x, int y)
        {
            Foo(1, {|#0:x == y|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_BooleanUnaryOperationDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(bool b)
        {
            Foo({|#0:!b|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_OtherBinaryOperationsNotFirstArgumentNot()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a, int i) {}
        public void Test(int x, int y)
        {
            Foo(1, x + y);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_OtherBinaryOperationsFirstArgumentNot()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int i) {}
        public void Test(int x, int y)
        {
            Foo(x + y);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Violation_AttributeBooleanExpression()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(bool b) {}
    }

    [My({|#0:1 == 1|})]
    public class CTest
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_BooleanPatternOperationDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x)
        {
            Foo({|#0:x is not 0 and not 1|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_SimpleBooleanPatternOperationDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int x)
        {
            Foo({|#0:x is 0|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Violation_ComplexBooleanPatternOperationDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(bool b) {}
        public void Test(int? x)
        {
            Foo({|#0:x is > 42 and < 310 or null|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_SystemMathAllowed()
        {
            var test = @"
using System;
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var x = Math.Min(1, 2);
            var y = Math.Abs(-1);
            var z = Math.Max(1.0, 2.0);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_MathfAllowed()
        {
            var test = @"
namespace Test
{
    public class Mathf
    {
        public static float Clamp(float value, float min, float max) => 0;
    }

    public class CTest
    {
        public void Test()
        {
            var x = Mathf.Clamp(0.5f, 0, 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8000_Compliant_SystemNamespaceSingleArgument()
        {
            var test = @"
using System;
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var t1 = TimeSpan.FromSeconds(10);
            var t2 = TimeSpan.FromSeconds(10.0);
            var d1 = DateTime.FromBinary(0);
            var g1 = Guid.Parse(""00000000-0000-0000-0000-000000000000"");
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
