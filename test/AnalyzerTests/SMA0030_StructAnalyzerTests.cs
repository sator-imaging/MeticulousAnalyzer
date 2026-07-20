// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.StructAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0030_StructAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0030_Violation_InvalidStructConstructor()
        {
            var test = @"
namespace Test
{
    struct MyStruct
    {
        public MyStruct(int x) { }
    }

    class Program
    {
        void Method()
        {
            var s = {|#0:new MyStruct()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidStructCtor)
                .WithLocation(markupKey: 0)
                .WithArguments("MyStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_ValidStructConstructor()
        {
            var test = @"
namespace Test
{
    struct MyStruct
    {
        public MyStruct(int x) { }
    }

    class Program
    {
        void Method()
        {
            var s = new MyStruct(1);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_ReadonlyStructField()
        {
            var test = @"
namespace Test
{
    readonly struct ReadonlyStruct
    {
        public readonly int X;
    }

    class Program
    {
        private readonly ReadonlyStruct _s;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Violation_GenericStruct_InvalidConstructor()
        {
            var test = @"
namespace Test
{
    struct MyStruct<T>
    {
        public MyStruct(T x) { }
    }

    class Program
    {
        void Method()
        {
            var s = {|#0:new MyStruct<int>()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidStructCtor)
                .WithLocation(markupKey: 0)
                .WithArguments("MyStruct<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0030_Violation_NestedStruct_InvalidConstructor()
        {
            var test = @"
namespace Test
{
    class Outer
    {
        public struct NestedStruct
        {
            public NestedStruct(int x) { }
        }
    }

    class Program
    {
        void Method()
        {
            var s = {|#0:new Outer.NestedStruct()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_InvalidStructCtor)
                .WithLocation(markupKey: 0)
                .WithArguments("Outer.NestedStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_EnumField()
        {
            var test = @"
    namespace Test
    {
        public enum MyEnum { A, B, C }

        class Program
        {
            private readonly MyEnum _e;
        }
    }
    ";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_BuiltinPrimitives()
        {
            var test = @"
namespace Test
{
    using System;

    struct MutableStruct_ReadOnlyPrimitifeFields
    {
        readonly int integer;
        readonly float number;
        readonly DateTime temporal;
        readonly string text;
    }

    class Class_ReadOnlyPrimitifeFields
    {
        readonly int integer;
        readonly float number;
        readonly DateTime temporal;
        readonly string text;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_ReadOnlyNullableInt()
        {
            var test = @"
namespace Test
{
    class Program
    {
        private readonly int? _i;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_ReadOnlyNullableReadOnlyStruct()
        {
            var test = @"
namespace Test
{
    readonly struct MyReadOnlyStruct { }

    class Program
    {
        private readonly MyReadOnlyStruct? _s;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_ExplicitBoxing()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Method()
        {
            object value = (object)1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0030_Compliant_ImplicitBoxing_Comment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Method()
        {
            // Allow boxing
            object value = 1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
