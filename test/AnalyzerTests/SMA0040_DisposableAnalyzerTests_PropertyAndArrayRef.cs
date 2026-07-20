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
    public class SMA0040_DisposableAnalyzerTests_PropertyAndArrayRef
    {
        [TestMethod]
        public async Task SMA0040_Violation_PropertyReference_DisposableProperty()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Container
    {
        public MyDisposable Item { get; set; }
    }

    class Program
    {
        void Method(bool flag, Container c)
        {
            var d = {|#0:flag ? c.Item : null|};
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
        public async Task SMA0040_Compliant_PropertyReference_RightHandSide()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Container
    {
        public MyDisposable Item { get; set; }
    }

    class Program
    {
        void Method()
        {
            var c = new Container();
            var other = new Container();
            other.Item = c.Item;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyReference_InEqualsValueClause()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Container
    {
        public MyDisposable Item { get; set; }
    }

    class Program
    {
        MyDisposable _field;

        void Method()
        {
            var c = new Container();
            using var d = c.Item;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ArrayElement_DisposableArray()
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
        void Method(bool flag, MyDisposable[] arr)
        {
            var d = {|#0:flag ? arr[0] : null|};
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
        public async Task SMA0040_Compliant_ArrayElement_RightHandSideAssignment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Container
    {
        public MyDisposable Item { get; set; }
    }

    class Program
    {
        void Method()
        {
            var arr = new MyDisposable[1];
            var c = new Container();
            c.Item = arr[0];
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyReference_NullConditionalChain()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Inner
    {
        public string Name { get; set; }
    }

    class Container
    {
        public Inner SubItem { get; set; }
    }

    class Program
    {
        void Method()
        {
            Container c = null;
            var name = c?.SubItem?.Name;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyReference_FieldAssignment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Container
    {
        public MyDisposable Item { get; set; }
    }

    class Program
    {
        MyDisposable _field;

        void Method()
        {
            var c = new Container();
            _field = c.Item;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
