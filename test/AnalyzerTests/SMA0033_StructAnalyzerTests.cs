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
        public async Task SMA0033_Violation_MissingMoveMethod_OrReferenceType()
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

            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyClass");
            var expected2 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_MissingMoveMethod)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyRecord");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0033_Compliant_WithValidMoveMethod()
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
        public async Task SMA0034_Violation_TupleDeconstructionAssignment()
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
        void Method(MoveOnlyStruct moveOnly, MoveOnlyStruct foo)
        {
            int bar;
            (foo, bar) = ({|#0:moveOnly|}, 42);
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0034_Violation_FieldPropertyLocalAndTupleAssignment()
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
        public MoveOnlyStruct Prop { get; set; }

        void Method(MoveOnlyStruct moveOnly)
        {
            var local = {|#0:moveOnly|};
            _field = {|#1:moveOnly|};
            Prop = {|#2:moveOnly|};
            (MoveOnlyStruct foo, int bar) tuple = ({|#3:moveOnly|}, 42);
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
            var expected2 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
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

        void Method(MoveOnlyStruct moveOnly)
        {
            Foo({|#0:moveOnly|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_PassByValueWithMove_ReturnCopy_OutParam()
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
        void OutFoo(out MoveOnlyStruct item) { item = default; }

        MoveOnlyStruct Bar(MoveOnlyStruct moveOnly)
        {
            return moveOnly;
        }

        MoveOnlyStruct ExpressionBody(MoveOnlyStruct moveOnly) => moveOnly;

        void Method(MoveOnlyStruct moveOnly)
        {
            Foo(moveOnly.Move());
            OutFoo(out MoveOnlyStruct outRes);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_PassByRef_SyncMethod()
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
        void Foo(in MoveOnlyStruct input, ref MoveOnlyStruct rw, out MoveOnlyStruct output)
        {
            output = default;
        }

        void Method(MoveOnlyStruct moveOnly, MoveOnlyStruct moveOnlyResult)
        {
            Foo(in moveOnly, ref moveOnly, out moveOnlyResult);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
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
        void Foo(ref MoveOnlyStruct item, out MoveOnlyStruct result)
        {
            result = default;
        }

        async Task BarAsync(MoveOnlyStruct moveOnly, MoveOnlyStruct moveOnlyResult)
        {
            Foo({|#0:ref moveOnly|}, {|#1:out moveOnlyResult|});
            await Task.CompletedTask;
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0035_Compliant_PassByIn_InAsyncMethod()
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
        void Foo(in MoveOnlyStruct input) { }

        async Task BarAsync(MoveOnlyStruct moveOnly)
        {
            Foo(in moveOnly);
            await Task.CompletedTask;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
