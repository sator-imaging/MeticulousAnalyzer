// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MidFlowBranchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8030_MidFlowBranchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8030_Compliant_EarlyBranchesOnly()
        {
            var test = @"
class C
{
    int M(bool invalid, int x)
    {
        if (invalid) return 0;
        if (x < 0) return -1;

        int a = 1;
        int b = 2;
        return a + b;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_DeconstructionLocalDeclarationsBeforeIfBranch()
        {
            var test = @"
class C
{
    int M(bool foo)
    {
        var (a, b) = (31, 42);
        (var x, var y) = (31, 42);
        (b, _) = (31, 42);

        if (foo)
        {
            return a + x;
        }

        return b + y;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_MidFlowBranchInIf()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo, bool bar)
    {
        if (invalid) return 0;

        int x = 10;
        x++;

        if (foo)
        {
            {|#0:return|} 1;
        }

        if (bar)
        {
            {|#1:return|} 2;
        }

        return 3;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0),
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_IfElseIfElseAllBranch()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo, bool bar)
    {
        if (invalid) return 0;

        int x = 10;
        x++;

        if (foo)
        {
            return 1;
        }
        else if (bar)
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_YieldBranchInMidFlowIf()
        {
            var test = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M(bool invalid, bool foo)
    {
        if (invalid) yield break;

        int count = 0;
        count++;

        if (foo)
        {
            {|#0:yield|} return 1;
        }

        yield return 2;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LocalDeclarationsBeforeIfBranch()
        {
            var test = @"
class C
{
    int M(bool foo)
    {
        int x = 1;
        string s = ""test"";

        if (foo)
        {
            return 1;
        }

        return 0;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_VoidReturningMethod()
        {
            var test = @"
class C
{
    void M(bool invalid, bool foo)
    {
        if (invalid) return;

        int x = 1;
        x++;

        if (foo)
        {
            x++;
        }
        else
        {
            x--;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_VoidReturningMethod()
        {
            var test = @"
class C
{
    void M(bool invalid, bool foo)
    {
        if (invalid) return;

        int x = 1;
        x++;

        if (foo)
        {
            {|#0:return|};
        }

        x++;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_EarlyThrowInsteadOfBranch()
        {
            var test = @"
using System;

class C
{
    int M(bool invalid, bool foo)
    {
        if (invalid) throw new InvalidOperationException();

        int x = 1;
        x++;

        if (foo)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ThrowInMidFlowBranch()
        {
            var test = @"
using System;

class C
{
    int M(bool foo)
    {
        int x = 1;
        x++;

        if (foo)
        {
            throw new InvalidOperationException();
        }
        else
        {
            return 0;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_NestedIfWithoutPriorStatements()
        {
            var test = @"
class C
{
    void M(bool foo, bool bar)
    {
        if (foo)
        {
            if (bar)
            {
                return;
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_NestedIfWithPriorStatement()
        {
            var test = @"
class C
{
    void Alpha() { }

    void M(bool foo, bool bar)
    {
        if (foo)
        {
            Alpha();

            if (bar)
            {
                {|#0:return|};
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_NestedIfElseWithPriorStatement()
        {
            var test = @"
class C
{
    void Alpha() { }

    void M(bool foo, bool bar)
    {
        if (foo)
        {
            Alpha();

            if (bar)
            {
                return;
            }
            else
            {
                return;
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ForLoopEarlyContinue()
        {
            var test = @"
class C
{
    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0) continue;

            int x = i;
            x++;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_ForLoopMidFlowContinue()
        {
            var test = @"
class C
{
    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            int x = i;
            x++;

            if (i % 2 == 0)
            {
                {|#0:continue|};
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ForeachLoopEarlyContinue()
        {
            var test = @"
class C
{
    void M(string[] items)
    {
        foreach (var item in items)
        {
            if (item == null) continue;

            int len = item.Length;
            len++;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_ForeachLoopMidFlowContinue()
        {
            var test = @"
class C
{
    void M(string[] items)
    {
        foreach (var item in items)
        {
            int len = item.Length;
            len++;

            if (item == null)
            {
                {|#0:continue|};
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_WhileLoopEarlyContinue()
        {
            var test = @"
class C
{
    void M(bool cond, bool skip)
    {
        while (cond)
        {
            if (skip) continue;

            int x = 1;
            x++;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_WhileLoopMidFlowContinue()
        {
            var test = @"
class C
{
    void M(bool cond, bool skip)
    {
        while (cond)
        {
            int x = 1;
            x++;

            if (skip)
            {
                {|#0:continue|};
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LoopNestedIfWithoutPriorStatements()
        {
            var test = @"
class C
{
    void M(bool foo, bool bar)
    {
        for (int i = 0; i < 10; i++)
        {
            if (foo)
            {
                if (bar)
                {
                    continue;
                }
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_LoopNestedIfWithPriorStatement()
        {
            var test = @"
class C
{
    void Alpha() { }

    void M(bool foo, bool bar)
    {
        for (int i = 0; i < 10; i++)
        {
            if (foo)
            {
                Alpha();

                if (bar)
                {
                    {|#0:continue|};
                }
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LoopNestedIfElseWithPriorStatement()
        {
            var test = @"
class C
{
    void Alpha() { }

    void M(bool foo, bool bar)
    {
        for (int i = 0; i < 10; i++)
        {
            if (foo)
            {
                Alpha();

                if (bar)
                {
                    continue;
                }
                else
                {
                    continue;
                }
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
