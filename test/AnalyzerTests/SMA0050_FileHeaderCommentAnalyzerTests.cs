using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Threading;
using MeticulousAnalyzer.Tests;
using System.Linq;

// The custom test runner is used because the default roslyn verifier runs a
// `#pragma warning disable` test automatically, but this analyzer is not
// affected by `#pragma`.
using VerifyCS = MeticulousAnalyzer.Tests.FileHeaderCommentAnalyzerVerifier;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0050_FileHeaderCommentAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0050_Violation_NoHeaderComment()
        {
            var test = @"{|#0:using System;|}

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0050_Violation_WithNestedAndGenericTypes()
        {
            var test = @"{|#0:using System;|}

namespace Test
{
    class MyClass<T>
    {
        class NestedClass { }
    }
}
";
            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0050_Compliant_WithSingleLineComment()
        {
            var test = @"// this is a comment
using System;

namespace Test
{
    class MyClass { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0050_Compliant_WithMultiLineComment()
        {
            var test = @"/* this is a multi-line comment */
using System;

namespace Test
{
    class MyClass { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0050_Violation_StartsWithBlankLine()
        {
            var test = @"
{|#0:using|} System;

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0050_Violation_WithPragmaWarningDisable()
        {
            var test = @"{|#0:#pragma warning disable CS8618|}

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0050_Compliant_WithMultiLineDocumentationComment()
        {
            var test = @"/** this is a multi-line comment */
using System;

namespace Test
{
    class MyClass { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0050_Compliant_WithSingleLineDocumentationComment()
        {
            var test = @"/// this is a single-line documentation comment
using System;

namespace Test
{
    class MyClass { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0050_Violation_FileNameContainsTest()
        {
            var test = @"{|#0:using System;|}

namespace Test
{
    class MyClass { }
}
";

            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(0);
            var verifier = new CSharpAnalyzerVerifier<FileHeaderCommentAnalyzer>.Test
            {
                TestState = { Sources = { ("test.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }
    }
}
