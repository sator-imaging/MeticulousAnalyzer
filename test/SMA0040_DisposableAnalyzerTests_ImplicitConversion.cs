// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_ImplicitConversion
    {
        [TestMethod]
        public async Task SMA0040_Violation_ImplicitConversion_UsingVarToStringField()
        {
            var test = @"
using System;

namespace Test
{
    class ConvertibleDisposable : IDisposable
    {
        public void Dispose() { }
        public static implicit operator string(ConvertibleDisposable d) => string.Empty;
    }

    class Program
    {
        static string _field;

        void Method()
        {
            using var d = new ConvertibleDisposable();
            _field = {|#0:d|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("ConvertibleDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ImplicitConversion_UsingVarToLocalString()
        {
            var test = @"
using System;

namespace Test
{
    class ConvertibleDisposable : IDisposable
    {
        public void Dispose() { }
        public static implicit operator string(ConvertibleDisposable d) => string.Empty;
    }

    class Program
    {
        void Method()
        {
            using var d = new ConvertibleDisposable();
            string s = {|#0:d|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("ConvertibleDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ImplicitConversion_AssignmentToLocalString()
        {
            var test = @"
using System;

namespace Test
{
    class ConvertibleDisposable : IDisposable
    {
        public void Dispose() { }
        public static implicit operator string(ConvertibleDisposable d) => string.Empty;
    }

    class Program
    {
        void Method()
        {
            using var d = new ConvertibleDisposable();
            string s;
            s = {|#0:d|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("ConvertibleDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ImplicitConversion_AssignToIDisposableField()
        {
            var test = @"
using System;

namespace Test
{
    class ConvertibleDisposable : IDisposable
    {
        public void Dispose() { }
        public static implicit operator string(ConvertibleDisposable d) => string.Empty;
    }

    class Program
    {
        static IDisposable _field;

        void Method()
        {
            _field = new ConvertibleDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_Reassignment_LocalIDisposableVariable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            IDisposable d = {|#0:new MyDisposable()|};
            d = {|#1:new MyDisposable()|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ImplicitConversion_NewInstanceToNonDisposableField()
        {
            var test = @"
using System;

namespace Test
{
    class ConvertibleDisposable : IDisposable
    {
        public void Dispose() { }
        public static implicit operator string(ConvertibleDisposable d) => string.Empty;
    }

    class Program
    {
        static object _field;

        void Method()
        {
            _field = {|#0:new ConvertibleDisposable()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("ConvertibleDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
