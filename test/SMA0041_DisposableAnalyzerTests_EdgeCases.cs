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
    public class SMA0041_DisposableAnalyzerTests_EdgeCases
    {
        [TestMethod]
        public async Task SMA0041_Violation_NullAssignment_PrecedingNonDisposeMethod()
        {
            // Preceding statement calls Close() instead of Dispose() - should still report
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
        public void Close() { }
    }

    class Program
    {
        void Method()
        {
            MyDisposable d = {|#0:new MyDisposable()|};
            d.Close();
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
        public async Task SMA0041_Violation_NullAssignment_PrecedingStatementIsDeclaration()
        {
            // Preceding statement is a variable declaration, not an ExpressionStatement
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
            var x = 42;
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
        public async Task SMA0041_Compliant_NullAssignment_NonDisposableType()
        {
            // Null assignment to a non-disposable type should not trigger SMA0041
            var test = @"
using System;

namespace Test
{
    class NotDisposable
    {
        public void Close() { }
    }

    class Program
    {
        void Method()
        {
            NotDisposable d = new NotDisposable();
            d = null;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0041_Violation_NullAssignment_ConditionalDisposeOnDifferentVariable()
        {
            // Preceding statement calls ?.Dispose() on a different variable
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
            other?.Dispose();
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

        [TestMethod]
        public async Task SMA0041_Suppressed_NullAssignment_ConditionalDisposeOnSameVariable()
        {
            // Preceding statement calls ?.Dispose() on the same variable - should be suppressed
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
        public async Task SMA0041_Violation_NullAssignment_PrecedingNonInvocationExpression()
        {
            // Preceding statement is an expression statement but not an invocation (e.g., increment)
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
        int _counter;
        void Method()
        {
            MyDisposable d = {|#0:new MyDisposable()|};
            _counter++;
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
        public async Task SMA0041_Violation_NullAssignment_FieldTarget()
        {
            // Null assignment to a field of disposable type
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
        MyDisposable _field;

        void Method()
        {
            {|#0:_field = null|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0041_Suppressed_NullAssignment_FieldTarget_WithPrecedingDispose()
        {
            // Null assignment to a field preceded by Dispose on same field
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
        MyDisposable _field;

        void Method()
        {
            _field.Dispose();
            _field = null;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
