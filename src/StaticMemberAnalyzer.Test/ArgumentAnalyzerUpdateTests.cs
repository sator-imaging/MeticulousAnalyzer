// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

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
        public async Task TestExceptionSkipReporting()
        {
            var test = @"
using System;
namespace Test
{
    public class MyException : Exception
    {
        public MyException(string message, int code) : base(message) {}
        public void Log(string message, int level) {}
    }

    public class CTest
    {
        public void Test()
        {
            var e = new Exception(""message"");
            var e2 = new MyException(""message"", 1);
            e2.Log(""test"", 2);
            throw new Exception(""msg"", new Exception(""inner""));
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestStringConstructorSkipReporting()
        {
            var test = @"
using System;
namespace Test
{
    public class CTest
    {
        public void Test()
        {
            var s = new string('a', 3);
            var s2 = new string(new char[] { 'a', 'b' });
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestExceptionAttributeSkipReporting()
        {
            var test = @"
using System;
namespace Test
{
    public class ExceptionAttribute : Attribute
    {
        public ExceptionAttribute(string message, int code) {}
    }

    [Exception(""error"", 500)]
    public class CTest
    {
    }
}
";
            // ExceptionAttribute DOES NOT inherit from Exception, so it should STILL REPORT if it has multiple positional arguments.
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithSpan(startLine: 10, startColumn: 16, endLine: 10, endColumn: 23).WithArguments("message");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithSpan(startLine: 10, startColumn: 25, endLine: 10, endColumn: 28).WithArguments("code");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task TestInheritedExceptionAttributeSkipReporting()
        {
            var test = @"
using System;
namespace Test
{
    public class MyException : Exception
    {
        public MyException(string message, int code) : base(message) {}
    }

    public class ExceptionTestAttribute : Attribute
    {
        public ExceptionTestAttribute(string msg, int code) {}
    }

    [ExceptionTest(""error"", 500)]
    public class CTest
    {
    }
}
";
            // ExceptionTestAttribute is NOT an Exception.
            var expected0 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithSpan(startLine: 15, startColumn: 20, endLine: 15, endColumn: 27).WithArguments("msg");
            var expected1 = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithSpan(startLine: 15, startColumn: 29, endLine: 15, endColumn: 32).WithArguments("code");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
