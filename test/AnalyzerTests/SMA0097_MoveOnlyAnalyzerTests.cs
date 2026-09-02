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

        [TestMethod]
        public async Task SMA0097_Violation_ReturnNewAndDefault_StatementAndArrow()
        {
            var test = MoveOnlyType + @"
class Program
{
    MoveOnlyStruct StatementNew()
    {
        return {|#0:new()|};
    }

    MoveOnlyStruct ArrowNew() => {|#1:new()|};

    MoveOnlyStruct StatementDefault()
    {
        return {|#2:default|};
    }

    MoveOnlyStruct ArrowDefault() => {|#3:default|};

    MoveOnlyStruct StatementExplicitNew()
    {
        return {|#4:new MoveOnlyStruct()|};
    }

    MoveOnlyStruct ArrowExplicitNew() => {|#5:new MoveOnlyStruct()|};

    MoveOnlyStruct StatementExplicitDefault()
    {
        return {|#6:default(MoveOnlyStruct)|};
    }

    MoveOnlyStruct ArrowExplicitDefault() => {|#7:default(MoveOnlyStruct)|};
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct");
            var expected4 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 4)
                .WithArguments("MoveOnlyStruct");
            var expected5 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 5)
                .WithArguments("MoveOnlyStruct");
            var expected6 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 6)
                .WithArguments("MoveOnlyStruct");
            var expected7 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 7)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7);
        }

        [TestMethod]
        public async Task SMA0097_Violation_ParameterLocalAndRefLocalReturn()
        {
            var test = MoveOnlyType + @"
class Program
{
    private MoveOnlyStruct _field;

    MoveOnlyStruct ReturnParam(MoveOnlyStruct param)
    {
        return {|#0:param|};
    }

    MoveOnlyStruct ReturnLocal(MoveOnlyStruct param)
    {
        var local = {|#1:param|};
        return {|#2:local|};
    }

    MoveOnlyStruct ReturnRefLocal()
    {
        ref var refLocal = ref {|#3:_field|};
        return {|#4:refLocal|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct");
            var expected4 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 4)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4);
        }
    }
}
