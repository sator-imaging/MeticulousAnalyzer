// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class NullSuppressionAnalyzerTests
    {
        private static DiagnosticResult CreateExpectedDiagnostic(int locationIndex, string operand)
        {
            return VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression)
                .WithLocation(locationIndex)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments(operand);
        }

        [TestMethod]
        public async Task NullSuppressionWithoutParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = {|#0:foo!|};
    }
}";
            var expected = CreateExpectedDiagnostic(0, operand: "foo");

            var fixedSource = @"#nullable enable
class C
{
    string? foo;
    void M()
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
    string? foo;
    void M()
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
    string? foo;
    void M()
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
    string? foo;
    void M()
    {
        var x = {|#0:(this.foo)!|};
    }
}";
            var expected = CreateExpectedDiagnostic(0, operand: "this.foo");

            var fixedSource = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = (((this.foo)))!;
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
    string? foo;
    void M()
    {
        var x = {|#0:((this.foo))!|};
    }
}";
            var expected = CreateExpectedDiagnostic(0, operand: "this.foo");

            var fixedSource = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = (((this.foo)))!;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task NullSuppressionInsideParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = ({|#0:foo!|});
    }
}";
            var expected = CreateExpectedDiagnostic(0, operand: "foo");

            var fixedSource = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = ((((foo)))!);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task NullSuppressionWithExpressionAndOneParenthesis_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = {|#0:(foo ?? """")!|};
    }
}";
            var expected = CreateExpectedDiagnostic(0, operand: "foo ?? \"\"");

            var fixedSource = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = (((foo ?? """")))!;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task NullSuppressionWithExpressionAndTwoParentheses_ReportsDiagnosticAndFixes()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = {|#0:((foo ?? """"))!|};
    }
}";
            var expected = CreateExpectedDiagnostic(0, operand: "foo ?? \"\"");

            var fixedSource = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = (((foo ?? """")))!;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }
    }
}
