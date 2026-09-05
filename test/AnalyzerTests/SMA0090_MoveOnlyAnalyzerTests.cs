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
        public async Task SMA0090_Violation_MissingMoveMethod()
        {
            var test = @"
namespace Test
{
    struct {|#0:MoveOnlyStruct|} { }
}
";

            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0090_Violation_MoveMethodReturnsDifferentType()
        {
            var test = @"
namespace Test
{
    struct {|#0:MoveOnlyStruct|}
    {
        public int Move() => 0;
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
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

        [TestMethod]
        public async Task SMA0090_Violation_NoCopyAttribute_MissingMoveMethod()
        {
            var test = @"
using System;

namespace Test
{
    [AttributeUsage(AttributeTargets.Struct)]
    class NoCopyAttribute : Attribute { }

    [NoCopy]
    struct {|#0:CustomNoCopyStruct|} { }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 0)
                .WithArguments("CustomNoCopyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0090_Compliant_NoCopyAttribute_WithValidMoveMethod()
        {
            var test = @"
using System;

namespace Test
{
    [AttributeUsage(AttributeTargets.Struct)]
    class NoCopyAttribute : Attribute { }

    [NoCopy]
    struct CustomNoCopyStruct
    {
        public CustomNoCopyStruct Move()
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

        [TestMethod]
        public async Task SMA0090_Violation_CloneMethodDoesNotSatisfyMoveMethod()
        {
            var test = @"
namespace Test
{
    struct {|#0:MoveOnlyStruct|}
    {
        public MoveOnlyStruct Clone() => {|#1:this|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
