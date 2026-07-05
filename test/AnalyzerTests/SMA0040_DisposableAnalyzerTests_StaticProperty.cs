// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_StaticProperty
    {
        [TestMethod]
        public async Task SMA0040_Compliant_StaticProperty_Discard()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public static MyDisposable New => new MyDisposable();
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            _ = MyDisposable.New;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_StaticProperty_ToStringChain()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public static MyDisposable New => new MyDisposable();
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            _ = MyDisposable.New.ToString();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_StaticProperty_NullConditionalToString()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public static MyDisposable New => new MyDisposable();
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            _ = MyDisposable.New?.ToString();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
