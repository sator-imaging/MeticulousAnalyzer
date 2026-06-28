// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_DuckTyping
    {
        private static async Task VerifyWithDuckTypingAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", @"
is_global = true
sator_imaging.duck_typing_recognition = enable
"));
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0040_Violation_DuckTyping_PublicDispose()
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
            await VerifyWithDuckTypingAsync(source, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_DuckTyping_InternalDispose()
        {
            var source = @"
using System;

namespace Test
{
    class DuckDisposableInternal
    {
        internal void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new DuckDisposableInternal()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DuckDisposableInternal");
            await VerifyWithDuckTypingAsync(source, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_DuckTyping_DisposeAsync()
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
        async Task Method()
        {
            var d = {|#0:new DuckAsyncDisposable()|};
            await Task.CompletedTask;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DuckAsyncDisposable");
            await VerifyWithDuckTypingAsync(source, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_DuckTyping_PrivateDispose()
        {
            var source = @"
using System;

namespace Test
{
    class DuckPrivateDispose
    {
        private void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = new DuckPrivateDispose();
        }
    }
}
";

            await VerifyWithDuckTypingAsync(source);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_DuckTyping_WithUsing()
        {
            var source = @"
using System;

namespace Test
{
    class DuckDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new DuckDisposable();
        }
    }
}
";

            await VerifyWithDuckTypingAsync(source);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_DuckTyping_DisposeAsyncWithParameter()
        {
            var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class DuckAsyncWithParam
    {
        public ValueTask DisposeAsync(CancellationToken ct) => default;
    }

    class Program
    {
        void Method()
        {
            var d = new DuckAsyncWithParam();
        }
    }
}
";

            // DisposeAsync with parameter (Parameters.Length != 0) should NOT be detected
            await VerifyWithDuckTypingAsync(source);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_DuckTyping_DisposeAsyncWrongReturnType()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class DuckAsyncWrongReturn
    {
        public Task DisposeAsync() => Task.CompletedTask;
    }

    class Program
    {
        void Method()
        {
            var d = new DuckAsyncWrongReturn();
        }
    }
}
";

            // DisposeAsync returning Task (not ValueTask) should NOT be detected
            await VerifyWithDuckTypingAsync(source);
        }
    }
}
