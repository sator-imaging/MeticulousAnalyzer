// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = global::MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7002_LambdaAnalyzerTests_EdgeCases
    {
        [TestMethod]
        public async Task SMA7002_Violation_ParenthesizedLambdaCapturingOuterScope()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int outerVar = 0;
        Action<int, int> a = {|#0:(x, y) =>|} { outerVar++; };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7002_Compliant_CapturingLambda_CommentOnDeclarationStatement()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int outerVar = 0;
        // Allow allocation
        Action<int, int> a = (x, y) => { outerVar++; };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7002_Violation_NestedLambda_InnerCaptures()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int localVar = 1;
        // Allow allocation
        Action outer = () =>
        {
            // The inner lambda captures localVar from enclosing scope
            Action inner = {|#0:() =>|} { localVar++; };
        };
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_LambdaAllocation).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
