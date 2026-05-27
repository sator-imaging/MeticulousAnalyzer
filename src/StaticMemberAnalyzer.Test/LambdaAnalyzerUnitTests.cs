using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.LambdaAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.LambdaStaticCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class LambdaAnalyzerUnitTests
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7000_Conform_StaticLambda()
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
        public async Task SMA7000_Conform_LambdaCapturingVariableSuppressedByCommentInArgument()
        {
            var test = @"
using System;
public class C
{
    void Foo(Action a) { }
    void M()
    {
        int x = 0;
        Foo(
            // Allow allocation
            () => { x++; }
        );
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7000_Conform_LambdaCapturingVariableSuppressedByCommentInArgumentWithParams()
        {
            var test = @"
using System;
public class C
{
    void Foo(int i, Action<int> a) { }
    void M()
    {
        int x = 0;
        Foo(1,
            // Allow allocation
            (args) => { x++; }
        );
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Violate_ImplicitConversionFromInstanceMethod()
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
        Action a2 = {|#1:StaticMethod|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            var expected1 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 1)
                .WithArguments("System.Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7001_Violate_ImplicitConversionFromInstanceMethodInArgument()
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
        public async Task SMA7001_Violate_ImplicitConversionWithGenericAction()
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
        public async Task SMA7001_Violate_ImplicitConversionWithGenericFunc()
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
        public async Task SMA7001_Violate_NonActionFuncDelegateConversionReported()
        {
            var test = @"
using System;
public delegate void MyDelegate();
public class C
{
    void InstanceMethod() { }
    void M()
    {
        MyDelegate d = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDelegate");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Conform_NonActionFuncStaticMethodConversionNotReported()
        {
            var test = @"
using System;
public delegate void MyDelegate();
public class C
{
    static void StaticMethod() { }
    void M()
    {
        MyDelegate d = StaticMethod;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_Violate_ImplicitConversionFromInstanceField()
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

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodConversionCodeFix()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<int, string>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodWithMultipleArgsConversionCodeFix()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<int, string, bool>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_InstanceMethodConversionCodeFixDoesNotApply()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            // VerifyCodeFixAsync checks that NO code fix is available if fixtest is null or same as test (depending on verifier implementation)
            // CSharpCodeFixVerifier usually checks if any fix was offered.
            await VerifyCS.VerifyCodeFixAsync(test, expected, test);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_InstanceMethodWithReceiverConversionCodeFixDoesNotApply()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            await VerifyCS.VerifyCodeFixAsync(test, expected, test);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodInArgumentConversionCodeFix()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7001_CodeFix_StaticMethodWithMultipleArgsInArgumentConversionCodeFix()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<int, int>");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task SMA7002_Violate_LambdaCapturingVariableReportsSMA7002()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        Action a = {|#0:() =>|} { x++; };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Violate_LambdaWithParamsCapturingVariableReportsSMA7002()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        Action<int> a = {|#0:y =>|} { x++; };
        Action<int, int> b = {|#1:(y, z) =>|} { x++; };
    }
}
";
            var expected0 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            var expected1 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7000_Conform_LambdaCapturingVariableSuppressedByComment()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        // Allow allocation
        Action a = () => { x++; };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

    }
}
