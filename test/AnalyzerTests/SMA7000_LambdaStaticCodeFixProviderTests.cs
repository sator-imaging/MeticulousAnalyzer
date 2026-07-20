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
    public class SMA7000_LambdaStaticCodeFixProviderTests
    {
        [TestMethod]
        public async Task SMA7000_CodeFix_NonStaticLambda()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Action a = {|#0:() => { }|};
    }
}
";
            var fixtest = @"
using System;
public class C
{
    void M()
    {
        Action a = static () => { };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7000_CodeFix_NonStaticLambdaInArgument()
        {
            var test = @"
using System;
public class C
{
    void Foo(Action a) { }
    void M()
    {
        Foo({|#0:() => { }|});
    }
}
";
            var fixtest = @"
using System;
public class C
{
    void Foo(Action a) { }
    void M()
    {
        Foo(static () => { });
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7000_CodeFix_NonStaticAsyncLambda()
        {
            var test = @"
using System;
using System.Threading.Tasks;
public class C
{
    void M()
    {
        Func<Task> a = {|#0:async () => { await Task.Delay(1); }|};
    }
}
";
            var fixtest = @"
using System;
using System.Threading.Tasks;
public class C
{
    void M()
    {
        Func<Task> a = static async () => { await Task.Delay(1); };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7000_CodeFix_AddStaticModifierPreservesFormatting_ReproIssue3()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Action a =
            {|#0:() => { }|};
    }
}
";
            var fixtest = @"
using System;
public class C
{
    void M()
    {
        Action a =
            static () => { };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
