// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCs = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_TypeMatrix
    {
        // =================================================================
        // Derived class implementing IDisposable
        // =================================================================

        [TestMethod]
        public async Task SMA0040_Violation_DerivedDisposable_WithoutUsing()
        {
            var test = @"
using System;

namespace Test
{
    class DisposableBase : IDisposable { public void Dispose() { } }
    class DisposableDerived : DisposableBase { }

    class Program
    {
        void Method()
        {
            var d = {|#0:new DisposableDerived()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DisposableDerived");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_DerivedDisposable_WithUsing()
        {
            var test = @"
using System;

namespace Test
{
    class DisposableBase : IDisposable { public void Dispose() { } }
    class DisposableDerived : DisposableBase { }

    class Program
    {
        void Method()
        {
            using var d = new DisposableDerived();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_DerivedDisposable_GenericFactory()
        {
            var test = @"
using System;

namespace Test
{
    class DisposableBase : IDisposable { public void Dispose() { } }
    class DisposableDerived : DisposableBase { }

    class Program
    {
        static T Create<T>() where T : new() => new T();

        void Method()
        {
            var d = {|#0:Create<DisposableDerived>()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DisposableDerived");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // =================================================================
        // Struct implementing IDisposable
        // =================================================================

        [TestMethod]
        public async Task SMA0040_Violation_StructDisposable_WithoutUsing()
        {
            var test = @"
using System;

namespace Test
{
    struct DisposableStruct : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = {|#0:new DisposableStruct()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DisposableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_StructDisposable_WithUsing()
        {
            var test = @"
using System;

namespace Test
{
    struct DisposableStruct : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new DisposableStruct();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_StructDisposable_GenericFactory()
        {
            var test = @"
using System;

namespace Test
{
    struct DisposableStruct : IDisposable { public void Dispose() { } }

    class Program
    {
        static T Create<T>() where T : new() => new T();

        void Method()
        {
            var d = {|#0:Create<DisposableStruct>()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DisposableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // =================================================================
        // Struct implementing IAsyncDisposable
        // =================================================================

        [TestMethod]
        public async Task SMA0040_Violation_StructAsyncDisposable_WithoutUsing()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    struct AsyncDisposableStruct : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            var d = {|#0:new AsyncDisposableStruct()|};
            await Task.CompletedTask;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("AsyncDisposableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_StructAsyncDisposable_WithAwaitUsing()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    struct AsyncDisposableStruct : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            await using var d = new AsyncDisposableStruct();
            await Task.CompletedTask;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_StructAsyncDisposable_WithAwaitUsingBlock()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    struct AsyncDisposableStruct : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            await using (new AsyncDisposableStruct()) { }
            await Task.CompletedTask;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // =================================================================
        // Class implementing IAsyncDisposable (without IDisposable)
        // =================================================================

        [TestMethod]
        public async Task SMA0040_Violation_ClassAsyncDisposable_WithoutUsing()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class AsyncDisposableClass : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            var d = {|#0:new AsyncDisposableClass()|};
            await Task.CompletedTask;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("AsyncDisposableClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ClassAsyncDisposable_WithAwaitUsing()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class AsyncDisposableClass : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            await using var d = new AsyncDisposableClass();
            await Task.CompletedTask;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ClassAsyncDisposable_GenericFactory()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class AsyncDisposableClass : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        static T Create<T>() where T : new() => new T();

        async Task Method()
        {
            var d = {|#0:Create<AsyncDisposableClass>()|};
            await Task.CompletedTask;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("AsyncDisposableClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // NOTE: Ref struct with DisposeAsync duck-typing pattern requires C# 13+ (net9.0+)
        // to be used with 'await using' or 'using' in async contexts.
        // These patterns are validated in the sandbox project (sandbox/DisposableSandbox.cs) instead.
    }
}
