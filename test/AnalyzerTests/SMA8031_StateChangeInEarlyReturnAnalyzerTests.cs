// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.StateChangeInEarlyReturnAnalyzer>;

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
    void M(string x)
    {
        for (int i = 0; i < x.Length; i++)
        {
            var left = x[i].ToString();
            if (left.Length == 0)
            {
                x = ""a"";
                {|#0:continue|};
            }
        }
    }
}";
            var expected = VerifyCS.Diagnostic(StateChangeInEarlyReturnAnalyzer.RuleId).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_EmptyStatementsAndDeclarations()
        {
            var test = @"
class C
{
    void M(string x)
    {
        for (int i = 0; i < x.Length; i++)
        {
            var left = x[i].ToString();
            if (left.Length == 0)
            {
                int temp = 0;
                ;
                continue;
            }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Compliant_OutParameterAssignment()
        {
            var test = @"
class C
{
    bool TryProcess(string input, out string result)
    {
        if (input == null)
        {
            result = string.Empty;
            return false;
        }
        result = input;
        return true;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8031_Violation_LocalAssignmentBeforeReturn()
        {
            var test = @"
class C
{
    bool Process(string input)
    {
        string state = """";
        if (input == null)
        {
            state = ""invalid"";
            {|#0:return|} false;
        }
        return true;
    }
}";
            var expected = VerifyCS.Diagnostic(StateChangeInEarlyReturnAnalyzer.RuleId).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_FieldAssignmentBeforeReturn()
        {
            var test = @"
class C
{
    private int _field;
    void M(bool cond)
    {
        if (cond)
        {
            _field = 10;
            {|#0:return|};
        }
    }
}";
            var expected = VerifyCS.Diagnostic(StateChangeInEarlyReturnAnalyzer.RuleId).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_RefParameterAssignmentBeforeReturn()
        {
            var test = @"
class C
{
    void M(ref int x, bool cond)
    {
        if (cond)
        {
            x = 10;
            {|#0:return|};
        }
    }
}";
            var expected = VerifyCS.Diagnostic(StateChangeInEarlyReturnAnalyzer.RuleId).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_TupleDeconstructionToOutParameters()
        {
            var test = @"
class C
{
    void M(out int x, out int y, bool cond)
    {
        if (cond)
        {
            (x, y) = (1, 2);
            {|#0:return|};
        }
        x = 0;
        y = 0;
    }
}";
            var expected = VerifyCS.Diagnostic(StateChangeInEarlyReturnAnalyzer.RuleId).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8031_Violation_MethodCallInEarlyReturnBlock()
        {
            var test = @"
class C
{
    void Log(string msg) { }
    void M(bool cond)
    {
        if (cond)
        {
            Log(""error"");
            {|#0:return|};
        }
    }
}";
            var expected = VerifyCS.Diagnostic(StateChangeInEarlyReturnAnalyzer.RuleId).WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
