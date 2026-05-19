// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;
using Microsoft.CodeAnalysis.Testing;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class NullSuppressionAnalyzerTests
    {
        [TestMethod]
        public async Task NullSuppressionWithoutParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = foo!;
    }
}";
            var expected = VerifyCS.Diagnostic().WithSpan(startLine: 6, startColumn: 17, endLine: 6, endColumn: 21);

            var fixedSource = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = (((foo)))!;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task NullSuppressionWithThreeParentheses_DoesNotReportDiagnostic()
        {
            var test = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = (((foo)))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NullSuppressionWithMoreThanThreeParentheses_DoesNotReportDiagnostic()
        {
            var test = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = ((((foo))))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NullSuppressionWithOneParenthesis_Exact_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = (foo + """")!;
    }
}";
            var expected = VerifyCS.Diagnostic().WithSpan(startLine: 6, startColumn: 17, endLine: 6, endColumn: 28);

            var fixedSource = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = (((foo + """")))!;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task NullSuppressionWithTwoParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = ((foo + """"))!;
    }
}";
            var expected = VerifyCS.Diagnostic().WithSpan(startLine: 6, startColumn: 17, endLine: 6, endColumn: 30);

            var fixedSource = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = (((foo + """")))!;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task NullSuppressionInsideParentheses_Requested_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = (foo!);
    }
}";
            var expected = VerifyCS.Diagnostic().WithSpan(startLine: 6, startColumn: 18, endLine: 6, endColumn: 22);

            var fixedSource = @"#nullable enable
class C
{
    void M(string? foo)
    {
        var x = ((((foo)))!);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }
    }
}
