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
    public class SMA0040_DisposableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0040_Violation_SimpleDisposable_Using()
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
        public async Task SMA0040_Compliant_SimpleDisposable_WithUsing()
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
        public async Task SMA0040_Violation_GenericDisposable_Using()
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
                .WithArguments("MyDisposable<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_GenericDisposable_WithUsing()
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
        public async Task SMA0040_Violation_NestedDisposable_Using()
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
                .WithArguments("Outer.NestedDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_NestedDisposable_WithUsing()
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
        public async Task SMA0040_Violation_AsyncDisposable_Using()
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
        public async Task SMA0040_Compliant_AsyncDisposable_WithUsing()
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
        public async Task SMA0040_Compliant_NullAssignment_WithDispose()
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
        public async Task SMA0040_Compliant_NullAssignment_WithConditionalDispose()
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
        public async Task SMA0040_Compliant_Disposable_NullPattern()
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
        public async Task SMA0040_Compliant_InterlockedExchange()
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
        public async Task SMA0040_Compliant_InterlockedCompareExchange()
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
        public async Task SMA0040_Compliant_NullAssignmentAfterDisposeWithInterveningComment()
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
        public async Task SMA0040_Violation_ThrowsOnSomePaths()
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
        public async Task SMA0040_Compliant_PropertyGetter_ReturnedOnAllPaths()
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
        public async Task SMA0040_Compliant_ReturnedOnAllPaths()
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
        public async Task SMA0040_Violation_IteratorMethod_Disposed()
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
        public async Task SMA0040_Compliant_ForEach_IEnumerable()
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
        public async Task SMA0040_Compliant_Comment()
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
        public async Task SMA0040_Violation_OtherComment()
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
        public async Task SMA0040_Compliant_Comment_CaseInsensitive()
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
        public async Task SMA0040_Compliant_CommentTwoLinesAbove()
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
        public async Task SMA0040_Violation_Comment_PassedAsArgument()
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
        public async Task SMA0040_Violation_Comment_UntrackedCast()
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
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_Comment_ComplexUntrackedCast()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(object value)
        {
            // Don't dispose
            var d = (object){|#0:(IDisposable)value|};
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
        public async Task SMA0040_Compliant_Comment_1()
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
        public async Task SMA0040_Violation_Assignment_Comment()
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
        public async Task SMA0040_Violation_Assignment_WithBlankLine()
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
        public async Task SMA0040_Violation_Assignment_BlankLine()
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
        public async Task SMA0040_Compliant_Comment_WithAdditionalText()
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
        public async Task SMA0040_Compliant_Comment_UntrackedCast()
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
        public async Task SMA0040_Compliant_MultipleSingleLineComments()
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
        public async Task SMA0040_Violation_Comment_FirstLine()
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
        public async Task SMA0040_Violation_PrecedingLineEndComment()
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
        public async Task SMA0040_Violation_Comment_BlankLineBetweenComments()
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
        public async Task SMA0040_Violation_VariableNamedUnderscore()
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
        public async Task SMA0040_Violation_AssignmentToVariableNamedUnderscore()
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
        public async Task SMA0040_Violation_DiscardWithoutComment()
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
        public async Task SMA0040_Compliant_DiscardWithComment()
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
        public async Task SMA0040_Compliant_CastAs_Comment()
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
        public async Task SMA0040_Violation_CastAs()
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
        public async Task SMA0040_Compliant_TernaryAssignmentToField()
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
        public async Task SMA0040_Compliant_TernaryInUsing()
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
        public async Task SMA0040_Compliant_TernaryInReturn()
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
        public async Task SMA0040_Violation_TernaryAssignmentToLocal_Ternary()
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
        public async Task SMA0040_Compliant_TernaryWithCastInUsing()
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
        public async Task SMA0040_Violation_TernaryWithCastInUsing()
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
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_TernaryWithFooAndCastInUsing_Cast()
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
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_TernaryWithFooAndCastInUsing_ReportsOnCast()
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
        public async Task SMA0040_Violation_TernaryWithFooAndCreationInUsing_Creation()
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
        public async Task SMA0040_Violation_TernaryWithCastAssignmentToLocal_Ternary()
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
        public async Task SMA0040_Violation_TernaryWithCreationAssignmentToLocal_Ternary()
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


        [TestMethod]
        public async Task SMA0040_Violation_MethodChaining_ReturnsDisposable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
        public MyDisposable GetSelf() => this;
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
        public async Task SMA0040_Compliant_PropertyChaining_ReturnsDisposable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
        public MyDisposable Self => this;
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            _ = d.Self.ToString();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_LocalArrayAssignment_NewInstance()
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
            IDisposable[] arr = new IDisposable[1];
            {|#1:arr[0]|} = {|#0:new MyDisposable()|};
        }
    }
}
";
            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 1)
                    .WithArguments("IDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_LocalListAdd_NewInstance()
        {
            var test = @"
using System;
using System.Collections.Generic;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }
    class Program
    {
        void Method()
        {
            List<IDisposable> list = new List<IDisposable>();
            list.Add({|#0:new MyDisposable()|});
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
        public async Task SMA0040_Violation_SwitchExpression_AssignmentToLocal()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }
    class Program
    {
        void Method(int value)
        {
            var d = {|#0:value switch
            {
                1 => new MyDisposable(),
                _ => null,
            }|};
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
        public async Task SMA0040_Violation_IfCondition_NewInstance()
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
            if ({|#0:new MyDisposable()|} != null)
            {
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
        public async Task SMA0040_Violation_WhileCondition_NewInstance()
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
            while ({|#0:new MyDisposable()|} != null)
            {
                break;
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
        public async Task SMA0040_Violation_DoWhileCondition_NewInstance()
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
            do { } while ({|#0:new MyDisposable()|} != null);
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
        public async Task SMA0040_Violation_GenericFactory_NotUsing()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }
    class Program
    {
        static T Create<T>() where T : new() => new T();
        void Method()
        {
            var d = {|#0:Create<MyDisposable>()|};
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
        public async Task SMA0040_Violation_AssignmentToNonDisposableField_NewInstance()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }
    class Program
    {
        object field;
        void Method()
        {
            field = {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Violation_ImplicitConversion_ToNonDisposable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
        public static implicit operator string(MyDisposable d) => """";
    }
    class Program
    {
        void Method()
        {
            string s = {|#0:new MyDisposable()|};
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
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_UntrackedCast_ToObject()
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
            _ = {|#1:(object){|#0:new MyDisposable()|}|};
        }
    }
}
";
            var expected = new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 1)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                    .WithLocation(markupKey: 0)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_UntrackedCast_FromObject()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }
    class Program
    {
        void Method(object o)
        {
            _ = {|#0:(MyDisposable)o|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
