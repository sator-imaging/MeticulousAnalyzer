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
    public class SMA0042_DisposableAnalyzerTests_ExpressionBody
    {
        [TestMethod]
        public async Task SMA0042_Compliant_ExpressionBodiedMethod_ReturnsLocal()
        {
            // Expression-bodied method returning a local variable - tests the expressionBody path
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

        [TestMethod]
        public async Task SMA0042_Compliant_MethodBody_WithThrowStatement()
        {
            // Method body containing a throw statement - triggers the throw check branch
            // which causes IsLocalVariableReturned to return false (generic diagnostic instead)
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

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0042_Compliant_MethodBody_WithThrowExpression()
        {
            // Method body containing a throw expression (in null-coalescing)
            // This triggers the body.DescendantNodes().Any(ThrowExpressionSyntax) check
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
        MyDisposable Method(string input)
        {
            var d = {|#0:new MyDisposable()|};
            var x = input ?? throw new ArgumentNullException(nameof(input));
            return d;
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
        public async Task SMA0042_Violation_NotAllPathsReturn_MultipleReturns()
        {
            // Multiple return statements where not all return the same variable
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
        MyDisposable? Method(int value)
        {
            var {|#0:d = new MyDisposable()|};
            if (value == 1)
            {
                return d;
            }
            else if (value == 2)
            {
                return new MyDisposable();
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
        public async Task SMA0042_Compliant_AccessorBody_ReturnsLocal()
        {
            // Accessor body that declares and returns a local variable on all paths
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
        MyDisposable Item
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
        public async Task SMA0042_Compliant_ExpressionBodiedAccessor_ThrowExpression()
        {
            // Expression-bodied accessor with throw expression - tests throw check in expressionBody
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
        MyDisposable Item => throw new NotImplementedException();
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0042_Compliant_AllPathsReturn_SingleReturn()
        {
            // Single return statement returning the local - all paths return it
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
    }
}
