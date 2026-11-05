// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Threading;
using System.Linq;

// The custom test runner is used because the default roslyn verifier runs a
// `#pragma warning disable` test automatically, but this analyzer is not
// affected by `#pragma`.
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.FileHeaderCommentAnalyzerVerifier;

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

        [TestMethod]
        public async Task SMA0050_Compliant_WithShebangAndSingleLineComment()
        {
            // TODO: CS1024 (Preprocessor directive expected) is ignored because '#' in shebang ('#!')
            //       is not supported in older Roslyn environment.
            var test = @"{|CS1024:#|}!/usr/bin/env dotnet-script
// this is a comment
using System;

namespace Test
{
    class MyClass { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0050_Compliant_WithShebangAndMultiLineComment()
        {
            // TODO: CS1024 (Preprocessor directive expected) is ignored because '#' in shebang ('#!')
            //       is not supported in older Roslyn environment.
            var test = @"{|CS1024:#|}!/usr/bin/env dotnet-script
/* this is a multi-line comment */
using System;

namespace Test
{
    class MyClass { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0050_Violation_WithShebang_NoHeaderComment()
        {
            // TODO: CS1024 (Preprocessor directive expected) is ignored because '#' in shebang ('#!')
            //       is not supported in older Roslyn environment.
            var test = @"{|CS1024:#|}!/usr/bin/env dotnet-script
{|#0:using System;|}

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0050_Violation_WithShebang_StartsWithBlankLine()
        {
            // TODO: CS1024 (Preprocessor directive expected) is ignored because '#' in shebang ('#!')
            //       is not supported in older Roslyn environment.
            var test = @"{|CS1024:{|#0:#|}|}!/usr/bin/env dotnet-script

using System;

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
