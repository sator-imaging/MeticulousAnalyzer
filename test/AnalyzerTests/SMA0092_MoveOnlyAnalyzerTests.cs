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
