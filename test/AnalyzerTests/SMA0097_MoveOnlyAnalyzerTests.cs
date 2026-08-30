// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MoveOnlyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0097_MoveOnlyAnalyzerTests
    {
        private const string MoveOnlyType = @"
struct MoveOnlyStruct
{
    public MoveOnlyStruct Move() => this;
}
";

        [TestMethod]
        public async Task SMA0097_Violation_ReturnWithoutMove_StatementAndArrow()
        {
            var test = MoveOnlyType + @"
class Program
{
    MoveOnlyStruct Statement(MoveOnlyStruct item)
    {
        return {|#0:item|};
    }

    MoveOnlyStruct Arrow(MoveOnlyStruct item) => {|#1:item|};
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0097_Violation_ReturnWithMove_StatementAndArrow()
        {
            var test = MoveOnlyType + @"
class Program
{
    MoveOnlyStruct Statement(MoveOnlyStruct item)
    {
        return {|#0:item.Move()|};
    }

    MoveOnlyStruct Arrow(MoveOnlyStruct item) => {|#1:item.Move()|};
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0097_Compliant_RefReturn_StatementAndArrow()
        {
            var test = MoveOnlyType + @"
class Program
{
    private MoveOnlyStruct _item;

    ref MoveOnlyStruct Statement()
    {
        return ref _item;
    }

    ref MoveOnlyStruct Arrow() => ref _item;
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
