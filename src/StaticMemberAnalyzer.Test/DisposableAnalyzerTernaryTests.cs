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
    public class DisposableAnalyzerTernaryTests
    {
        [TestMethod]
        public async Task TernaryAssignmentToField_ReportsNoDiagnostic()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        private IDisposable mut_disposable;
        void Method(bool isEmpty)
        {
            this.mut_disposable = isEmpty ? null : new MemoryStream();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TernaryInUsing_ReportsNoDiagnostic()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        void Method(bool isEmpty)
        {
            using var d = isEmpty ? null : new MemoryStream();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TernaryInReturn_ReportsNoDiagnostic()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        IDisposable Method(bool isEmpty)
        {
            return isEmpty ? null : new MemoryStream();
        }

        IDisposable Method2(bool isEmpty) => isEmpty ? null : new MemoryStream();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TernaryAssignmentToLocal_ReportsDiagnosticOnTernary()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        void Method(bool isEmpty)
        {
            IDisposable d;
            d = {|#0:isEmpty ? null : new MemoryStream()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MemoryStream");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TernaryWithCastInUsing_ReportsNoDiagnostic()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(object foo, bool isEmpty)
        {
            using var d = isEmpty ? null : foo as IDisposable;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TernaryWithFooAndCastInUsing_ReportsDiagnosticOnCast()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        IDisposable Foo(IDisposable d) => d;
        void Method(object bar, bool isEmpty)
        {
            using var d = isEmpty ? null : Foo({|#0:bar as IDisposable|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TernaryWithFooAndCreationInUsing_ReportsDiagnosticOnCreation()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        IDisposable Foo(IDisposable d) => d;
        void Method(bool isEmpty)
        {
            using var d = isEmpty ? null : Foo({|#0:new MemoryStream()|});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TernaryWithCastAssignmentToLocal_ReportsDiagnosticOnTernary()
        {
            var test = @"
using System;

namespace Test
{
    class Program
    {
        void Method(object foo, bool isEmpty)
        {
            var d = {|#0:isEmpty ? null : foo as IDisposable|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TernaryWithCreationAssignmentToLocal_ReportsDiagnosticOnTernary()
        {
            var test = @"
using System;
using System.IO;

namespace Test
{
    class Program
    {
        void Method(bool isEmpty)
        {
            var d = {|#0:isEmpty ? null : new MemoryStream()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MemoryStream");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
