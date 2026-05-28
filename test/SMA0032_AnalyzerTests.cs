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
    public class SMA0032_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0032_Violation_ImplicitBoxing_AssignmentToObject()
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
        public async Task SMA0032_Violation_ImplicitBoxing_MethodArgument()
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
        public async Task SMA0032_Violation_ImplicitBoxing_InterfaceArgument()
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
        public async Task SMA0032_Violation_ImplicitBoxing_NullableIntToObject()
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
    }
}
