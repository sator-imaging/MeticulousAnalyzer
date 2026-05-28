using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Threading;
using StaticMemberAnalyzer.Test;
using System.Linq;

// The custom test runner is used because the default roslyn verifier runs a
// `#pragma warning disable` test automatically, but this analyzer is not
// affected by `#pragma`.
using VerifyCS = StaticMemberAnalyzer.Test.FileHeaderCommentAnalyzerVerifier;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0050_AnalyzerTests
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

            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithSpan(path: "test.cs", startLine: 1, startColumn: 1, endLine: 1, endColumn: 14);
            var verifier = new CSharpAnalyzerVerifier<FileHeaderCommentAnalyzer>.Test
            {
                TestCode = test,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            verifier.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var document = project.Documents.First();
                return solution.WithDocumentFilePath(document.Id, filePath: "test.cs");
            });
            await verifier.RunAsync(CancellationToken.None);
        }
    }
}
