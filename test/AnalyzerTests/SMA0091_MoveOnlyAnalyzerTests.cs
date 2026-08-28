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
    public class SMA0091_MoveOnlyAnalyzerTests
    {
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
        public async Task SMA0091_Compliant_DefaultAndNewObjectAssignment()
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
        private MoveOnlyStruct _field1 = default;
        private MoveOnlyStruct _field2 = new MoveOnlyStruct();
        private MoveOnlyStruct _field3 = new();

        void Method()
        {
            MoveOnlyStruct a = default;
            MoveOnlyStruct b = default(MoveOnlyStruct);
            MoveOnlyStruct c = new MoveOnlyStruct();
            MoveOnlyStruct d = new();

            a = default;
            b = default(MoveOnlyStruct);
            c = new MoveOnlyStruct();
            d = new();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0091_Compliant_PublicMoveMethodExemptedFromChecks()
        {
            var test = @"
namespace Test
{
    struct MoveOnlyStruct
    {
        private void Helper(MoveOnlyStruct item) { }

        public MoveOnlyStruct Move()
        {
            var temp = this;
            Helper(temp);
            this = default;
            return temp;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0091_Violation_ConstructorArgumentWithoutMove()
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
            var c = new ConsumerClass({|#0:moveOnly|});
            var s = new ConsumerStruct({|#1:moveOnly|});
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

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
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
    }
}
