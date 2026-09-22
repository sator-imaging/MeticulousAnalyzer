// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MidFlowBranchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8032_NonLocalExitFromLoopAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8032_Violation_NonLocalExitInLoops()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    int ReturnInForLoop(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == 0)
            {
                {|#0:return|} i;
            }
        }
        Console.WriteLine();
        return -1;
    }

    void ThrowStatementInWhileLoop(bool cond)
    {
        while (cond)
        {
            {|#1:throw|} new InvalidOperationException();
        }
        Console.WriteLine();
    }

    void ThrowExpressionInForeachLoop(string[] items)
    {
        foreach (var item in items)
        {
            string s = item ?? {|#2:throw|} new ArgumentNullException();
        }
        Console.WriteLine();
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_NonLocalExitFromLoop).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_NonLocalExitFromLoop).WithLocation(1);
            var expected2 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_NonLocalExitFromLoop).WithLocation(2);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA8032_Compliant_YieldInLoop()
        {
            var test = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> YieldInDoWhileLoop(bool cond)
    {
        do
        {
            yield return 1;
            yield break;
        } while (cond);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8032_Compliant_NonLocalExitInLoopsSuppressed()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    int ForLoop(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == 0)
            {
                // Allow non-local exit from loop
                return i;
            }
        }
        return -1;
    }

    int ForeachLoop(int[] items)
    {
        foreach (var item in items)
        {
            if (item == 0)
            {
                // Allow non-local exit from loop [ Early exit when zero is found ]
                return item;
            }
        }
        return -1;
    }

    void WhileLoop(ref bool cond)
    {
        while (cond)
        {
            // allow non-local exit from loop
            throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8032_Compliant_NonLocalExitInLambdaOrLocalFunctionInsideLoop()
        {
            var test = @"
using System;

class C
{
    void M(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Func<int> f = () =>
            {
                return i;
            };

            int LocalFunc()
            {
                return i * 2;
            }

            f();
            LocalFunc();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8032_Compliant_LastStatementInLoopFollowedByThrowOrReturn()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    int ReturnInForLoop(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == 0)
            {
                return i;
            }
        }
        return -1;
    }

    int ReturnInElseIfInForeachLoop(int[] items, bool foo, bool bar)
    {
        foreach (var item in items)
        {
            if (foo)
            {
                return 1;
            }
            else if (bar) return 2;
        }
        throw new Exception();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8032_Compliant_NestedIfInLoop()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    int M(string[] items, bool foo, bool bar, bool baz)
    {
        foreach (var item in items)
        {
            if (foo)
            {
                if (bar)
                {
                    return 1;
                }
            }
            else
            {
                if (baz)
                {
                    throw new Exception();
                }
            }
        }
        return 0;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8032_Violation_NestedIfInLoopFollowedByEmptyStatement()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    int M(string[] items, bool foo, bool bar, bool baz)
    {
        foreach (var item in items)
        {
            if (foo)
            {
                if (bar)
                {
                    {|#0:return|} 1;
                }
            }
            else
            {
                if (baz)
                {
                    {|#1:throw|} new Exception();
                }
            }
            ;  // empty statement breaks the requirement
        }
        return 0;
    }
}";
            var expected0_8030 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0);
            var expected0_8032 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_NonLocalExitFromLoop).WithLocation(0);
            var expected1_8030 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1);
            var expected1_8032 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_NonLocalExitFromLoop).WithLocation(1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0_8030, expected0_8032, expected1_8030, expected1_8032);
        }

        [TestMethod]
        public async Task SMA8032_Compliant_LoopIsLastStatementInMethodOrLoopRootBlock()
        {
            var test = @"
using System;

class C
{
    void Quit()
    {
        StopBackgroundTasks();

        int startAt = 0;
        while (!EnsureBackgroundTasksComplete())
        {
            if (startAt > 30)
            {
                throw new Exception();
            }
        }
    }

    void StopBackgroundTasks() { }
    bool EnsureBackgroundTasksComplete() => false;
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
