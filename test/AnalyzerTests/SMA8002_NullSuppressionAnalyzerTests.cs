using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8002_NullSuppressionAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8002_Compliant_NullSuppressionWithThreeParentheses()
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
        public async Task SMA8002_Compliant_NullSuppressionWithMoreThanThreeParentheses()
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
        public async Task SMA8002_Violation_NullSuppressionWithNoParentheses()
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
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("foo"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionWithOneParenthesis()
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
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("this.foo"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionWithTwoParentheses()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = {|#0:((foo))!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("foo"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnMethodCallExpression()
        {
            var test = @"#nullable enable
class C
{
    string? GetValue() => null;
    void M()
    {
        var x = {|#0:GetValue()!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("GetValue()"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnPropertyAccess()
        {
            var test = @"#nullable enable
class C
{
    string? Value { get; set; }
    void M(C obj)
    {
        var x = {|#0:obj.Value!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("obj.Value"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnConditionalExpression()
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
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments(@"foo ?? """""));
        }

        [TestMethod]
        public async Task SMA8002_Violation_MultipleSuppressionsInOneMethod()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    string? bar;
    void M()
    {
        var x = {|#0:foo!|};
        var y = {|#1:bar!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("foo"),
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(1).WithArguments("bar"));
        }

        [TestMethod]
        public async Task SMA8002_Compliant_NoNullSuppressionOperator()
        {
            var test = @"#nullable enable
class C
{
    string foo = ""hello"";
    void M()
    {
        var x = foo;
        var y = foo.Length;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionWithoutNullableContext()
        {
            var test = @"
class C
{
    string foo;
    void M()
    {
        var x = {|#0:foo!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("foo"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnCastExpression()
        {
            var test = @"#nullable enable
class MyClass { }
class C
{
    void M(object obj)
    {
        var x = {|#0:((MyClass)obj)!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("(MyClass)obj"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnArrayAccess()
        {
            var test = @"#nullable enable
class C
{
    void M(string?[] arr)
    {
        var x = {|#0:arr[0]!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("arr[0]"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnNewExpression()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        var x = {|#0:(new object())!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("new object()"));
        }

        [TestMethod]
        public async Task SMA8002_Compliant_NullSuppressionTripleParenthesizedMethodCall()
        {
            var test = @"#nullable enable
class C
{
    string? GetValue() => null;
    void M()
    {
        var x = (((GetValue())))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnConditionalAccess()
        {
            var test = @"#nullable enable
class C
{
    string? Value { get; set; }
    void M(C? obj)
    {
        var x = {|#0:(obj?.Value)!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("obj?.Value"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnAwaitExpression()
        {
            var test = @"#nullable enable
using System.Threading.Tasks;
class C
{
    Task<string?> GetAsync() => Task.FromResult<string?>(null);
    async void M()
    {
        var x = {|#0:(await GetAsync())!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("await GetAsync()"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnLiteral()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        var x = {|#0:""hello""!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments(@"""hello"""));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppressionOnBinaryExpression()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        string? a = null;
        string? b = null;
        var x = {|#0:(a + b)!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("a + b"));
        }

        [TestMethod]
        public async Task SMA8002_Compliant_NullSuppressionExactlyThreeParenthesesOnComplexExpr()
        {
            var test = @"#nullable enable
class C
{
    string? Value { get; set; }
    void M(C? obj)
    {
        var x = (((obj?.Value)))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}

