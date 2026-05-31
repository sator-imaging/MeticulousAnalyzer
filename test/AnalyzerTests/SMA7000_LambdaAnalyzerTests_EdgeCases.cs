// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7000_LambdaAnalyzerTests_EdgeCases
    {
        [TestMethod]
        public async Task SMA7000_Compliant_AnonymousDelegate_Capturing()
        {
            // Anonymous delegate syntax (delegate { }) is an IAnonymousFunctionOperation
            // but its Syntax is NOT LambdaExpressionSyntax, so it returns early
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        Action a = delegate { x++; };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7000_Violation_LambdaInNestedScope_NoCapture()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        bool condition = true;
        if (condition)
        {
            Action a = {|#0:() => { }|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Violation_LambdaInLoop_NoCapture()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            Action a = {|#0:() => { }|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Compliant_LambdaCapturingLoopVariable()
        {
            var test = @"
using System;
using System.Collections.Generic;
public class C
{
    void M()
    {
        var actions = new List<Action>();
        for (int i = 0; i < 10; i++)
        {
            actions.Add(
                // Allow allocation
                () => { Console.WriteLine(i); }
            );
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7000_Violation_AsyncLambdaNoCapture_TaskDelay()
        {
            var test = @"
using System;
using System.Threading.Tasks;
public class C
{
    void M()
    {
        Func<Task> f = {|#0:async () => { await Task.Delay(100); }|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaCanBeStatic).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7000_Compliant_LambdaCapturingParameter()
        {
            var test = @"
using System;
public class C
{
    void M(int param)
    {
        // Allow allocation
        Action a = () => { Console.WriteLine(param); };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
