// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_YieldReturn
    {
        [TestMethod]
        public async Task SMA0040_Compliant_YieldReturn_DisposableCreation()
        {
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
        IEnumerable<MyDisposable> Method()
        {
            yield return new MyDisposable();
        }

        IEnumerable<IDisposable> Method2()
        {
            yield return new MyDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_YieldReturn_DisposableMember()
        {
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
        MyDisposable DisposableProperty => new MyDisposable();
        MyDisposable GetDisposable() => new MyDisposable();

        IEnumerable<MyDisposable> Method()
        {
            yield return DisposableProperty;
        }

        IEnumerable<MyDisposable> Method2()
        {
            yield return GetDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
