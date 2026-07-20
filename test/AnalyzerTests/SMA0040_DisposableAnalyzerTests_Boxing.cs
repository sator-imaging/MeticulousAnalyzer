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
    public class SMA0040_DisposableAnalyzerTests_Boxing
    {
        [TestMethod]
        public async Task SMA0040_Violation_Boxing_UsingVarPassedToObjectParam()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static void MethodArg(object _) { }

        void Method()
        {
            using var d = new MyDisposable();
            MethodArg({|#0:d|});
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
        public async Task SMA0040_Violation_Boxing_FactoryResultPassedToObjectParam()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static void MethodArg(object _) { }
        static T Create<T>() where T : new() => new T();

        void Method()
        {
            using var d = Create<MyDisposable>();
            MethodArg({|#0:d|});
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
        public async Task SMA0040_Violation_Boxing_DerivedDisposablePassedToObjectParam()
        {
            var test = @"
using System;

namespace Test
{
    class DisposableBase : IDisposable { public void Dispose() { } }
    class DisposableDerived : DisposableBase { }

    class Program
    {
        static void MethodArg(object _) { }

        void Method()
        {
            using var d = new DisposableDerived();
            MethodArg({|#0:d|});
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
        public async Task SMA0040_Violation_Boxing_StructDisposablePassedToObjectParam()
        {
            var test = @"
using System;

namespace Test
{
    struct DisposableStruct : IDisposable { public void Dispose() { } }

    class Program
    {
        static void MethodArg(object _) { }

        void Method()
        {
            using var d = new DisposableStruct();
            MethodArg({|#0:d|});
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
        public async Task SMA0040_Compliant_Boxing_PassedToIDisposableParam()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static void Consume(IDisposable _) { }

        void Method()
        {
            using var d = new MyDisposable();
            Consume(d);
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_Boxing_AssignToNonDisposableField()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static object _field;

        void Method()
        {
            using var d = new MyDisposable();
            _field = {|#0:d|};
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
        public async Task SMA0040_Compliant_Boxing_AssignToIDisposableField()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static IDisposable _field;

        void Method()
        {
            using var d = new MyDisposable();
            _field = d;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
