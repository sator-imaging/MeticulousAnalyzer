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
        public async Task SMA0092_RefLocalAndArcGetRef_AsyncMethodPassing()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    class Arc
    {
        private MoveOnlyStruct _value;
        public ref MoveOnlyStruct GetRef() => ref _value;
    }

    class Program
    {
        Task AsyncNoMod(MoveOnlyStruct item) => Task.CompletedTask;
        Task AsyncIn(in MoveOnlyStruct item) => Task.CompletedTask;
        Task AsyncRef(ref MoveOnlyStruct item) => Task.CompletedTask;

        void NonAsyncMethod(Arc arc)
        {
            ref var refLocal = ref arc.GetRef();

            // Passing to async method without async context:
            // arc.GetRef()
            AsyncNoMod({|#0:arc.GetRef()|});
            AsyncIn(in {|#1:arc.GetRef()|});
            AsyncRef(ref arc.GetRef());
            AsyncNoMod(arc.GetRef().Move());
            AsyncIn(arc.GetRef().Move());

            // ref local
            AsyncNoMod({|#2:refLocal|});
            AsyncIn(in {|#3:refLocal|});
            AsyncRef(ref {|#4:refLocal|});
            AsyncNoMod(refLocal.Move());
            AsyncIn(refLocal.Move());
        }

        async Task AsyncMethod(Arc arc)
        {
            // Unawaited async method call in async method:
            AsyncIn({|#5:in arc.GetRef()|});
            AsyncRef({|#6:ref arc.GetRef()|});

            // Awaited async method call in async method:
            await AsyncIn(in arc.GetRef());
            await AsyncRef(ref arc.GetRef());

            await AsyncNoMod(arc.GetRef().Move());
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 0).WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 1).WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 2).WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 3).WithArguments("MoveOnlyStruct");
            var expected4 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy).WithLocation(markupKey: 4).WithArguments("MoveOnlyStruct");
            var expected5 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync).WithLocation(markupKey: 5).WithArguments("MoveOnlyStruct");
            var expected6 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync).WithLocation(markupKey: 6).WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5, expected6);
        }
    }
}
