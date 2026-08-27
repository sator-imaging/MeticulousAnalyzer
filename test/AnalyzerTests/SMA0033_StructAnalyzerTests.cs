// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.StructAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0033_StructAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0033_Violation_MissingMoveMethod()
        {
            var test = @"
namespace Test
{
    struct {|#0:MoveOnlyStruct|}
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidNoCopyType)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct", "does not have a public parameterless Move() method returning itself");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0033_Violation_ClassNotStruct()
        {
            var test = @"
namespace Test
{
    class {|#0:MoveOnlyClass|}
    {
        public MoveOnlyClass Move() => this;
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidNoCopyType)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyClass", "is not a struct or record struct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0033_Compliant_ValidMoveOnlyStruct()
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
        public async Task SMA0034_Violation_PassByValueWithoutMove()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Foo(MoveOnlyStruct item) { }

        void Bar(MoveOnlyStruct moveOnly)
        {
            Foo({|#0:moveOnly|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_PassByValueWithMove()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Foo(MoveOnlyStruct item) { }

        void Bar(MoveOnlyStruct moveOnly)
        {
            Foo(moveOnly.Move());
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_PassByRefAndReturn()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Foo(in MoveOnlyStruct a, ref MoveOnlyStruct b, out MoveOnlyStruct c)
        {
            c = default;
        }

        MoveOnlyStruct Bar(MoveOnlyStruct moveOnly)
        {
            return moveOnly;
        }

        MoveOnlyStruct ExpressionBody(MoveOnlyStruct moveOnly) => moveOnly;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Violation_LocalAndFieldAssignment()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        private MoveOnlyStruct _field;

        void Method(MoveOnlyStruct moveOnly)
        {
            var local = {|#0:moveOnly|};
            _field = {|#1:moveOnly|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0035_Violation_RefOutInAsyncMethod()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Foo(ref MoveOnlyStruct item) { }

        async Task BarAsync(MoveOnlyStruct moveOnly)
        {
            Foo({|#0:ref moveOnly|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
