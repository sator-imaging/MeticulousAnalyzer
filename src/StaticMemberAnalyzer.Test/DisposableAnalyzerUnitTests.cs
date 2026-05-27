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
    public class DisposableAnalyzerUnitTests
    {
        [TestMethod]
        public async Task SMA0040_Violate_SimpleDisposable_WithoutUsing_ReportsDiagnostic()
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
        public async Task SMA0040_Conform_SimpleDisposable_WithUsing_ReportsNoDiagnostic()
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
            using var d = new MyDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_GenericDisposable_WithoutUsing_ReportsDiagnostic()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable<T> : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable<int>()|};
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
        public async Task SMA0040_Conform_GenericDisposable_WithUsing_ReportsNoDiagnostic()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable<T> : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable<int>();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_NestedDisposable_WithoutUsing_ReportsDiagnostic()
        {
            var test = @"
using System;

namespace Test
{
    class Outer
    {
        public class NestedDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    class Program
    {
        void Method()
        {
            var d = {|#0:new Outer.NestedDisposable()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("NestedDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Conform_NestedDisposable_WithUsing_ReportsNoDiagnostic()
        {
            var test = @"
using System;

namespace Test
{
    class Outer
    {
        public class NestedDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    class Program
    {
        void Method()
        {
            using var d = new Outer.NestedDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_AsyncDisposable_WithoutUsing_ReportsDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class MyAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            var d = {|#0:new MyAsyncDisposable()|};
            await Task.CompletedTask;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyAsyncDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Conform_AsyncDisposable_WithUsing_ReportsNoDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class MyAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        async Task Method()
        {
            await using var d = new MyAsyncDisposable();
            await Task.CompletedTask;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0041_Violate_NullAssignment_WithoutDispose_ReportsDiagnostic()
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
        public async Task SMA0040_Conform_NullAssignment_WithDispose_ReportsNoDiagnostic()
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
        public async Task SMA0040_Conform_NullAssignment_WithConditionalDispose_ReportsNoDiagnostic()
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
        public async Task SMA0040_Conform_Disposable_IsNullPattern_ReportsNoDiagnostic()
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
        MyDisposable MyProperty { get; }

        void Method()
        {
            if (MyProperty is null)
            {
                // no warning
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_InterlockedExchange_ReportsNoDiagnostic()
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
        private MyDisposable _disposable = new MyDisposable();

        void Method()
        {
            var oldDisposable = Interlocked.Exchange(ref _disposable, new MyDisposable());
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_InterlockedCompareExchange_ReportsNoDiagnostic()
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
        private MyDisposable _disposable = new MyDisposable();

        void Method()
        {
            var oldDisposable = Interlocked.CompareExchange(ref _disposable, new MyDisposable(), null);
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0041_Violate_DoubleNullAssignmentAfterDispose_ReportsDiagnosticOnSecondAssignment()
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
        public async Task SMA0040_Conform_NullAssignmentAfterDisposeWithInterveningComment_IsNotReported()
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

            // comment

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
        public async Task SMA0040_Violate_ThrowsOnSomePaths_ReportsDiagnostic()
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
        MyDisposable Method(bool condition)
        {
            var d = {|#0:new MyDisposable()|};
            if (condition)
            {
                return d;
            }
            else
            {
                throw new Exception();
            }
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
        public async Task SMA0040_Conform_PropertyGetter_ReturnedOnAllPaths_ReportsNoDiagnostic()
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
        MyDisposable MyProperty
        {
            get
            {
                var d = new MyDisposable();
                return d;
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0042_Violate_NotAllCodePathsReturn_ReportsDiagnostic()
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
        MyDisposable Method(bool condition)
        {
            var {|#0:d = new MyDisposable()|};
            var {|#1:other = new MyDisposable()|};

            if (condition)
            {
                return d;
            }
            else
            {
                return other;
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test, new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                    .WithLocation(markupKey: 0)
                    .WithArguments("d"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                    .WithLocation(markupKey: 1)
                    .WithArguments("other")
            });
        }

        [TestMethod]
        public async Task SMA0040_Conform_ReturnedOnAllPaths_ReportsNoDiagnostic()
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
        MyDisposable Method(bool condition)
        {
            var d = new MyDisposable();
            if (condition)
            {
                return d;
            }
            else
            {
                return d;
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0042_Violate_ReturnedOnSomePaths_ReportsDiagnostic()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable? Method(bool condition)
        {
            var {|#0:d = new MyDisposable()|};
            if (condition)
            {
                return d;
            }
            else
            {
                return null;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0042_Violate_ReturnedOnSomePaths_WithDefault_ReportsDiagnostic()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable? Method(bool condition)
        {
            var {|#0:d = new MyDisposable()|};
            if (condition)
            {
                return d;
            }
            else
            {
                return default;
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0042_Violate_NotAllCodePathsReturn_ObjectCreation_ReportsDiagnostic()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable? Method()
        {
            var {|#0:d = new MyDisposable()|};
            if (DateTime.Now.Year > 3000)
            {
                return new MyDisposable();
            }
            return d;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violate_IteratorMethod_NotDisposed_ReportsDiagnostic()
        {
            var test = @"
using System;
using System.Collections.Generic;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        IEnumerable<int> Method()
        {
            var d = {|#0:new MyDisposable()|};
            yield return 1;
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
        public async Task SMA0040_Conform_ForEach_IEnumerable_ReportsNoDiagnostic()
        {
            var test = @"
using System;
using System.Collections;
using System.Collections.Generic;

namespace Test
{
    class Iterator<T> : IEnumerable<T>, IEnumerator<T>
    {
        public IEnumerator<T> GetEnumerator() => (new List<T>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool MoveNext() => false;
        public T Current => default!;
        object IEnumerator.Current => default!;
        public void Dispose() { }
        public void Reset() { }
    }

    class Foo
    {
        public Iterator<int> MethodReturningEnumerator() => new Iterator<int>();
        public Iterator<int> EnumeratorProperty => new Iterator<int>();
    }

    class Program
    {
        void Method()
        {
            using var iterator = new Iterator<int>();
            foreach (var item in iterator)
            {
            }

            var foo = new Foo();
            foreach (var item in foo.MethodReturningEnumerator())
            {
            }
            foreach (var item in foo.EnumeratorProperty)
            {
            }

            var listOfFloat = new List<float>();
            foreach (var item in listOfFloat)
            {
            }

            var arrayOfString = new string[0];
            foreach (var item in arrayOfString)
            {
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
