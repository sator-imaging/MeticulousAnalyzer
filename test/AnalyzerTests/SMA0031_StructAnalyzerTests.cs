// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.StructAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0031_StructAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0031_Violation_MutableStructField()
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
        private readonly MutableStruct {|#0:_s|};
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidReadOnlyField)
                .WithLocation(markupKey: 0)
                .WithArguments("MutableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0031_Violation_GenericStruct_MutableField()
        {
            var test = @"
namespace Test
{
    struct MutableStruct<T>
    {
        public T X;
    }

    class Program
    {
        private readonly MutableStruct<int> {|#0:_s|};
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidReadOnlyField)
                .WithLocation(markupKey: 0)
                .WithArguments("MutableStruct<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0031_Violation_NestedStruct_MutableField()
        {
            var test = @"
namespace Test
{
    class Outer
    {
        public struct NestedStruct
        {
            public int X;
        }
    }

    class Program
    {
        private readonly Outer.NestedStruct {|#0:_s|};
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidReadOnlyField)
                .WithLocation(markupKey: 0)
                .WithArguments("Outer.NestedStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0031_Violation_ReadOnlyNullableMutableStruct()
        {
            var test = @"
namespace Test
{
    struct MyMutableStruct { }

    class Program
    {
        private readonly MyMutableStruct? {|#0:_s|};
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidReadOnlyField)
                .WithLocation(markupKey: 0)
                .WithArguments("MyMutableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
