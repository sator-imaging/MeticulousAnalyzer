// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MidFlowReturnAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8030_MidFlowReturnAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8030_Compliant_EarlyReturnsOnly()
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
        public async Task SMA8030_Compliant_DeconstructionLocalDeclarationsBeforeIfReturn()
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
        public async Task SMA8030_Violation_MidFlowReturnInIf()
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
                VerifyCS.Diagnostic(MidFlowReturnAnalyzer.RuleId).WithLocation(0),
                VerifyCS.Diagnostic(MidFlowReturnAnalyzer.RuleId).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_IfElseIfElseAllReturn()
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
        public async Task SMA8030_Violation_YieldReturnInMidFlowIf()
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
                VerifyCS.Diagnostic(MidFlowReturnAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_LocalDeclarationsBeforeIfReturn()
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
                VerifyCS.Diagnostic(MidFlowReturnAnalyzer.RuleId).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8030_Compliant_EarlyThrowInsteadOfReturn()
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
    }
}
