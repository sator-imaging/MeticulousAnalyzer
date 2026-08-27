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
        private const string NoCopyAttributeDef = @"
using System;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class NoCopyAttribute : Attribute { }
";

        [TestMethod]
        public async Task SMA0033_Violation_MissingMoveMethod_OrReferenceType()
        {
            var test = NoCopyAttributeDef + @"
namespace Test
{
    [NoCopy]
    struct {|#0:MoveOnlyStruct|} { }

    [NoCopy]
    class {|#1:MoveOnlyClass|}
    {
        public MoveOnlyClass Move() => this;
    }

    [NoCopy]
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
using System;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class NoCopyAttribute : Attribute { }

namespace Test
{
    [NoCopy]
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Violation_FieldAssignment()
        {
            var test = NoCopyAttributeDef + @"
namespace Test
{
    [NoCopy]
    struct MoveOnly
    {
        public MoveOnly Move() => this;
    }

    class Program
    {
        private MoveOnly _field;

        void Method(MoveOnly moveOnly)
        {
            _field = {|#0:moveOnly|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_NoCopyValueCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnly");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0);
        }

        [TestMethod]
        public async Task SMA0034_Violation_PassByValue_And_AssignToLocal()
        {
            var test = NoCopyAttributeDef + @"
namespace Test
{
    [NoCopy]
    struct MoveOnly
    {
        public MoveOnly Move() => this;
    }

    class Program
    {
        void Foo(MoveOnly m) { }

        void Method(MoveOnly moveOnly)
        {
            Foo({|#0:moveOnly|});

            var {|#1:local|} = {|#2:moveOnly|};

            (MoveOnly a, int b) tuple = ({|#3:moveOnly|}, 42);
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_NoCopyValueCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnly");
            var expected2 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_NoCopyValueCopy)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnly");
            var expected3 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_NoCopyValueCopy)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnly");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_PassByValueWithMove_And_ReturnByCopy()
        {
            var test = NoCopyAttributeDef + @"
namespace Test
{
    [NoCopy]
    struct MoveOnly
    {
        public MoveOnly Move() => this;
    }

    class Program
    {
        void Foo(MoveOnly m) { }

        MoveOnly ReturnByCopy(MoveOnly moveOnly) => moveOnly;

        MoveOnly ReturnByCopy2(MoveOnly moveOnly)
        {
            return moveOnly;
        }

        void Method(MoveOnly moveOnly)
        {
            Foo(moveOnly.Move());
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_PassByRef_SyncMethod()
        {
            var test = NoCopyAttributeDef + @"
namespace Test
{
    [NoCopy]
    struct MoveOnly
    {
        public MoveOnly Move() => this;
    }

    class Program
    {
        void Foo(in MoveOnly input, ref MoveOnly rw, out MoveOnly output)
        {
            output = default;
        }

        void Method(MoveOnly moveOnly, MoveOnly moveOnlyResult)
        {
            Foo(in moveOnly, ref moveOnly, out moveOnlyResult);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0035_Compliant_PassByIn_InAsyncMethod()
        {
            var test = @"
using System;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class NoCopyAttribute : Attribute { }

namespace Test
{
    [NoCopy]
    struct MoveOnly
    {
        public MoveOnly Move() => this;
    }

    class Program
    {
        void Foo(in MoveOnly input) { }

        async Task MethodAsync(MoveOnly moveOnly)
        {
            Foo(in moveOnly);
            await Task.CompletedTask;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0035_Violation_RefOrOutInAsyncMethod()
        {
            var test = @"
using System;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class NoCopyAttribute : Attribute { }

namespace Test
{
    [NoCopy]
    struct MoveOnly
    {
        public MoveOnly Move() => this;
    }

    class Program
    {
        void Foo(ref MoveOnly rw, out MoveOnly output)
        {
            output = default;
        }

        async Task MethodAsync(MoveOnly moveOnly, MoveOnly moveOnlyResult)
        {
            Foo({|#0:ref moveOnly|}, {|#1:out moveOnlyResult|});
            await Task.CompletedTask;
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_AsyncRefOutNoCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnly");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_AsyncRefOutNoCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnly");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
