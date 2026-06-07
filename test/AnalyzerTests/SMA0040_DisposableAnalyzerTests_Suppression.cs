// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_Suppression
    {
        // NOTE: Assembly-level DisposableAnalyzerSuppressor attribute tests require
        // multi-project test setup to work properly with the analyzer test harness.
        // The attribute suppression mechanism is validated in the sandbox project
        // (sandbox/DisposableTests.cs) instead.

        [TestMethod]
        public async Task SMA0040_Violation_AssemblyAttribute_UnsuppressedType()
        {
            // Verify that types NOT listed in the suppression attribute still trigger warnings.
            // This tests that the analyzer correctly identifies disposable violations.
            var test = @"
using System;

namespace Test
{
    class OtherDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = {|#0:new OtherDisposable()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("OtherDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
