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
    public class SMA0090_MoveOnlyAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0090_Violation_MissingMoveMethod_OrReferenceType()
        {
            var test = @"
namespace Test
{
    struct {|#0:MoveOnlyStruct|} { }

    class {|#1:MoveOnlyClass|}
    {
        public MoveOnlyClass Move() => this;
    }

    record {|#2:MoveOnlyRecord|}
    {
        public MoveOnlyRecord Move() => this;
    }
}
";

            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyClass");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyRecord");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0090_Compliant_WithValidMoveMethod()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move()
        {
            var ret = this;
            this = default;
            return ret;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
