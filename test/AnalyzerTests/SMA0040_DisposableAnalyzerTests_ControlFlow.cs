// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_ControlFlow
    {
        [TestMethod]
        public async Task SMA0040_Compliant_IfCondition_ExistingVariable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            if (d == null || d is IDisposable)
            {
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithSpan(13, 17, 13, 18)
                .WithArguments("MyDisposable", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_SwitchStatement_ExistingVariable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            switch (d)
            {
                case MyDisposable x when x is IDisposable:
                    break;
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_SwitchStatement_NewInstance()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            switch (new MyDisposable())
            {
                case MyDisposable x when x != null:
                    break;
            }
        }
    }
}
";

            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithSpan(12, 21, 12, 39)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                    .WithSpan(14, 42, 14, 43)
                    .WithArguments("MyDisposable", "object"),
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_WhileCondition_ExistingVariable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            while (d != null)
            {
                break;
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithSpan(13, 20, 13, 21)
                .WithArguments("MyDisposable", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_DoWhileCondition_ExistingVariable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            do { } while (d != null);
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithSpan(13, 27, 13, 28)
                .WithArguments("MyDisposable", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_IfElseCondition_NewInstance()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            if (d == null)
            {
            }
            else if (new MyDisposable() != null)
            {
            }
        }
    }
}
";

            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                    .WithSpan(13, 17, 13, 18)
                    .WithArguments("MyDisposable", "object"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithSpan(16, 22, 16, 40)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                    .WithSpan(16, 22, 16, 40)
                    .WithArguments("MyDisposable", "object"),
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
