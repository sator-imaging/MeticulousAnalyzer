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
    public class SMA0040_DisposableAnalyzerTests_ObjectInitializer
    {
        [TestMethod]
        public async Task SMA0040_Violation_ObjectInitializer_WithParens()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Value { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable() { Value = ""with ()"" }|};
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
        public async Task SMA0040_Violation_ObjectInitializer_WithoutParens()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Value { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable { Value = ""without ()"" }|};
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
        public async Task SMA0040_Violation_ObjectInitializer_Ternary()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Value { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method(bool condition)
        {
            var d = {|#0:condition ? new MyDisposable() { Value = ""with ()"" } : new MyDisposable() { Value = ""with ()"" }|};
            var d2 = {|#1:condition ? new MyDisposable { Value = ""without ()"" } : new MyDisposable { Value = ""without ()"" }|};
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

            // Redundant reporting in current implementation
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected0, expected1, expected1);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ObjectInitializer_CoalesceThrow_WithParens()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Value { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable() { Value = ""with ()"" } ?? throw new Exception()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");

            // Redundant reporting in current implementation
            await VerifyCS.VerifyAnalyzerAsync(test, expected, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ObjectInitializer_CoalesceThrow_WithoutParens()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Value { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable { Value = ""without ()"" } ?? throw new Exception()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");

            // Redundant reporting in current implementation
            await VerifyCS.VerifyAnalyzerAsync(test, expected, expected);
        }
    }
}
