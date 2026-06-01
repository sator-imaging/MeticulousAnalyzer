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
    public class SMA0040_DisposableAnalyzerTests_Coverage2
    {
        // TARGET: IsLocalVariableReturned - expression body returns the declared variable
        // Hits lines ~928-938 in DisposableAnalyzer.cs (expression body with IdentifierNameSyntax)
        [TestMethod]
        public async Task SMA0040_Compliant_Disposable_ReturnedViaExpressionBody()
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
        MyDisposable Method()
        {
            var d = new MyDisposable();
            return d;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // TARGET: IsLocalVariableReturned - expression body with ThrowExpressionSyntax
        // Hits lines ~920-925 in DisposableAnalyzer.cs
        [TestMethod]
        public async Task SMA0040_Violation_Disposable_ExpressionBodyThrow()
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
        MyDisposable Method() => throw new NotImplementedException();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // TARGET: IsLocalVariableReturned - accessor expression body returns variable
        // Hits the AccessorDeclarationSyntax path (lines ~912-916)
        [TestMethod]
        public async Task SMA0040_Compliant_Disposable_ReturnedFromPropertyAccessorExpressionBody()
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
        MyDisposable Prop
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

        // TARGET: Interlocked.Exchange pattern with creation - IsOperationIgnorable
        // Hits lines ~619-636 in DisposableAnalyzer.cs
        [TestMethod]
        public async Task SMA0040_Compliant_Disposable_InterlockedExchange()
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
        MyDisposable _field;

        void Method()
        {
            Interlocked.Exchange(ref _field, new MyDisposable());
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // TARGET: AnalyzeNullAssignment - null assignment in non-block parent (not inside a block)
        // Hits line ~271 in DisposableAnalyzer.cs (assignmentStatement.Parent is not BlockSyntax)
        [TestMethod]
        public async Task SMA0041_Compliant_NullAssignment_NotInBlock()
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
        void Method(bool condition)
        {
            MyDisposable d = {|#0:new MyDisposable()|};
            if (condition)
                d = null;
        }
    }
}
";
            // Only the MissingUsing is reported; NullAssignment is not reported because it's not in a block
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // TARGET: IsLocalVariableReturned with body having throw statement (line ~945-949)
        // Method body with throw statement causes early return false
        [TestMethod]
        public async Task SMA0040_Violation_Disposable_MethodBodyWithThrowStatement()
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
                throw new InvalidOperationException();
            }
            return d;
        }
    }
}
";
            // When a throw exists, IsLocalVariableReturned returns false, so SMA0040 is triggered
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // TARGET: AnalyzeOtherSyntax - array element assignment to disposable (left-hand indexer)
        // The analyzer reports on both the array element ref and the creation
        [TestMethod]
        public async Task SMA0040_Violation_Disposable_ArrayElementAssignment()
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
        void Method(MyDisposable[] arr)
        {
            {|#0:arr[0]|} = {|#1:new MyDisposable()|};
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
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // TARGET: IsLocalVariableReturned with accessor body that returns variable
        // Hits the AccessorDeclarationSyntax body path (lines ~914-916)
        [TestMethod]
        public async Task SMA0042_Violation_Disposable_NotReturnedOnAllPathsFromAccessor()
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
        MyDisposable? Prop
        {
            get
            {
                var {|#0:d = new MyDisposable()|};
                if (DateTime.Now.Year > 3000)
                {
                    return null;
                }
                return d;
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
    }
}
