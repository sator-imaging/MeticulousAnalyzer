// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableMethodImplAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0044_DisposableMethodImplAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0044_Violation_MissingDispose()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|}
{
    private MyDisposable _field = new MyDisposable();
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass", "Dispose");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass", "IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0044_Violation_AsyncDisposable_MissingDisposeAsync()
        {
            var test = @"
using System;
using System.Threading.Tasks;

class MyAsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}

class {|#0:TestClass|}
{
    private MyAsyncDisposable _field = new MyAsyncDisposable();
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass", "DisposeAsync");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass", "IAsyncDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0044_Violation_AsyncDisposable_InvalidCandidateReturnTypes()
        {
            var test = @"
using System;
using System.Threading.Tasks;

class MyAsyncDisposable : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}

class {|#0:TestClass1|}
{
    private MyAsyncDisposable _field = new MyAsyncDisposable();
    public Task DisposeAsync() => Task.CompletedTask;
}

class {|#1:TestClass2|}
{
    private MyAsyncDisposable _field = new MyAsyncDisposable();
    public void DisposeAsync() {}
}

class {|#2:TestClass3|}
{
    private MyAsyncDisposable _field = new MyAsyncDisposable();
    public ValueTask<int> DisposeAsync() => default;
}

class {|#3:TestClass4|}
{
    private MyAsyncDisposable _field = new MyAsyncDisposable();
    public static ValueTask DisposeAsync() => default;
}

class {|#4:TestClass5|}
{
    private MyAsyncDisposable _field = new MyAsyncDisposable();
    public ValueTask DisposeAsync<T>() => default;
}";
            var expected0_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass1", "DisposeAsync");
            var expected0_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass1", "IAsyncDisposable");

            var expected1_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 1)
                .WithArguments("TestClass2", "DisposeAsync");
            var expected1_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 1)
                .WithArguments("TestClass2", "IAsyncDisposable");

            var expected2_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 2)
                .WithArguments("TestClass3", "DisposeAsync");
            var expected2_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 2)
                .WithArguments("TestClass3", "IAsyncDisposable");

            var expected3_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 3)
                .WithArguments("TestClass4", "DisposeAsync");
            var expected3_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 3)
                .WithArguments("TestClass4", "IAsyncDisposable");

            var expected4_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 4)
                .WithArguments("TestClass5", "DisposeAsync");
            var expected4_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 4)
                .WithArguments("TestClass5", "IAsyncDisposable");

            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expected0_impl, expected0_iface,
                expected1_impl, expected1_iface,
                expected2_impl, expected2_iface,
                expected3_impl, expected3_iface,
                expected4_impl, expected4_iface);
        }

        [TestMethod]
        public async Task SMA0044_Violation_Disposable_InvalidCandidateMethods()
        {
            var test = @"
using System;

class MyDisposable : IDisposable
{
    public void Dispose() {}
}

class {|#0:TestClass1|}
{
    private MyDisposable _field = new MyDisposable();
    public int Dispose() => 0;
}

class {|#1:TestClass2|}
{
    private MyDisposable _field = new MyDisposable();
    public static void Dispose() {}
}

class {|#2:TestClass3|}
{
    private MyDisposable _field = new MyDisposable();
    public void Dispose<T>() {}
}";
            var expected0_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass1", "Dispose");
            var expected0_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass1", "IDisposable");

            var expected1_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 1)
                .WithArguments("TestClass2", "Dispose");
            var expected1_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 1)
                .WithArguments("TestClass2", "IDisposable");

            var expected2_impl = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 2)
                .WithArguments("TestClass3", "Dispose");
            var expected2_iface = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 2)
                .WithArguments("TestClass3", "IDisposable");

            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expected0_impl, expected0_iface,
                expected1_impl, expected1_iface,
                expected2_impl, expected2_iface);
        }
    }
}
