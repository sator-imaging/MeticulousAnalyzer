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
                .WithArguments("TestClass");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0044_Compliant_SuppressionWithReason()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

// Don't dispose reason here
class TestClass
{
    private MyDisposable _field = new MyDisposable();
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0044_Violation_SuppressionWithoutReason()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

// Don't dispose
class {|#0:TestClass|}
{
    private MyDisposable _field = new MyDisposable();
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
        }

    }
}
