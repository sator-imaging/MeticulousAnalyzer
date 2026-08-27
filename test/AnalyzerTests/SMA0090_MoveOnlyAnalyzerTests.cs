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

        [TestMethod]
        public async Task SMA0091_Violation_TupleDeconstructionAssignment()
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
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0091_Violation_FieldPropertyLocalAndTupleAssignment()
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
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0091_Violation_PassByValueWithoutMove()
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
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0091_Compliant_PassByValueWithMove_ReturnCopy_OutParam()
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
        public async Task SMA0091_Compliant_PassByRef_SyncMethod()
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
        public async Task SMA0091_Compliant_PassingToConstructor_ClassAndStruct()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class ConsumerClass
    {
        public ConsumerClass(MoveOnlyStruct item) { }
    }

    struct ConsumerStruct
    {
        public ConsumerStruct(MoveOnlyStruct item) { }
    }

    class Program
    {
        void Method(MoveOnlyStruct moveOnly)
        {
            var c = new ConsumerClass(moveOnly.Move());
            var s = new ConsumerStruct(moveOnly.Move());
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0091_Compliant_FieldAndPropertyAssignmentInStructConstructor()
        {
            var test = @"
namespace Test
{
    struct OtherStruct { }

    struct MoveOnlyStruct
    {
        private OtherStruct _field;
        public OtherStruct Prop { get; set; }

        public MoveOnlyStruct(OtherStruct item)
        {
            _field = item;
            Prop = item;
        }

        public MoveOnlyStruct Move() => this;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0091_GenericMethodTests()
        {
            var test = @"
namespace Test
{
    public interface IFoo { }

    struct MoveOnlyStruct : IFoo
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void GenericNoConstraint<T>(T value) { }
        void GenericStructConstraint<T>(T value) where T : struct { }
        void GenericInterfaceConstraint<T>(T value) where T : IFoo { }
        void GenericBothConstraints<T>(T value) where T : struct, IFoo { }

        void Method(MoveOnlyStruct moveOnly)
        {
            GenericNoConstraint({|#0:moveOnly|});
            GenericStructConstraint({|#1:moveOnly|});
            GenericInterfaceConstraint({|#2:moveOnly|});
            GenericBothConstraints({|#3:moveOnly|});

            GenericNoConstraint(moveOnly.Move());
            GenericStructConstraint(moveOnly.Move());
            GenericInterfaceConstraint(moveOnly.Move());
            GenericBothConstraints(moveOnly.Move());
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 0).WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 1).WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 2).WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 3).WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0092_Compliant_InRefOutInAsyncMethod_AllowedConditions()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class ConsumerClass
    {
        public ConsumerClass(in MoveOnlyStruct item) { }
    }

    class Program
    {
        void SyncFoo(ref MoveOnlyStruct item) { }

        Task AsyncFoo(ref MoveOnlyStruct item)
        {
            return Task.CompletedTask;
        }

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            // Allowed 1: passing to constructor
            var c = new ConsumerClass(in moveOnly);

            // Allowed 2: passing to sync method
            SyncFoo(ref moveOnly);

            // Allowed 3: passing to async method that is awaited
            await AsyncFoo(ref moveOnly);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0092_Violation_RefOutInAsyncMethod_UnawaitedAsyncMethod()
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
        Task AsyncFoo(ref MoveOnlyStruct item)
        {
            return Task.CompletedTask;
        }

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            AsyncFoo({|#0:ref moveOnly|});
            await Task.CompletedTask;
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }
    }
}
