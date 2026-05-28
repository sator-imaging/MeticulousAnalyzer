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
    public class SMA0040_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0040_Violate_SimpleDisposable_WithoutUsing()
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
        public async Task SMA0040_Conform_SimpleDisposable_WithUsing()
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
        public async Task SMA0040_Violate_GenericDisposable_WithoutUsing()
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
        public async Task SMA0040_Conform_GenericDisposable_WithUsing()
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
        public async Task SMA0040_Violate_NestedDisposable_WithoutUsing()
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
        public async Task SMA0040_Conform_NestedDisposable_WithUsing()
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
        public async Task SMA0040_Violate_AsyncDisposable_WithoutUsing()
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
        public async Task SMA0040_Conform_AsyncDisposable_WithUsing()
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
        public async Task SMA0040_Conform_NullAssignment_WithDispose()
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
        public async Task SMA0040_Conform_NullAssignment_WithConditionalDispose()
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
        public async Task SMA0040_Conform_Disposable_IsNullPattern()
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
        public async Task SMA0040_Conform_InterlockedExchange()
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
        public async Task SMA0040_Conform_InterlockedCompareExchange()
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
        public async Task SMA0040_Conform_NullAssignmentAfterDisposeWithInterveningComment()
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
        public async Task SMA0040_Violate_ThrowsOnSomePaths()
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
        public async Task SMA0040_Conform_PropertyGetter_ReturnedOnAllPaths()
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
        public async Task SMA0040_Conform_ReturnedOnAllPaths()
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
        public async Task SMA0040_Violate_IteratorMethod_NotDisposed()
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
        public async Task SMA0040_Conform_ForEach_IEnumerable()
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

        [TestMethod]
        public async Task SMA0040_Conform_SuppressedByComment()
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
            // Don't dispose
            var d = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_NotSuppressedByOtherComment()
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
            // Just a comment
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
        public async Task SMA0040_Conform_SuppressedByComment_CaseInsensitive()
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
            // don't DISPOSE
            var d = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_SuppressedByCommentTwoLinesAbove()
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
            // Don't dispose

            var d = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_NotSuppressedByComment_WhenPassedAsArgument()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void MethodTakingDisposable(IDisposable d) {}
        void Method()
        {
            // Don't dispose
            MethodTakingDisposable({|#0:new MyDisposable()|});
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
        public async Task SMA0040_Violate_NotSuppressedByComment_UntrackedCast()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(IDisposable value)
        {
            var d = {|#0:value as object|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("Object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Conform_IsSuppressedByComment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        MyDisposable Method(bool condition)
        {
            // Don't dispose
            var d = new MyDisposable();

            if (condition) return d;
            return null;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_Assignment_IsNotSuppressedByComment()
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
            IDisposable d;

            // Don't dispose
            d = {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Violate_Assignment_WithBlankLine_IsNotSuppressed()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void DoNothing() {}
        void Method()
        {
            DoNothing();  // Don't dispose

            // The following assignment must report error (expect the above comment is ignored)
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
        public async Task SMA0040_Violate_Assignment_WithoutBlankLine_IsNotSuppressed()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void DoNothing() {}
        void Method()
        {
            DoNothing();  // Don't dispose
            // The following assignment must report error (expect the above comment is ignored)
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
        public async Task SMA0040_Conform_SuppressedByComment_WithAdditionalText()
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
            // Don't dispose: Additional comment.
            var d = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_SuppressedByComment_UntrackedCast()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(IDisposable value)
        {
            // Don't dispose
            var d = value as object;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_SuppressedByMultipleSingleLineComments()
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
            // Don't dispose
            // - Because...
            var d = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_NotSuppressedByComment_WhenNotFirstLine()
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
            // NOTE:
            // Don't dispose here because...
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
        public async Task SMA0040_Violate_NotSuppressedByPrecedingLineEndComment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void DoSomething() {}
        void Method()
        {
            DoSomething();  // Don't dispose
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
        public async Task SMA0040_Violate_NotSuppressedByComment_WhenBlankLineBetweenComments()
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
            // Blah blah blah

            // Don't dispose
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
        public async Task SMA0040_Violate_VariableNamedUnderscore_IsNotDiscard()
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
            var _ = {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Violate_AssignmentToVariableNamedUnderscore_IsNotDiscard()
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
            MyDisposable _;

            // Don't dispose
            _ = {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Violate_DiscardWithoutComment_ReportsError()
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
            _ = {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Conform_DiscardWithComment_IsSuppressed()
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
            // Don't dispose
            _ = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_CastAs_SuppressedByComment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void Method(object disposable)
        {
            // Don't dispose
            var casted = disposable as IDisposable;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_CastAs_NotSuppressed()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() {} }
    class Program
    {
        void Method(object disposable)
        {
            var casted = {|#0:disposable as IDisposable|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Conform_TernaryAssignmentToField()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        private IDisposable mut_disposable;
        void Method(bool isEmpty)
        {
            this.mut_disposable = isEmpty ? null : new MemoryStream();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_TernaryInUsing()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        void Method(bool isEmpty)
        {
            using var d = isEmpty ? null : new MemoryStream();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Conform_TernaryInReturn()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        IDisposable Method(bool isEmpty)
        {
            return isEmpty ? null : new MemoryStream();
        }

        IDisposable Method2(bool isEmpty) => isEmpty ? null : new MemoryStream();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_TernaryAssignmentToLocal_ReportsDiagnosticOnTernary()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        void Method(bool isEmpty)
        {
            IDisposable d;
            d = {|#0:isEmpty ? null : new MemoryStream()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MemoryStream");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Conform_TernaryWithCastInUsing()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(object foo, bool isEmpty)
        {
            using var d = isEmpty ? null : foo as IDisposable;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_TernaryWithCastInUsing()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(IDisposable foo, bool isEmpty)
        {
            var d = isEmpty ? null : {|#0:foo as object|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("Object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violate_TernaryWithFooAndCastInUsing_ReportsDiagnosticOnCast()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        IDisposable Foo(object d) => (IDisposable)d;
        void Method(IDisposable bar, bool isEmpty)
        {
            using var d = isEmpty ? null : Foo({|#0:bar as object|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("Object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Conform_TernaryWithFooAndCastInUsing_ReportsNoDiagnosticOnCast()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        IDisposable Foo(IDisposable d) => d;
        void Method(object bar, bool isEmpty)
        {
            using var d = isEmpty ? null : Foo(bar as IDisposable);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violate_TernaryWithFooAndCreationInUsing_ReportsDiagnosticOnCreation()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        IDisposable Foo(IDisposable d) => d;
        void Method(bool isEmpty)
        {
            using var d = isEmpty ? null : Foo({|#0:new MemoryStream()|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MemoryStream");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violate_TernaryWithCastAssignmentToLocal_ReportsDiagnosticOnTernary()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(object foo, bool isEmpty)
        {
            var d = {|#0:isEmpty ? null : foo as IDisposable|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violate_TernaryWithCreationAssignmentToLocal_ReportsDiagnosticOnTernary()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        void Method(bool isEmpty)
        {
            var d = {|#0:isEmpty ? null : new MemoryStream()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MemoryStream");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
