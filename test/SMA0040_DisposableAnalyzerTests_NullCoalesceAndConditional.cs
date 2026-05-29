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
    public class SMA0040_DisposableAnalyzerTests_NullCoalesceAndConditional
    {
        [TestMethod]
        public async Task SMA0040_Compliant_ArrowReturn_InvocationReturningDisposable()
        {
            // Arrow-bodied method returning a disposable via invocation
            // Tests the IReturnOperation -> IBlockOperation -> IMethodBodyBaseOperation -> ExpressionBody branch
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Factory
    {
        public static MyDisposable Create() => new MyDisposable();
    }

    class Program
    {
        MyDisposable GetDisposable() => Factory.Create();
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_NullCoalescing_DisposableCreation()
        {
            // Null-coalescing with disposable - tests the UnwrapAllNullCoalesceOperation path
            // The diagnostic is reported on the new MyDisposable() creation in the right operand
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
        void Method(MyDisposable? existing)
        {
            var d = existing ?? {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Compliant_NullCoalescing_WithUsing()
        {
            // Null-coalescing with using keyword
            // The new MyDisposable() in the right operand still triggers SMA0040
            // because it's analyzed as a separate object creation operation
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
        void Method(MyDisposable? existing)
        {
            using var d = existing ?? {|#0:new MyDisposable()|};
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
        public async Task SMA0040_Compliant_SwitchExpression_WithUsing()
        {
            // Switch expression result assigned to using var
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
        void Method(int value)
        {
            using var d = value switch
            {
                1 => new MyDisposable(),
                _ => new MyDisposable(),
            };
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_SwitchExpression_WithoutUsing()
        {
            // Switch expression without using - tests ISwitchExpressionArmOperation path
            // Each arm with a new object creation reports a separate diagnostic
            // but both report at the switch expression location
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
        void Method(int value)
        {
            var d = {|#0:value switch
            {
                1 => new MyDisposable(),
                _ => new MyDisposable(),
            }|};
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
        public async Task SMA0040_Compliant_MemberDeclaration_FieldInitializer()
        {
            // Field initializer with disposable - tests MemberDeclarationSyntax branch
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
        MyDisposable _field = new MyDisposable();
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyReference_NonDisposableMemberAccess()
        {
            // Accessing a non-disposable property on a disposable - tests !IsDisposable(memberRefOp.Type) branch
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Name { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            var name = d.Name;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyReference_DisposablePropFollowedByNonDisposableMethod()
        {
            // disposable.DisposableProp.ToString() - tests the invocation chain with non-disposable return
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Inner { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            var s = d.Inner.ToString();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_InvocationReturn_NotInterlocked()
        {
            // Method invocation returning disposable assigned to field (non-Interlocked path)
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Factory
    {
        public static MyDisposable Create() => new MyDisposable();
    }

    class Program
    {
        MyDisposable _field;

        void Method()
        {
            _field = Factory.Create();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ForEach_DisposableEnumerator()
        {
            // foreach with method returning disposable enumerator - tests IForEachLoopOperation branch
            var test = @"
using System;
using System.Collections.Generic;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        IEnumerable<MyDisposable> GetItems() => new List<MyDisposable>();

        void Method()
        {
            foreach (var item in GetItems())
            {
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_Discard_WithoutComment()
        {
            // Discard assignment without suppression comment
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
        public async Task SMA0040_Compliant_Discard_WithComment()
        {
            // Discard assignment with suppression comment
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
            // Don't dispose
            _ = new MyDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
