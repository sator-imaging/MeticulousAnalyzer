using System.Threading.Tasks;
// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCs = CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8002_NullSuppressionCodeFixProviderTests
    {
        private static DiagnosticResult CreateExpectedDiagnostic(int locationIndex, string operand)
        {
            return VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression)
                .WithLocation(locationIndex)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments(operand);
        }

        [TestMethod]
        public async Task SMA8002_CodeFix_NullSuppressionTriviaPreservation_Repro()
        {
            var test = @"
public class C
{
    void M(string s)
    {
        _ = /* leading */ {|#0:s!|} /* trailing */;
    }
}
";
            var fixtest = @"
public class C
{
    void M(string s)
    {
        _ = /* leading */ (((s)))! /* trailing */;
    }
}
";
            var expected = VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(markupKey: 0).WithArguments("s");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
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
