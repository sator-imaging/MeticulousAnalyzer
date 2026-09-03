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
using System;

class C
{
    int M(bool cond)
    {
        if (cond)
        {
            Console.Write(""a"");
            Console.Write(""b"");
            {|#0:return|} 0;
        }

        return 1;
    }
}";
            var expected = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_MultipleEarlyReturnIfBlocks()
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

        if (cond2)
        {
            x = 2;
            {|#1:return|} 2;
        }

        return 0;
    }
}";
            var expected0 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(0);
            var expected1 = VerifyCS.Diagnostic(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn).WithLocation(1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
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

        [TestMethod]
        public async Task SMA8031_Compliant_BlocklessIfAndElseIf()
        {
            var test = @"
class C
{
    int M(bool cond1, bool cond2)
    {
        if (cond1) return 1;
        else if (cond2) return 2;
        else return 3;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_StateChangingExpressionInReturnAndThrow()
        {
            var test = @"
using System;

class C
{
    int M1(bool cond, int val)
    {
        if (cond)
        {
            return (val = 42);
        }

        return 0;
    }

    int M2(bool cond, int val)
    {
        if (cond) return (val = 42);
        return 0;
    }

    void M3(bool cond, Exception ex)
    {
        if (cond)
        {
            throw (ex = new InvalidOperationException());
        }
    }

    void M4(bool cond, Exception ex)
    {
        if (cond) throw (ex = new InvalidOperationException());
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_StateChangingNonExitingEarlyBranch()
        {
            var test = @"
class C
{
    void M(bool cond, int x)
    {
        if (cond)
        {
            x = 10;
        }

        int a = 1;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_LocalVarAndSingleConsoleWriteBeforeReturn()
        {
            var test = @"
using System;

class C
{
    int M(bool cond)
    {
        if (cond)
        {
            int val = 42;
            Console.Write(""a"");
            return val;
        }

        return 0;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_SingleConsoleWriteBeforeReturn()
        {
            var test = @"
using System;

class C
{
    void M(bool cond)
    {
        if (cond)
        {
            Console.Write(""a"");
            return;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_LocalVarAndSingleConsoleWriteBeforeThrow()
        {
            var test = @"
using System;

class C
{
    void M(bool cond)
    {
        if (cond)
        {
            string msg = ""error"";
            Console.Write(msg);
            throw new InvalidOperationException(msg);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_SingleConsoleWriteBeforeThrow()
        {
            var test = @"
using System;

class C
{
    void M(bool cond)
    {
        if (cond)
        {
            Console.Write(""a"");
            throw new InvalidOperationException();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
