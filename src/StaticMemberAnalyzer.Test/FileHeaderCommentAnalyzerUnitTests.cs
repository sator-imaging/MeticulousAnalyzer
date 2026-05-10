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
    public class FileHeaderCommentAnalyzerUnitTests
    {
        [TestMethod]
        public async Task TestNoHeaderComment()
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
        public async Task TestWithNestedAndGenericTypes()
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
        public async Task TestWithSingleLineComment()
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
        public async Task TestWithMultiLineComment()
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
        public async Task TestStartsWithBlankLine()
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
        public async Task TestWithPragmaWarningDisable()
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
        public async Task TestWithMultiLineDocumentationComment()
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
        public async Task TestWithSingleLineDocumentationComment()
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
        public async Task TestFileNameContainsTest()
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
