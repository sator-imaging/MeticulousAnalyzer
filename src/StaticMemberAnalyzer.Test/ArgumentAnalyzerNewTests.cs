using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer>;
using VerifyFix = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ArgumentAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NamedArgumentCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ArgumentAnalyzerNewTests
    {
        [TestMethod]
        public async Task TestBooleanBinaryOperationDiagnostic()
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
        public async Task TestBooleanExpressionCodeFix()
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
        public async Task TestBooleanExpressionNotFirstArgument()
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
        public async Task TestOtherBinaryOperationsNotFirstArgumentReported()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int a, int i) {}
        public void Test(int x, int y)
        {
            Foo(1, {|#0:x + y|});
        }
    }
}
";
            // Non-boolean binary operations at non-first positions are now reported because they are
            // included in IsPossibleOperation but not exempt by IsOmittableType (which only handles index 0).
            var expected = VerifyCS.Diagnostic(ArgumentAnalyzer.RuleId_LiteralArgument).WithLocation(markupKey: 0).WithArguments("i");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestOtherBinaryOperationsFirstArgumentNotReported()
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
            // First argument int is exempt for methods.
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestAttributeBooleanExpression()
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
    }
}
