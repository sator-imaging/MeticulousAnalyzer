// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MidFlowBranchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8031_StateChangeInEarlyReturnAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8031_Violation_SampleFromPrompt()
        {
            var test = @"
class C
{
    void M(string[] x)
    {
        for (int i = 0; i < x.Length; i++)
        {
            var left = x[i].ToLowerInvariant();
            if (left.Length == 0)
            {
                x = new string[] { ""a"" };
                {|#0:continue|};
            }
        }
    }
}";
            var expected = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_LocalReassignmentBeforeEarlyReturn()
        {
            var test = @"
class C
{
    int M(bool cond, int val)
    {
        if (cond)
        {
            val = 10;
            {|#0:return|} 0;
        }

        int a = 1;
        return a;
    }
}";
            var expected = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_MethodCallBeforeEarlyReturn()
        {
            var test = @"
class C
{
    void DoWork() { }

    int M(bool cond)
    {
        if (cond)
        {
            DoWork();
            {|#0:return|} 0;
        }

        return 1;
    }
}";
            var expected = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_ElseIfAndElseBlocks()
        {
            var test = @"
class C
{
    int M(bool cond1, bool cond2, int x)
    {
        if (cond1)
        {
            x = 1;
            {|#0:return|} 1;
        }
        else if (cond2)
        {
            x = 2;
            {|#1:return|} 2;
        }
        else
        {
            x = 3;
            {|#2:return|} 3;
        }

        return 0;
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(1);
            var expected2 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(2);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_OutParameterAssignment()
        {
            var test = @"
class C
{
    bool TryParse(string input, out int result)
    {
        if (input == null)
        {
            result = -1;
            return false;
        }

        result = input.Length;
        return true;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_DeclarationsAndEmptyStatements()
        {
            var test = @"
class C
{
    int M(bool cond)
    {
        if (cond)
        {
            ;
            int temp = 42;
            var (a, b) = (1, 2);
            return temp + a + b;
        }

        return 0;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Violation_RefParameterAssignment()
        {
            var test = @"
class C
{
    bool M(bool cond, ref int refParam, out int outParam)
    {
        if (cond)
        {
            outParam = 0;
            refParam = 10;
            {|#0:return|} false;
        }

        outParam = 1;
        return true;
    }
}";
            var expected = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_YieldAndThrowAndBreakAndGoto()
        {
            var test = @"
using System;
using System.Collections.Generic;

class C
{
    IEnumerable<int> YieldM(bool cond, int x)
    {
        if (cond)
        {
            x = 1;
            {|#0:yield|} return 1;
        }

        yield return 0;
    }

    void ThrowM(bool cond, int x)
    {
        if (cond)
        {
            x = 1;
            {|#1:throw|} new Exception();
        }
    }

    void BreakM(int x)
    {
        for (int i = 0; i < 10; i++)
        {
            if (i == 5)
            {
                x = 10;
                {|#2:break|};
            }
        }
    }

    void GotoM(bool cond, int x)
    {
        if (cond)
        {
            x = 10;
            {|#3:goto|} TARGET;
        }

    TARGET:
        return;
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(1);
            var expected2 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(2);
            var expected3 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(3);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }
    }
}
