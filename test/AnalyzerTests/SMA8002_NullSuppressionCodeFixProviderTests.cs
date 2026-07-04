using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
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
