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

        Task AsyncBar(out MoveOnlyStruct {|#3:item|})
        {
            item = default;
            return Task.CompletedTask;
        }

        Task AsyncBaz(in MoveOnlyStruct item)
        {
            return Task.CompletedTask;
        }

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            AsyncFoo({|#0:ref moveOnly|});
            AsyncBar({|#1:out moveOnly|});
            AsyncBaz({|#2:in moveOnly|});
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
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedOutParameter)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }
    }
}
