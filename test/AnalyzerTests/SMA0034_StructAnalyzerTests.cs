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
        ref {|#0:readonly|} S GetRef() => ref _s;

        void Method()
        {
            ref {|#1:readonly|} S s = ref GetRef();
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("S");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0034_Violation_LocalFunctionRefReadonlyReturn()
        {
            var test = @"
namespace Test
{
    struct S { public int X; }

    class Program
    {
        private S _s;

        void Method()
        {
            ref {|#0:readonly|} S LocalGetRef() => ref _s;
            ref {|#1:readonly|} S s = ref LocalGetRef();
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("S");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0034_Violation_DelegateRefReadonlyReturn()
        {
            var test = @"
namespace Test
{
    struct S { public int X; }

    delegate ref {|#0:readonly|} S RefReadonlyDelegate();

    class Program
    {
        private S _s;

        void Method()
        {
            RefReadonlyDelegate del = () => ref _s;
            ref {|#1:readonly|} S s = ref del();
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("S");
            var expected1 = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0034_Violation_RefReadonlyIndexer()
        {
            var test = @"
namespace Test
{
    struct S
    {
        public int X;
    }

    class Program
    {
        private S _s;
        public ref {|#0:readonly|} S this[int index] => ref _s;
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_RefReadonlyDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
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

    delegate ref readonly ReadOnlyS RefReadonlyDel();

    class Program
    {
        private ReadOnlyS _s;
        ref readonly ReadOnlyS GetRef() => ref _s;

        void Method()
        {
            ref readonly ReadOnlyS s = ref GetRef();
            s.Print();

            ref readonly ReadOnlyS LocalGetRef() => ref _s;
            LocalGetRef().Print();
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
