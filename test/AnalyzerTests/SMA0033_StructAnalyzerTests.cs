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
    public class SMA0033_StructAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0033_Violation_PassingNonReadonlyStructAsInArgument()
        {
            var test = @"
namespace Test
{
    struct MutableStruct
    {
        public int X;
    }

    class Program
    {
        void Process(in MutableStruct s) { }

        void Method()
        {
            var mutableStruct = new MutableStruct();
            Process({|#0:mutableStruct|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InArgumentDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MutableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0033_Violation_LocalFunctionInArgument()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int X; }

    class Program
    {
        void Method()
        {
            var s = new MutableStruct();
            void LocalFunc(in MutableStruct arg) { }
            LocalFunc({|#0:s|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InArgumentDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MutableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0033_Violation_DelegateInArgument()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int X; }

    delegate void InDelegate(in MutableStruct arg);

    class Program
    {
        void Method()
        {
            var s = new MutableStruct();
            InDelegate del = (in MutableStruct arg) => { };
            del({|#0:s|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InArgumentDefensiveCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MutableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0033_Compliant_PassingReadonlyStructAsInArgument()
        {
            var test = @"
namespace Test
{
    readonly struct ReadOnlyStruct
    {
        public readonly int X;
    }

    delegate void InDelegate(in ReadOnlyStruct s);

    class Program
    {
        void Process(in ReadOnlyStruct s) { }

        void Method()
        {
            var s = new ReadOnlyStruct();
            Process(s);

            void LocalFunc(in ReadOnlyStruct arg) { }
            LocalFunc(s);

            InDelegate del = (in ReadOnlyStruct arg) => { };
            del(s);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0033_Compliant_PassingPrimitiveAsInArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Process(in int x) { }

        void Method()
        {
            int val = 10;
            Process(val);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0033_Compliant_PassingByRefOrValue()
        {
            var test = @"
namespace Test
{
    struct MutableStruct
    {
        public int X;
    }

    class Program
    {
        void ProcessRef(ref MutableStruct s) { }
        void ProcessValue(MutableStruct s) { }

        void Method()
        {
            var s = new MutableStruct();
            ProcessRef(ref s);
            ProcessValue(s);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
