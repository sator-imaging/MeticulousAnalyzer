// Licensed under the MIT License
// https://github.com/sator-imaging/CSharp-StaticFieldAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class DisposableAnalyzerFeatureTests
    {
        [TestMethod]
        public async Task SMA0040_SuppressedByComment()
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
        public async Task SMA0041_IsNotSuppressedByComment()
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
                    .WithLocation(0)
                    .WithArguments("MyDisposable"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NullAssignment)
                    .WithLocation(1)
                    .WithArguments("MyDisposable")
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_NotSuppressedByOtherComment()
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
                .WithLocation(0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_SuppressedByComment_CaseInsensitive()
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
        public async Task SMA0040_NotSuppressedByCommentTwoLinesAbove()
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

            var d = {|#0:new MyDisposable()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_NotSuppressedByComment_WhenPassedAsArgument()
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
                .WithLocation(0)
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0042_SuppressedByComment()
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
    }
}
