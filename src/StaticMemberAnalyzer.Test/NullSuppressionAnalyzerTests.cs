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
        public async Task SMA8002_CodeFix_NullSuppressionWithoutParentheses()
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
        public async Task SMA8002_Conform_NullSuppressionWithThreeParentheses()
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
        public async Task SMA8002_Conform_NullSuppressionWithMoreThanThreeParentheses()
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
        public async Task SMA8002_CodeFix_NullSuppressionWithOneParenthesis_Exact()
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
        public async Task SMA8002_CodeFix_NullSuppressionWithTwoParentheses()
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
        public async Task SMA8002_CodeFix_NullSuppressionInsideParentheses()
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
        public async Task SMA8002_CodeFix_NullSuppressionWithExpressionAndOneParenthesis()
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
        public async Task SMA8002_CodeFix_NullSuppressionWithExpressionAndTwoParentheses()
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
