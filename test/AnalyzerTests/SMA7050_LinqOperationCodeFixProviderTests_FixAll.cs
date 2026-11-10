// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<LinqOperationAnalyzer, LinqOperationCodeFixProvider>;

    [TestClass]
    public class SMA7050_LinqOperationCodeFixProviderTests_FixAll
    {
        [TestMethod]
        public async Task SMA7050_CodeFix_FixAllAcrossDocuments_UpdatesSharedPortal()
        {
            var source0 = @"
namespace App.Features
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C0
    {
        public int M(IEnumerable<int> items) => items.Count();
    }
}
";
            var source1 = @"
namespace App.Other
{
    {|#1:using System.Linq;|}
    using System.Collections.Generic;

    public class C1
    {
        public bool M(IEnumerable<int> items) => items.Any();
    }
}
";
            var fixedSource0 = @"
namespace App.Features
{
    
    using System.Collections.Generic;

    public class C0
    {
        public int M(IEnumerable<int> items) => items.Count();
    }
}
";
            var fixedSource1 = @"
namespace App.Other
{
    
    using System.Collections.Generic;

    public class C1
    {
        public bool M(IEnumerable<int> items) => items.Any();
    }
}
";
            var portal = @"using System;
using System.Collections.Generic;

namespace App
{
    public static partial class LinqPortal
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }
    }
}
";
            var fixedSources = new[]
            {
                ("Test0.cs", fixedSource0),
                ("Test1.cs", fixedSource1),
                ("LinqPortal.App.cs", portal)
            };
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test0.cs", source0),
                        ("Test1.cs", source1)
                    },
                    ExpectedDiagnostics =
                    {
                        VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 0),
                        VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 1)
                    }
                },
                FixedState =
                {
                    Sources = { fixedSources[0], fixedSources[1], fixedSources[2] }
                },
                BatchFixedState =
                {
                    Sources = { fixedSources[0], fixedSources[1], fixedSources[2] }
                },
                NumberOfIncrementalIterations = 2
            };

            await test.RunAsync();
        }
    }
}
