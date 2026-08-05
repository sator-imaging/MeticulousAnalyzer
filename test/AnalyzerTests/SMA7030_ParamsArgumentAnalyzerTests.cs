// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ParamsArgumentAnalyzer>;
using VerifyFix = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ParamsArgumentAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.ParamsArgumentCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA7030_ParamsArgumentAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7030_Violation_ParamsImplicitAllocation_WithLiteralArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo({|#0:1, 2, 3|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                .WithLocation(markupKey: 0)
                .WithArguments("values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7030_Violation_ParamsImplicitAllocation_WithMixedArguments()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            int foo = 10;
            int bar = 20;
            Foo({|#0:1, foo, 2, bar|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                .WithLocation(markupKey: 0)
                .WithArguments("values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7030_Violation_ParamsImplicitAllocation_WithPrecedingNormalArgs()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(int x, params int[] values) {}

        public void Test()
        {
            Foo(42, {|#0:1, 2, 3|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                .WithLocation(markupKey: 0)
                .WithArguments("values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7030_Violation_ParamsImplicitAllocation_Constructor()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(params int[] values) {}

        public void Test()
        {
            var x = new CTest({|#0:1, 2, 3|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                .WithLocation(markupKey: 0)
                .WithArguments("values");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7030_Compliant_ParamsExplicitArray_PassedExplicitly()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo(new int[] { 1, 2, 3 });
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7030_Compliant_ParamsNoArguments_NoAlloc()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7030_CodeFix_ParamsImplicitAllocation_ToExplicitArray()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo({|#0:1, 2, 3|});
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public void Foo(params int[] values) {}

        public void Test()
        {
            Foo(new int[] { 1, 2, 3 });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                .WithLocation(markupKey: 0)
                .WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7030_CodeFix_ParamsImplicitAllocation_ConstructorToExplicitArray()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public CTest(params int[] values) {}

        public void Test()
        {
            var x = new CTest({|#0:1, 2, 3|});
        }
    }
}
";
            var fixtest = @"
namespace Test
{
    public class CTest
    {
        public CTest(params int[] values) {}

        public void Test()
        {
            var x = new CTest(new int[] { 1, 2, 3 });
        }
    }
}
";
            var expected = VerifyFix.Diagnostic(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation)
                .WithLocation(markupKey: 0)
                .WithArguments("values");
            await VerifyFix.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
