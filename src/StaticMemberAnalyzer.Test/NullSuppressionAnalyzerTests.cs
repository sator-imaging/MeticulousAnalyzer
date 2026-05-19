using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class NullSuppressionAnalyzerTests
    {
        [TestMethod]
        public async Task NullSuppressionWithoutParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = [|foo!|];
        }
    }
}
";
            var fixedCode = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = (((foo)))!;
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
        }

        [TestMethod]
        public async Task NullSuppressionWithOneParenthesis_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = [|(foo + """")!|];
        }
    }
}
";
            var fixedCode = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = (((foo + """")))!;
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
        }

        [TestMethod]
        public async Task NullSuppressionWithTwoParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = [|((foo))!|];
        }
    }
}
";
            var fixedCode = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = (((foo)))!;
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
        }

        [TestMethod]
        public async Task NullSuppressionWithThreeParentheses_DoesNotReportDiagnostic()
        {
            var test = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = (((foo)))!;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NullSuppressionWithMoreThanThreeParentheses_DoesNotReportDiagnostic()
        {
            var test = @"#nullable enable
namespace Test
{
    public class C
    {
        public void M(string? foo)
        {
            var x = ((((foo))))!;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
