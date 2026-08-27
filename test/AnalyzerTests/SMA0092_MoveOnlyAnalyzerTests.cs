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
        Task AsyncFoo(ref MoveOnlyStruct item)
        {
            return Task.CompletedTask;
        }

        async Task MethodAsync(MoveOnlyStruct moveOnly)
        {
            // Allowed: constructor and awaited method call
            var c = new ConsumerClass(in moveOnly);
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
