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

        if (foo)
        {
            return a;
        }

        return b;
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
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0),
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1));
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
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
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
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
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
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
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
    void DoSomething(int x) { }

    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0) continue;

            int x = i;
            DoSomething(x);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ForLoopInvertedConditionWithoutContinue()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            int x = i;
            x++;
            if (i % 2 != 0)
            {
                DoSomething(x);
            }
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
    void DoSomething(int x) { }

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

            DoSomething(x);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ForeachLoopEarlyContinue()
        {
            var test = @"
class C
{
    void DoSomething(string item) { }

    void M(string[] items)
    {
        foreach (var item in items)
        {
            if (item == null) continue;

            DoSomething(item);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ForeachLoopInvertedConditionWithoutContinue()
        {
            var test = @"
class C
{
    void DoSomething(string item) { }

    void M(string[] items)
    {
        foreach (var item in items)
        {
            int len = item?.Length ?? 0;
            len++;

            if (item != null)
            {
                DoSomething(item);
            }
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
    void DoSomething(string item) { }

    void M(string[] items)
    {
        foreach (var item in items)
        {
            int len = item?.Length ?? 0;
            len++;

            if (item == null)
            {
                {|#0:continue|};
            }

            DoSomething(item);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_WhileLoopEarlyContinue()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(bool cond, bool skip)
    {
        while (cond)
        {
            if (skip) continue;

            int x = 1;
            DoSomething(x);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_WhileLoopInvertedConditionWithoutContinue()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(bool cond, bool skip)
    {
        while (cond)
        {
            int x = 1;
            x++;

            if (!skip)
            {
                DoSomething(x);
            }
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
    void DoSomething(int x) { }

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

            DoSomething(x);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LoopNestedIfWithoutPriorStatements()
        {
            var test = @"
class C
{
    void DoSomething() { }

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

            DoSomething();
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
    void DoSomething() { }

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

            DoSomething();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LoopNestedIfElseWithPriorStatement()
        {
            var test = @"
class C
{
    void Alpha() { }
    void DoSomething() { }

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

            DoSomething();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_NullCoalesceLocalDeclarationBeforeIfBranch()
        {
            var test = @"#nullable enable
class Item
{
    public int Value { get; set; }
}

class C
{
    void M(Item? some)
    {
        int value = some?.Value ?? 0;

        if (value == 0) return;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_DoWhileLoopEarlyContinue()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(bool cond, bool skip)
    {
        int i = 0;
        do
        {
            if (skip) continue;

            int x = i++;
            DoSomething(x);
        } while (cond);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_DoWhileLoopMidFlowContinue()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(bool cond, bool skip)
    {
        int i = 0;
        do
        {
            int x = i++;
            DoSomething(x);

            if (skip)
            {
                {|#0:continue|};
            }
        } while (cond);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LoopNullCoalesceLocalDeclarationBeforeIfBranch()
        {
            var test = @"#nullable enable
class Item
{
    public int Value { get; set; }
}

class C
{
    void M(Item?[] items)
    {
        foreach (var item in items)
        {
            int value = item?.Value ?? 0;

            if (value == 0) continue;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_LoopMidFlowReturn()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            int value = items[i];
            DoSomething(value);

            if (value == 0)
            {
                {|#0:return|};
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_MethodEarlyReturnWithNullCoalesce()
        {
            var test = @"#nullable enable
class Item
{
    public int Value { get; set; }
}

class C
{
    int M(Item? item)
    {
        int value = item?.Value ?? 0;

        if (value == 0) return -1;

        return value * 2;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_MethodMidFlowReturnAfterStatement()
        {
            var test = @"
class C
{
    void Process() { }

    int M(bool cond)
    {
        Process();

        if (cond)
        {
            {|#0:return|} 1;
        }

        return 0;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LocalFunctionEarlyReturn()
        {
            var test = @"
class C
{
    void M()
    {
        int LocalFunc(bool invalid)
        {
            if (invalid) return 0;

            int a = 10;
            return a;
        }

        LocalFunc(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_LocalFunctionMidFlowReturn()
        {
            var test = @"
class C
{
    void M()
    {
        void Helper() { }

        int LocalFunc(bool cond)
        {
            Helper();

            if (cond)
            {
                {|#0:return|} 1;
            }

            return 0;
        }

        LocalFunc(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Violation_ReassignmentStartsMainFlow_SimpleAssignment()
        {
            var test = @"
class C
{
    int M(bool earlyReturn, bool foo)
    {
        int pos;
        if (earlyReturn) return 0;

        pos = 310;

        if (foo)
        {
            {|#0:return|} 1;
        }

        return 2;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Violation_ReassignmentStartsMainFlow_TupleAssignment()
        {
            var test = @"
class C
{
    int M(bool earlyReturn, bool foo)
    {
        var (a, b) = (1, 2);
        if (earlyReturn) return 0;

        (a, b) = (11, 22);

        if (foo)
        {
            {|#0:return|} 1;
        }

        return 2;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_NonRepeatedDeclarationsAllowed()
        {
            var test = @"
class C
{
    int M(bool cond1, bool cond2)
    {
        int x = 1;
        if (cond1) return 0;

        int y = 2;
        if (cond2) return 0;

        return x + y;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_RepeatedDeclarationBeforeFirstIfDoesNotStartMainFlow()
        {
            var test = @"
class C
{
    int M(bool foo)
    {
        int x = 1;
        int y = 2;

        if (foo)
        {
            return 1;
        }

        return x + y;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_ElseLessIfElseIf()
        {
            var test = @"
using System;

class C
{
    void M(bool foo, bool bar)
    {
        int x = 10;
        x++;

        if (foo)
        {
            {|#0:return|};
        }
        else if (bar)
        {
            {|#1:throw|} new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0),
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8030_Violation_YieldInElseLessIfElseIf()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    IEnumerable<int> M(bool foo, bool bar)
    {
        int count = 0;
        count++;

        if (foo)
        {
            {|#0:yield|} return 1;
        }
        else if (bar)
        {
            {|#1:throw|} new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0),
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8030_Violation_IfOnlyExit()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    void DoSomething() { }

    void Return1(bool foo)
    {
        DoSomething();

        if (foo)
        {
            {|#0:return|};
        }
        else
        {
            // Do nothing
        }
    }

    void Return2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            {|#1:return|};
        }
        else if (bar)
        {
            // Do nothing
        }
    }

    IEnumerable<int> Yield1(bool foo)
    {
        DoSomething();

        if (foo)
        {
            {|#2:yield|} return 1;
        }
        else
        {
            // Do nothing
        }
    }

    IEnumerable<int> Yield2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            {|#3:yield|} return 1;
        }
        else if (bar)
        {
            // Do nothing
        }
    }

    void Throw1(bool foo)
    {
        DoSomething();

        if (foo)
        {
            {|#4:throw|} new Exception();
        }
        else
        {
            // Do nothing
        }
    }

    void Throw2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            {|#5:throw|} new Exception();
        }
        else if (bar)
        {
            // Do nothing
        }
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1);
            var expected2 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(2);
            var expected3 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(3);
            var expected4 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(4);
            var expected5 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(5);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5);
        }

        [TestMethod]
        public async Task SMA8030_Violation_ElseOnlyExit()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    void DoSomething() { }

    void Return1(bool foo)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else
        {
            {|#0:return|};
        }
    }

    void Return2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            // Do nothing
        }
        else
        {
            {|#1:return|};
        }
    }

    IEnumerable<int> Yield1(bool foo)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else
        {
            {|#2:yield|} return 1;
        }
    }

    IEnumerable<int> Yield2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            // Do nothing
        }
        else
        {
            {|#3:yield|} return 1;
        }
    }

    void Throw1(bool foo)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else
        {
            {|#4:throw|} new Exception();
        }
    }

    void Throw2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            // Do nothing
        }
        else
        {
            {|#5:throw|} new Exception();
        }
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1);
            var expected2 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(2);
            var expected3 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(3);
            var expected4 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(4);
            var expected5 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(5);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5);
        }

        [TestMethod]
        public async Task SMA8030_Violation_ElseIfOnlyExit()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    void DoSomething() { }

    void Return1(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            {|#0:return|};
        }
    }

    void Return2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            {|#1:return|};
        }
        else
        {
            // Do nothing
        }
    }

    IEnumerable<int> Yield1(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            {|#2:yield|} return 1;
        }
    }

    IEnumerable<int> Yield2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            {|#3:yield|} return 1;
        }
        else
        {
            // Do nothing
        }
    }

    void Throw1(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            {|#4:throw|} new Exception();
        }
    }

    void Throw2(bool foo, bool bar)
    {
        DoSomething();

        if (foo)
        {
            // Do nothing
        }
        else if (bar)
        {
            {|#5:throw|} new Exception();
        }
        else
        {
            // Do nothing
        }
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1);
            var expected2 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(2);
            var expected3 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(3);
            var expected4 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(4);
            var expected5 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(5);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_SyncMethod_IfStatementsWithoutExitInMainFlow()
        {
            var test = @"
class C
{
    void DoSomething() { }

    void If(bool foo)
    {
        DoSomething();

        if (foo) { }
    }

    void IfElse(bool foo)
    {
        DoSomething();

        if (foo) { }
        else { }
    }

    void IfElseIf(bool foo, bool bar)
    {
        DoSomething();

        if (foo) { }
        else if (bar) { }
    }

    void IfElseIfElse(bool foo, bool bar)
    {
        DoSomething();

        if (foo) { }
        else if (bar) { }
        else { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_AsyncMethod_IfStatementsWithoutExitInMainFlow()
        {
            var test = @"
using System.Threading.Tasks;

class C
{
    void DoSomething() { }

    async Task If(bool foo)
    {
        DoSomething();

        if (foo) { }
    }

    async Task IfElse(bool foo)
    {
        DoSomething();

        if (foo) { }
        else { }
    }

    async Task IfElseIf(bool foo, bool bar)
    {
        DoSomething();

        if (foo) { }
        else if (bar) { }
    }

    async Task IfElseIfElse(bool foo, bool bar)
    {
        DoSomething();

        if (foo) { }
        else if (bar) { }
        else { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_ForeachLoop_IfStatementsWithoutExitInMainFlow()
        {
            var test = @"
class C
{
    void DoSomething() { }

    void If(string[] items, bool foo)
    {
        foreach (var item in items)
        {
            DoSomething();

            if (foo) { }
        }
    }

    void IfElse(string[] items, bool foo)
    {
        foreach (var item in items)
        {
            DoSomething();

            if (foo) { }
            else { }
        }
    }

    void IfElseIf(string[] items, bool foo, bool bar)
    {
        foreach (var item in items)
        {
            DoSomething();

            if (foo) { }
            else if (bar) { }
        }
    }

    void IfElseIfElse(string[] items, bool foo, bool bar)
    {
        foreach (var item in items)
        {
            DoSomething();

            if (foo) { }
            else if (bar) { }
            else { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        [TestMethod]
        public async Task SMA8030_Compliant_EarlyYieldBreak()
        {
            var test = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M(bool invalid, int count)
    {
        if (invalid) yield break;
        if (count <= 0) yield break;

        int a = 1;
        yield return a;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_YieldBreakInMidFlowIf()
        {
            var test = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M(bool foo)
    {
        int count = 0;
        count++;

        if (foo)
        {
            {|#0:yield|} break;
        }

        yield return count;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_YieldBreakInAllIfElseBranches()
        {
            var test = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M(bool foo, bool bar)
    {
        int count = 0;
        count++;

        if (foo)
        {
            yield break;
        }
        else if (bar)
        {
            yield return 1;
        }
        else
        {
            yield break;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_EarlyBreakAndGoto()
        {
            var test = @"
class C
{
    void M(bool cond1, bool cond2)
    {
        for (int i = 0; i < 10; i++)
        {
            if (cond1) break;

            int x = i;
            x++;
        }

        while (cond1)
        {
            if (cond2) goto END;

            int y = 0;
            y++;
        }

    END:
        return;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_MidFlowBreakInLoop()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(bool cond)
    {
        for (int i = 0; i < 10; i++)
        {
            int x = i;
            DoSomething(x);

            if (cond)
            {
                {|#0:break|};
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Violation_MidFlowGotoInMethod()
        {
            var test = @"
class C
{
    void DoSomething(int x) { }

    void M(bool cond)
    {
        int x = 10;
        DoSomething(x);

        if (cond)
        {
            {|#0:goto|} TARGET;
        }

    TARGET:
        return;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_NestedLoopInsideIfStatement()
        {
            var test = @"
class C
{
    void M(bool foo)
    {
        if (foo)
        {
        }
        else
        {
            while (true)
            {
                if (true)
                {
                    break;
                }
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_EarlyReturn_IncompleteIf()
        {
            var test = @"
class C
{
    void M1(bool foo)
    {
        if (foo)
        {
            // skip
        }
        else
        {
            return;
        }

        int x = 1;
        x++;
    }

    void M2(bool foo, bool bar)
    {
        if (foo)
        {
            // skip
        }
        else if (bar)
        {
            return;
        }

        int x = 1;
        x++;
    }

    void M3(bool foo, bool bar)
    {
        if (foo)
        {
            // skip
        }
        else if (bar)
        {
            // skip
        }
        else
        {
            return;
        }

        int x = 1;
        x++;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_RepeatedDeclarationAfterIfStartsMainFlow()
        {
            var test = @"
class C
{
    int M(bool earlyExit, bool foo)
    {
        var x = 1;
        var y = 2;
        if (earlyExit) return 0;

        var z = 10;
        var w = 20;

        if (foo)
        {
            {|#0:return|} 1;
        }

        return z + w;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_NestedBlockRepeatedDeclarationsBeforeIf()
        {
            var test = @"
class C
{
    void Foo(bool cond1, bool cond2)
    {
        var x = 1;
        var y = 2;
        if (cond1) return;

        {
            var z = 10;
            var w = 20;
            if (cond2) return;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_SuppressionComment_EarlyExit()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo)
    {
        if (invalid) return 0;

        int x = 10;
        x++;

        // Early exit
        if (foo)
        {
            return 1;
        }

        return 3;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Compliant_SuppressionComment_CaseInsensitive()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo, bool bar)
    {
        if (invalid) return 0;

        int x = 10;
        x++;

        // early exit
        if (foo)
        {
            return 1;
        }

        // EARLY EXIT: Reason can be omitted.
        if (bar)
        {
            return 2;
        }

        return 3;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8030_Violation_SuppressionComment_WhitespaceMismatch()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo)
    {
        if (invalid) return 0;

        int x = 10;
        x++;

        //Early exit
        if (foo)
        {
            {|#0:return|} 1;
        }

        return 3;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Violation_SuppressionComment_NotFirstComment()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo)
    {
        if (invalid) return 0;

        int x = 10;
        x++;

        // regular comment
        // Early exit
        if (foo)
        {
            {|#0:return|} 1;
        }

        return 3;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Violation_SuppressThirdIfStatementOnly()
        {
            var test = @"
class C
{
    int M(bool invalid, bool foo, bool bar, bool baz)
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

        // Early exit
        if (baz)
        {
            return 3;
        }

        return 4;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(0),
                VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_MidFlowBranch).WithLocation(1));
        }
    }
}
