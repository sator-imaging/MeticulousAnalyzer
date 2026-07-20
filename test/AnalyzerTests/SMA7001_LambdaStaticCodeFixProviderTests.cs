// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = global::MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<LambdaAnalyzer, LambdaStaticCodeFixProvider>;

    [TestClass]
    public class SMA7001_LambdaStaticCodeFixProviderTests
    {
        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodConversion()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod(int i, string s) { }
    void M()
    {
        Action<int, string> a = {|#0:StaticMethod|};
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod(int i, string s) { }
    void M()
    {
        Action<int, string> a = static (i, s) => StaticMethod(i, s);
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<int, string>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodWithMultipleArgsConversion()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod(int i, string s, bool b) { }
    void M()
    {
        Action<int, string, bool> a = {|#0:StaticMethod|};
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod(int i, string s, bool b) { }
    void M()
    {
        Action<int, string, bool> a = static (i, s, b) => StaticMethod(i, s, b);
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<int, string, bool>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_InstanceMethodConversion()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    void M()
    {
        Action a = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            // VerifyCodeFixAsync checks that NO code fix is available if fixtest is null or same as test (depending on verifier implementation)
            // CSharpCodeFixVerifier usually checks if any fix was offered.
            await VerifyCS.VerifyCodeFixAsync(test, expected, test);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_InstanceMethodWithReceiverConversion()
        {
            var test = @"
using System;
public class Other { public void InstanceMethod() { } }
public class C
{
    void M(Other obj)
    {
        Action a = {|#0:obj.InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyCodeFixAsync(test, expected, test);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodInArgumentConversion()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod() { }
    void Call(Action a) { }
    void M()
    {
        Call({|#0:StaticMethod|});
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod() { }
    void Call(Action a) { }
    void M()
    {
        Call(static () => StaticMethod());
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodWithMultipleArgsInArgumentConversion()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod(int i1, int i2) { }
    void Call(Action<int, int> a) { }
    void M()
    {
        Call({|#0:StaticMethod|});
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod(int i1, int i2) { }
    void Call(Action<int, int> a) { }
    void M()
    {
        Call(static (i1, i2) => StaticMethod(i1, i2));
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<int, int>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodWithKeywordArgsConversion_ReproIssue1()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod(int @class) { }
    void M()
    {
        Action<int> a = {|#0:StaticMethod|};
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod(int @class) { }
    void M()
    {
        Action<int> a = static (@class) => StaticMethod(@class);
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<int>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_WrapWithStaticLambdaPreservesTrivia_ReproIssue2()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod() { }
    void M()
    {
        Action a = /* leading */ {|#0:StaticMethod|} /* trailing */;
    }
}
";
            var fixtest = @"
using System;
public class C
{
    static void StaticMethod() { }
    void M()
    {
        Action a = /* leading */ static () => StaticMethod() /* trailing */;
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
