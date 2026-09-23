// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.StructAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0034_StructAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0034_Violation_RefReadonlyDefensiveCopy()
        {
            var test = @"
namespace Test
{
    struct S
    {
        public int X;
        public void Increment() { X++; }
    }

    class Program
    {
        private S _s;
        ref readonly S GetRef() => ref _s;

        void Method()
        {
            {|#0:ref readonly|} S s = ref GetRef();
            s.Increment();
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_RefReadonlyCallingReadonlyMethod()
        {
            var test = @"
namespace Test
{
    struct S
    {
        public int X;
        public readonly int GetX() => X;
    }

    class Program
    {
        private S _s;
        ref readonly S GetRef() => ref _s;

        void Method()
        {
            ref readonly S s = ref GetRef();
            s.GetX();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_RefReadonlyStructType()
        {
            var test = @"
namespace Test
{
    readonly struct ReadOnlyS
    {
        public readonly int X;
        public void Print() { }
    }

    class Program
    {
        private ReadOnlyS _s;
        ref readonly ReadOnlyS GetRef() => ref _s;

        void Method()
        {
            ref readonly ReadOnlyS s = ref GetRef();
            s.Print();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0034_Compliant_MutableRefVariable()
        {
            var test = @"
namespace Test
{
    struct S
    {
        public int X;
        public void Increment() { X++; }
    }

    class Program
    {
        private S _s;
        ref S GetRef() => ref _s;

        void Method()
        {
            ref S s = ref GetRef();
            s.Increment();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
