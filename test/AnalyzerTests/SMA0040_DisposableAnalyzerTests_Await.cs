// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_Await
    {
        [TestMethod]
        public async Task SMA0040_Violation_AwaitTask_Disposable_NotUsed()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        private MyDisposable _field = new MyDisposable();
        Task<MyDisposable> GetDisposableAsync() => Task.FromResult(_field);

        async Task Method()
        {
            var x = {|#0:await GetDisposableAsync()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_AwaitTask_Disposable_WithUsing()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        private MyDisposable _field = new MyDisposable();
        Task<MyDisposable> GetDisposableAsync() => Task.FromResult(_field);

        async Task Method()
        {
            using var x = await GetDisposableAsync();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
