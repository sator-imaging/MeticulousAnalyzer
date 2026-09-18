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
            if ({|#0:d|} == null || d is IDisposable)
            {
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithLocation(markupKey: 0)
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
            switch ({|#0:new MyDisposable()|})
            {
                case MyDisposable x when {|#1:x|} != null:
                    break;
            }
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
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
            while ({|#0:d|} != null)
            {
                break;
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithLocation(markupKey: 0)
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
            do { } while ({|#0:d|} != null);
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithLocation(markupKey: 0)
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
            if ({|#1:d|} == null)
            {
            }
            else if ({|#0:new MyDisposable()|} != null)
            {
            }
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable", "object");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected2 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_CastFromDisposableToNonDisposable)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }
    }
}
