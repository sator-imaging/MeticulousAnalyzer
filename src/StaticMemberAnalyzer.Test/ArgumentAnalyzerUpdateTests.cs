using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerUpdateTests
    {
        [TestMethod]
        public async Task TestSingleArgumentMethod_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index) {}

        public void Test()
        {
            Foo(1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestThreeArgumentMethod_WithOneArgument_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index, bool strict = true, string message = null) {}

        public void Test()
        {
            Foo(1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestThreeArgumentMethod_WithMultipleArguments_ReportsDiagnostics()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int index, bool strict = true, string message = null) {}

        public void Test()
        {
            Foo({|#0:1|}, {|#1:false|});
            Foo({|#2:1|}, {|#3:false|}, {|#4:""msg""|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(1).WithArguments("strict");
            var expected2 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(2).WithArguments("index");
            var expected3 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(3).WithArguments("strict");
            var expected4 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(4).WithArguments("message");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4);
        }

        [TestMethod]
        public async Task TestStringMethods_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            string.Concat(""a"", ""b"");
            string.Format(""{0} {1}"", 1, 2);
            ""abc"".Substring(0, 1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestSinglePositionalAttributeArgument_NoDiagnostic()
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

    [My(1, Name = ""test"")]
    public class CTest
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMultiplePositionalAttributeArguments_ReportsDiagnostics()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(int index, bool strict) {}
    }

    [My({|#0:1|}, {|#1:true|})]
    public class CTest
    {
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(0).WithArguments("index");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(1).WithArguments("strict");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task TestConfigureAwait_NoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;
namespace Test
{
    public class CTest
    {
        public async Task Foo()
        {
            await Task.Delay(1).ConfigureAwait(false);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestAttributeArgumentWithNullLiteral_ReportsDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(string name, string value) {}
    }

    [My({|#0:""test""|}, {|#1:null|})]
    public class CTest
    {
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(0).WithArguments("name");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(1).WithArguments("value");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
