using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.LambdaAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.LambdaStaticCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class LambdaAnalyzerUnitTests
    {
        [TestMethod]
        public async Task TestNonStaticLambda()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestStaticLambda()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Action a = static () => { };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestImplicitConversionFromInstanceMethod()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    static void StaticMethod() { }

    void M()
    {
        Action a1 = {|#0:InstanceMethod|};
        Action a2 = StaticMethod;
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestImplicitConversionFromInstanceMethodInArgument()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    void Call(Action a) { }

    void M()
    {
        Call({|#0:InstanceMethod|});
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestImplicitConversionWithGenericAction()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod(int i) { }
    void M()
    {
        Action<int> a = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestImplicitConversionWithGenericFunc()
        {
            var test = @"
using System;
public class C
{
    int InstanceMethod() => 0;
    void M()
    {
        Func<int> f = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Func<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestNonActionFuncDelegateConversionNotReported()
        {
            var test = @"
using System;
public delegate void MyDelegate();
public class C
{
    void InstanceMethod() { }
    void M()
    {
        MyDelegate d = InstanceMethod;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNonStaticAsyncLambda()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestImplicitConversionFromInstanceField()
        {
            var test = @"
using System;
public class C
{
    public Action<object> InstanceField;
    void M()
    {
        // Contravariant conversion from Action<object> to Action<string>
        Action<string> a = {|#0:InstanceField|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<string>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
