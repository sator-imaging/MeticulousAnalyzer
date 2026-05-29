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
    public class SMA0041_DisposableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0041_Violation_NullAssignment_Dispose()
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
            var d = {|#0:new MyDisposable()|};
            {|#1:d = null|};
        }
    }
}
";

            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                    .WithLocation(markupKey: 1)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0041_Violation_DoubleNullAssignmentAfterDispose_SecondAssignment()
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
            var d = {|#0:new MyDisposable()|};

            d.Dispose();

            d = null;
            {|#1:d = null|};
        }
    }
}
";

            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                    .WithLocation(markupKey: 1)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0041_Violation_Comment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable()|};

            // Don't dispose
            {|#1:d = null|};
        }
    }
}
";
            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                    .WithLocation(markupKey: 1)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0041_Compliant_NullAssignment_PrecedingDispose()
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
            MyDisposable d = {|#0:new MyDisposable()|};
            d.Dispose();
            d = null;
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
        public async Task SMA0041_Compliant_NullAssignment_PrecedingConditionalDispose()
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
            MyDisposable d = {|#0:new MyDisposable()|};
            d?.Dispose();
            d = null;
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
        public async Task SMA0041_Violation_NullAssignment_FirstStatement()
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
            MyDisposable d = {|#0:new MyDisposable()|};
            {|#1:d = null|};
        }
    }
}
";

            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                    .WithLocation(markupKey: 1)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0041_Violation_NullAssignment_DifferentVariable()
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
            MyDisposable d = {|#0:new MyDisposable()|};
            MyDisposable other = {|#1:new MyDisposable()|};
            other.Dispose();
            {|#2:d = null|};
        }
    }
}
";

            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 1)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                    .WithLocation(markupKey: 2)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
