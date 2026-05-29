// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7002_LambdaAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7002_Violation_LambdaCapturingVariable()
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
        public async Task SMA7002_Violation_LambdaWithParamsCapturingVariable()
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
        public async Task SMA7002_Violation_AsyncLambdaCapturingVariable()
        {
            var test = @"
using System;
using System.Threading.Tasks;
public class C
{
    void M()
    {
        int x = 0;
        Func<Task> f = {|#0:async () =>|} { await Task.Delay(x); };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Violation_LambdaCapturingThis()
        {
            var test = @"
using System;
public class C
{
    int field = 0;
    void M()
    {
        Action a = {|#0:() =>|} { field++; };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Violation_LambdaCapturingThisViaMethod()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    void M()
    {
        Action a = {|#0:() =>|} { InstanceMethod(); };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Compliant_LambdaCapturingVariableWithCommentOnLambda()
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
        public async Task SMA7002_Compliant_LambdaCapturingVariableWithCommentOnDeclaration()
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
        public async Task SMA7002_Violation_LambdaInArgumentCapturingVariable()
        {
            var test = @"
using System;
public class C
{
    void Foo(Action a) { }
    void M()
    {
        int x = 0;
        Foo({|#0:() =>|} { x++; });
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Violation_SimpleLambdaCapturingVariable()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        Func<int, int> f = {|#0:y =>|} x + y;
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Violation_LambdaCapturingMultipleVariables()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        int y = 1;
        Action a = {|#0:() =>|} { var z = x + y; };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Compliant_LambdaCapturingWithCommentOnParenthesizedLambda()
        {
            var test = @"
using System;
public class C
{
    void Foo(Action<int> a) { }
    void M()
    {
        int x = 0;
        Foo(
            // Allow allocation
            (args) => { x++; }
        );
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
