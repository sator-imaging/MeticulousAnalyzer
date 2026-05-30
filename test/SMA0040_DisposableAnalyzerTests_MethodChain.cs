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
    public class SMA0040_DisposableAnalyzerTests_MethodChain
    {
        [TestMethod]
        public async Task SMA0040_Compliant_PropertyChain_SelfProperty()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = d.Self;
            _ = d.Self.Self;
            _ = d.Self.Self.ToString();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyChain_NullConditional()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = d?.Self;
            _ = d?.Self?.Self;
            _ = d?.Self?.ToString();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_MethodChain_GetSelfToString()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public MyDisposable GetSelf() => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = {|#0:d.GetSelf()|}.ToString();
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
        public async Task SMA0040_Violation_MethodChain_GetSelfSelf()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public MyDisposable GetSelf() => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = {|#0:d.GetSelf()|}.Self;
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
        public async Task SMA0040_Violation_MethodChain_NullConditionalGetSelf()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public MyDisposable GetSelf() => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = {|#0:d.GetSelf()|};
            _ = {|#1:d.GetSelf()|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0040_Violation_MethodChain_DoubleGetSelf()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public MyDisposable GetSelf() => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = {|#0:d.GetSelf()|}.{|#1:GetSelf()|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithSpan(18, 17, 18, 38)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0040_Violation_MethodChain_NullConditionalTripleGetSelf()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public MyDisposable GetSelf() => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = d.GetSelf()?.GetSelf()?.GetSelf();
        }
    }
}
";

            // The analyzer reports on each method invocation in the chain that returns a disposable.
            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithSpan(18, 17, 18, 28)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithSpan(18, 29, 18, 39)
                .WithArguments("MyDisposable");
            var expected2 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithSpan(18, 40, 18, 50)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0040_Violation_MethodChain_PropertyThenGetSelf()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public MyDisposable GetSelf() => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = d?.Self.{|#0:GetSelf()|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithSpan(18, 19, 18, 24)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithSpan(18, 19, 18, 34)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
