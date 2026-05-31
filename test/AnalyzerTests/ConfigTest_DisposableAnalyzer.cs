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
    public class ConfigTest_DisposableAnalyzer
    {
        [TestMethod]
        public async Task SMA0040_Config_DuckTypingDisabled()
        {
            var source = @"
using System;

namespace Test
{
    class DuckDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = new DuckDisposable();
        }
    }
}
";

            // Without duck typing config, no violation
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
