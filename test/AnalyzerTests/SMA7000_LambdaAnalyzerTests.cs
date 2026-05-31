// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7000_LambdaAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7000_Compliant_StaticLambda()
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
        public async Task SMA7000_Compliant_LambdaCapturingVariableCommentInArgument()
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
        public async Task SMA7000_Compliant_LambdaCapturingVariableCommentInArgumentWithParams()
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
        public async Task SMA7000_Compliant_NonActionFuncStaticMethodConversionNot()
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
        public async Task SMA7000_Compliant_LambdaCapturingVariableComment()
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

        [TestMethod]
        public async Task SMA7000_Violation_NonStaticLambdaWithoutCapture()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Violation_NonStaticLambdaWithParams()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Action<int> a = {|#0:(x) => { }|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Violation_NonStaticSimpleLambdaWithoutCapture()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Func<int, int> f = {|#0:x => x * 2|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Violation_NonStaticLambdaInArgument()
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Violation_NonStaticAsyncLambdaWithoutCapture()
        {
            var test = @"
using System;
using System.Threading.Tasks;
public class C
{
    void M()
    {
        Func<Task> f = {|#0:async () => { await Task.Delay(1); }|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Violation_NonStaticLambdaMultipleWithoutCapture()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Action a = {|#0:() => { }|};
        Action<int, int> b = {|#1:(x, y) => { }|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 0);
            var expected1 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaShouldBeStatic).WithLocation(markupKey: 1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7000_Compliant_StaticLambdaWithParams()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Func<int, int> f = static x => x * 2;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7000_Compliant_StaticLambdaInArgument()
        {
            var test = @"
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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
