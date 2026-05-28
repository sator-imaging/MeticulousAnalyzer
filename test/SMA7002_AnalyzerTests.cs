// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7002_AnalyzerTests
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
    }
}
