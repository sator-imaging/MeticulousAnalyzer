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
    public class StructAnalyzerUnitTests
    {
        [TestMethod]
        public async Task SMA0030_Violate_InvalidStructConstructor_ReportsDiagnostic()
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
        public async Task SMA0030_Conform_ValidStructConstructor_ReportsNoDiagnostic()
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
        public async Task SMA0031_Violate_MutableStructField_ReportsDiagnostic()
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
        public async Task SMA0030_Conform_ReadonlyStructField_ReportsNoDiagnostic()
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
        public async Task SMA0030_Violate_GenericStruct_InvalidConstructor_ReportsDiagnostic()
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
                .WithArguments("MyStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0030_Violate_NestedStruct_InvalidConstructor_ReportsDiagnostic()
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
                .WithArguments("NestedStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0031_Violate_GenericStruct_MutableField_ReportsDiagnostic()
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
                .WithArguments("MutableStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0031_Violate_NestedStruct_MutableField_ReportsDiagnostic()
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
                .WithArguments("NestedStruct");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0030_Conform_EnumField_ReportsNoDiagnostic()
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
        public async Task SMA0030_Conform_BuiltinPrimitives_NoDiagnostic()
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
        public async Task SMA0030_Conform_ReadOnlyNullableInt_ShouldNotReportDiagnostic()
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
        public async Task SMA0030_Conform_ReadOnlyNullableReadOnlyStruct_ShouldNotReportDiagnostic()
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
        public async Task SMA0031_Violate_ReadOnlyNullableMutableStruct_ShouldReportDiagnostic()
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

        [TestMethod]
        public async Task SMA0032_Violate_ImplicitBoxing_AssignmentToObject_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Method()
        {
            object value = {|#0:1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ImplicitBoxing)
                .WithLocation(markupKey: 0)
                .WithArguments("int", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0032_Violate_ImplicitBoxing_MethodArgument_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Foo(object value) {}
        void Method()
        {
            Foo({|#0:2|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ImplicitBoxing)
                .WithLocation(markupKey: 0)
                .WithArguments("int", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0032_Violate_ImplicitBoxing_InterfaceArgument_ReportsDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    struct MyStructDisposable : IDisposable
    {
        public void Dispose() {}
    }

    class Program
    {
        void Bar(IDisposable d) {}
        void Method()
        {
            var structDisposable = new MyStructDisposable();
            Bar({|#0:structDisposable|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ImplicitBoxing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyStructDisposable", "IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0032_Violate_ImplicitBoxing_NullableIntToObject_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Method()
        {
            int? i = 1;
            object value = {|#0:i|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(StructAnalyzer.RuleId_ImplicitBoxing)
                .WithLocation(markupKey: 0)
                .WithArguments("int?", "object");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0030_Conform_ExplicitBoxing_NoDiagnostic()
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
        public async Task SMA0030_Conform_ImplicitBoxing_SuppressedByComment_NoDiagnostic()
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
