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
        public async Task TestStringMethods_DoesNotReportDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var s = ""hello world"";
            var sub = s.Substring(0, 5);
            var join = string.Join("","", new[] { ""a"", ""b"" });
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestSingleArgument_DoesNotReportDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a) {}
        public CTest(int a) {}

        public void Test()
        {
            Foo(1);
            var x = new CTest(1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestDefaultParametersSingleArgument_DoesNotReportDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a, int b = 0, int c = 0) {}

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
        public async Task TestAttributeSingleArgument_DoesNotReportDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    public class MyAttribute : Attribute
    {
        public MyAttribute(int a) {}
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
        public async Task TestMultipleArguments_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a, int b) {}

        public void Test()
        {
            Foo({|#0:1|}, {|#1:2|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(0).WithArguments("a");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(1).WithArguments("b");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
