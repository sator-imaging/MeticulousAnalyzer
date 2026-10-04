// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MoveOnlyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0095_MoveOnlyAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0095_Violation_LambdaCapturesMoveOnlyLocalVariable()
        {
            var test = @"
namespace Test
{
    using System;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Method(MoveOnlyStruct param)
        {
            MoveOnlyStruct moveOnly = default;
            Action act1 = () =>
            {
                var x = {|#0:moveOnly|}.Move();
            };
            Action act2 = () =>
            {
                var y = {|#1:param|}.Move();
            };
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedLambdaCapture)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedLambdaCapture)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0095_Violation_StaticLambdaCapturesMoveOnlyVariable()
        {
            var test = @"
namespace Test
{
    using System;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Method()
        {
            MoveOnlyStruct moveOnly = default;
            Action act = static () =>
            {
                var x = {|#0:moveOnly|}.Move();
            };
        }
    }
}
";
            var expectedCompilerError = DiagnosticResult.CompilerError("CS8820")
                .WithSpan(18, 25, 18, 33)
                .WithArguments("moveOnly");

            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedLambdaCapture)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expectedCompilerError, expected0);
        }

        [TestMethod]
        public async Task SMA0095_Compliant_LambdaParameterAndLocalVariable()
        {
            var test = @"
namespace Test
{
    using System;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Method()
        {
            Action<MoveOnlyStruct> act1 = state =>
            {
                var x = state.Move();
            };

            Action act2 = () =>
            {
                MoveOnlyStruct localInLambda = default;
                var y = localInLambda.Move();
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0095_Compliant_GenericAndObjectMethods()
        {
            var test = @"
namespace Test
{
    using System;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void SomeGeneric<TState>(Action<TState> act, TState state) { }
        void SomeObject(Action<object> act, object state) { }

        void Method(MoveOnlyStruct moveOnly)
        {
            SomeGeneric(state => { }, moveOnly.Move());
            SomeObject(state => { }, moveOnly.Move());
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0095_Violation_GenericAndObjectMethodsWithoutMove()
        {
            var test = @"
namespace Test
{
    using System;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void SomeGeneric<TState>(Action<TState> act, TState state) { }
        void SomeObject(Action<object> act, object state) { }

        void Method(MoveOnlyStruct moveOnly)
        {
            SomeGeneric(state => { }, {|#0:moveOnly|});
            SomeObject(state => { }, {|#1:moveOnly|});
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct", "object");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0095_Violation_CloneNotExemptFromLambdaCaptureCheck()
        {
            var test = @"
namespace Test
{
    using System;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
        public MoveOnlyStruct Clone() => {|#0:this|};
    }

    class Program
    {
        void Method(MoveOnlyStruct param)
        {
            Action act = () =>
            {
                var x = {|#1:{|#2:param|}.Clone()|};
            };
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedLambdaCapture)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }
    }
}
