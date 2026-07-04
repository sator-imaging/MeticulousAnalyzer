// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_FieldAndMisc
    {
        [TestMethod]
        public async Task SMA0040_Compliant_FieldAssignment_IndexerProperty()
        {
            var test = @"
using System;
using System.Collections.Generic;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        List<MyDisposable> _items = new List<MyDisposable> { null };

        void Method()
        {
            _items[0] = new MyDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ArrowBody_ReturnsDisposable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable CreateDisposable() => new MyDisposable();
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_UsingBlock_Statement()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using (var d = new MyDisposable())
            {
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_DisposableMethod_NotUsed()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Factory
    {
        public static MyDisposable Create() => new MyDisposable();
    }

    class Program
    {
        void Method()
        {
            {|#0:Factory.Create()|};
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
        public async Task SMA0040_Compliant_TaskReturn_NotDisposable()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        Task<int> GetTaskAsync() => Task.FromResult(42);

        async Task Method()
        {
            var t = GetTaskAsync();
            await t;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_InterlockedExchange_NewCreation()
        {
            var test = @"
using System;
using System.Threading;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable _field;

        void Method()
        {
            Interlocked.Exchange(ref _field, new MyDisposable());
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_NullSuppression_Parenthesized()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = (new MyDisposable())!;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_FieldDeclaration_WithComment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            // Don't dispose
            var d = new MyDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
