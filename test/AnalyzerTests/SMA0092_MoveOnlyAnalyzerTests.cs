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
    public class SMA0092_MoveOnlyAnalyzerTests
    {
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
        public async Task SMA0092_Violation_RefInAsyncMethod_UnawaitedTaskReturningMethod()
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

        Task AsyncBaz(in MoveOnlyStruct item)
        {
            return Task.CompletedTask;
        }

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            AsyncFoo({|#0:ref moveOnly|});
            AsyncBaz({|#1:in moveOnly|});
            await Task.CompletedTask;
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0092_Violation_UniTask()
        {
            var test = @"
using System.Threading.Tasks;

namespace Cysharp.Threading.Tasks
{
    public struct UniTask
    {
        public UniTaskAwaiter GetAwaiter() => new UniTaskAwaiter();
    }

    public struct UniTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted => true;
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
    }
}

namespace Test
{
    using Cysharp.Threading.Tasks;

    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        UniTask UniTaskFoo(ref MoveOnlyStruct item) => default;
        UniTask UniTaskBaz(in MoveOnlyStruct item) => default;

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            UniTaskFoo({|#0:ref moveOnly|});
            UniTaskBaz({|#1:in moveOnly|});

            await UniTaskFoo(ref moveOnly);
            await UniTaskBaz(in moveOnly);
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0092_Violation_GenericTaskAndValueTask()
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
        Task<int> GenericTaskFoo(ref MoveOnlyStruct item) => Task.FromResult(0);
        ValueTask<int> GenericValueTaskFoo(in MoveOnlyStruct item) => default;

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            GenericTaskFoo({|#0:ref moveOnly|});
            GenericValueTaskFoo({|#1:in moveOnly|});

            await GenericTaskFoo(ref moveOnly);
            await GenericValueTaskFoo(in moveOnly);
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0092_Violation_RefLocalParameterAndLocalVariableInAsyncMethod()
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
        private MoveOnlyStruct _field;

        Task AsyncFoo(ref MoveOnlyStruct item)
        {
            return Task.CompletedTask;
        }

        async Task MethodAsync(MoveOnlyStruct param)
        {
            var local = {|#0:param|};
            ref var refLocal = ref {|#1:_field|};

            AsyncFoo({|#2:ref param|});
            AsyncFoo({|#3:ref local|});
            AsyncFoo({|#4:ref refLocal|});

            await Task.CompletedTask;
        }
    }
}
";
            // CS8177: Async methods cannot have by-reference locals
            var expectedCompilerError = DiagnosticResult.CompilerError("CS8177")
                .WithSpan(23, 21, 23, 29);

            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct");
            var expected4 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 4)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expectedCompilerError, expected0, expected1, expected2, expected3, expected4);
        }
    }
}
