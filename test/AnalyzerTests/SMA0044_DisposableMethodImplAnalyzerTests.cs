// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableMethodImplAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
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

    }
}
