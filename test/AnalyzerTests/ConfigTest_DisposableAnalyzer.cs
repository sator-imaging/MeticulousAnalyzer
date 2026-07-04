// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class ConfigTest_DisposableAnalyzer
    {
        [TestMethod]
        public async Task SMA0040_Config_DuckTypingRecognition_Default_NoDiagnostic()
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

        [TestMethod]
        public async Task SMA0040_Config_DuckTypingRecognition_Enabled_Dispose_Diagnostic()
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
            var d = {|#0:new DuckDisposable()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DuckDisposable");

            await VerifyWithSettingsAsync(source, $"is_global = true\n{Core.Config_EnableDuckTypingRecognition} = enable", expected);
        }

        [TestMethod]
        public async Task SMA0040_Config_DuckTypingRecognition_Enabled_DisposeAsync_Diagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class DuckAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new DuckAsyncDisposable()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DuckAsyncDisposable");

            await VerifyWithSettingsAsync(source, $"is_global = true\n{Core.Config_EnableDuckTypingRecognition} = enable", expected);
        }

        [TestMethod]
        public async Task SMA0040_Config_DuckTypingRecognition_DisabledExplicitly_NoDiagnostic()
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
            await VerifyWithSettingsAsync(source, $"is_global = true\n{Core.Config_EnableDuckTypingRecognition} = disable");
        }

        private static async Task VerifyWithSettingsAsync(string source, string? configContent, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            if (configContent != null)
            {
                test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", configContent));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
