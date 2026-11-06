// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

// The custom test runner is used because the default roslyn verifier runs a
// `#pragma warning disable` test automatically, but this analyzer is not
// affected by `#pragma`.
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.ShebangAnalyzerVerifier;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0051_ShebangAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0051_Violation_BuildCs_NoShebang()
        {
            var test = @"{|#0:using System;|}

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(ShebangAnalyzer.RuleId_MissingShebang)
                .WithLocation(0)
                .WithArguments("build.cs");

            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("build.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Violation_BuildCs_SubdirectoryPath_NoShebang()
        {
            var test = @"{|#0:using System;|}

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(ShebangAnalyzer.RuleId_MissingShebang)
                .WithLocation(0)
                .WithArguments("build.cs");

            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("some/path/to/build.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Violation_BuildCs_StartsWithBlankLine()
        {
            var test = @"
{|#0:using|} System;

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(ShebangAnalyzer.RuleId_MissingShebang)
                .WithLocation(0)
                .WithArguments("build.cs");

            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("build.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Violation_BuildCs_StartsWithCommentBeforeShebang()
        {
            // TODO: CS1024 (Preprocessor directive expected) is ignored because '#' in shebang ('#!')
            //       is not supported in older Roslyn environment.
            var test = @"{|#0:// leading comment|}
{|CS1024:#|}!/usr/bin/env dotnet-script
using System;

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(ShebangAnalyzer.RuleId_MissingShebang)
                .WithLocation(0)
                .WithArguments("build.cs");

            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("build.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Violation_BuildCs_WithPragmaWarningDisable()
        {
            var test = @"{|#0:#pragma warning disable CS8618|}

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(ShebangAnalyzer.RuleId_MissingShebang)
                .WithLocation(0)
                .WithArguments("build.cs");

            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("build.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Compliant_BuildCs_StartsWithShebang()
        {
            // TODO: CS1024 (Preprocessor directive expected) is ignored because '#' in shebang ('#!')
            //       is not supported in older Roslyn environment.
            var test = @"{|CS1024:#|}!/usr/bin/env dotnet-script
using System;

namespace Test
{
    class MyClass { }
}
";
            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("build.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Compliant_OtherCs_NoShebang()
        {
            var test = @"using System;

namespace Test
{
    class MyClass { }
}
";
            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState = { Sources = { ("other.cs", test) } },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Violation_AdditionalFiles_BuildCs_NoShebang()
        {
            var additional = @"using System;

namespace Test
{
    class MyClass { }
}
";
            var expected = VerifyCS.Diagnostic(ShebangAnalyzer.RuleId_MissingShebang)
                .WithSpan("build.cs", 1, 1, 1, 14)
                .WithArguments("build.cs");

            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState =
                {
                    Sources = { ("Dummy.cs", "class Dummy {}") },
                    AdditionalFiles = { ("build.cs", additional) },
                },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            verifier.ExpectedDiagnostics.Add(expected);
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Compliant_AdditionalFiles_BuildCs_StartsWithShebang()
        {
            var additional = @"#!/usr/bin/env dotnet-script
using System;

namespace Test
{
    class MyClass { }
}
";
            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState =
                {
                    Sources = { ("Dummy.cs", "class Dummy {}") },
                    AdditionalFiles = { ("build.cs", additional) },
                },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            await verifier.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0051_Compliant_AdditionalFiles_OtherCs_NoShebang()
        {
            var additional = @"using System;

namespace Test
{
    class MyClass { }
}
";
            var verifier = new CSharpAnalyzerVerifier<ShebangAnalyzer>.Test
            {
                TestState =
                {
                    Sources = { ("Dummy.cs", "class Dummy {}") },
                    AdditionalFiles = { ("other.cs", additional) },
                },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };
            await verifier.RunAsync(CancellationToken.None);
        }
    }
}
