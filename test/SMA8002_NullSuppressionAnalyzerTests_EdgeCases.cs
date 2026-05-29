// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.NullSuppressionAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.NullSuppressionCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA8002_NullSuppressionAnalyzerTests_EdgeCases
    {
        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_DefaultLiteral()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        var x = {|#0:default(string)!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("default(string)"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_NullLiteral()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        string x = {|#0:null!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("null"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_DoubleBang()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        string? foo = null;
        var x = {|#0:{|#1:foo!|}!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS8715").WithSpan(7, 17, 7, 20),
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(1).WithArguments("foo"),
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("foo!"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_ThisExpression()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        var x = {|#0:this!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("this"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_TypeOfExpression()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        var x = {|#0:(typeof(string))!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("typeof(string)"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_InterpolatedString()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        string? value = null;
        string s = {|#0:$""{value}""!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments(@"$""{value}"""));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_LambdaInvocation()
        {
            var test = @"#nullable enable
using System;
class C
{
    void M()
    {
        Func<string?> f = () => null;
        var x = {|#0:f()!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("f()"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_AsExpression()
        {
            var test = @"#nullable enable
class C
{
    void M(object obj)
    {
        var x = {|#0:(obj as string)!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("obj as string"));
        }

        [TestMethod]
        public async Task SMA8002_Compliant_NullSuppression_ThreeParensAroundAs()
        {
            var test = @"#nullable enable
class C
{
    void M(object obj)
    {
        var x = (((obj as string)))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_TernaryExpression()
        {
            var test = @"#nullable enable
class C
{
    void M(bool condition)
    {
        string? a = ""hello"";
        string? b = null;
        var x = {|#0:(condition ? a : b)!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("condition ? a : b"));
        }

        [TestMethod]
        public async Task SMA8002_Compliant_NullSuppression_ThreeParensAroundTernary()
        {
            var test = @"#nullable enable
class C
{
    void M(bool condition)
    {
        string? a = ""hello"";
        string? b = null;
        var x = (((condition ? a : b)))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_IndexerAccess()
        {
            var test = @"#nullable enable
using System.Collections.Generic;
class C
{
    void M()
    {
        var dict = new Dictionary<string, string?>();
        var x = {|#0:dict[""key""]!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments(@"dict[""key""]"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_GenericMethodCall()
        {
            var test = @"#nullable enable
class C
{
    T? Method<T>() where T : class => default;
    void M()
    {
        var x = {|#0:Method<string>()!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("Method<string>()"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_NameofExpression()
        {
            var test = @"#nullable enable
class C
{
    string? foo;
    void M()
    {
        var x = {|#0:(nameof(foo))!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("nameof(foo)"));
        }

        [TestMethod]
        public async Task SMA8002_Violation_NullSuppression_TupleExpression()
        {
            var test = @"#nullable enable
class C
{
    void M()
    {
        var x = {|#0:((1, 2))!|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(NullSuppressionAnalyzer.RuleId_NullSuppression).WithLocation(0).WithArguments("(1, 2)"));
        }

        [TestMethod]
        public async Task SMA8002_Compliant_NullSuppression_FourParensGenericMethod()
        {
            var test = @"#nullable enable
class C
{
    T? Method<T>() where T : class => default;
    void M()
    {
        var x = ((((Method<string>()))))!;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
