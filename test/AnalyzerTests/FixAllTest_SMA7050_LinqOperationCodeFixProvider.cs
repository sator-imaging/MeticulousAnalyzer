// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.LinqOperationAnalyzer,
    SatorImaging.MeticulousAnalyzer.CodeFixes.Providers.LinqOperationCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class FixAllTest_SMA7050_LinqOperationCodeFixProvider
    {
        private const string SourceTemplate = @"
namespace Test
{{
    // Leading trivia
    {{|#{1}:using System.Linq;|}} // Trailing trivia
    using System.Collections.Generic;

    public class C_{0}_0
    {{
        int Count(IEnumerable<int> items) => items.Count();
    }}
}}

namespace Test
{{
    // Leading trivia
    {{|#{2}:using System.Linq;|}} // Trailing trivia
    using System.Collections.Generic;

    public class C_{0}_1
    {{
        bool Any(IEnumerable<int> items) => items.Any();
    }}
}}

namespace Test
{{
    // Leading trivia
    {{|#{3}:using System.Linq;|}} // Trailing trivia
    using System.Collections.Generic;

    public class C_{0}_2
    {{
        IEnumerable<int> Where(IEnumerable<int> items) => items.Where(x => x > 0);
    }}
}}";

        private const string FixedTemplate = @"
namespace Test
{{
    // Leading trivia
     // Trailing trivia
    using System.Collections.Generic;

    public class C_{0}_0
    {{
        int Count(IEnumerable<int> items) => items.Count();
    }}
}}

namespace Test
{{
    // Leading trivia
     // Trailing trivia
    using System.Collections.Generic;

    public class C_{0}_1
    {{
        bool Any(IEnumerable<int> items) => items.Any();
    }}
}}

namespace Test
{{
    // Leading trivia
     // Trailing trivia
    using System.Collections.Generic;

    public class C_{0}_2
    {{
        IEnumerable<int> Where(IEnumerable<int> items) => items.Where(x => x > 0);
    }}
}}";

        private const string Portal = @"using System;
using System.Collections.Generic;

namespace Test
{
    internal static partial class LinqPortal
    {
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
        }

        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }

        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }
    }
}
";

        [TestMethod]
        public async Task SMA7050_CodeFix_FixAllInSolution()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 0, 0, 1, 2)),
                        ("Test1.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 1, 3, 4, 5)),
                        ("Test2.cs", string.Format(SourceTemplate.ReplaceLineEndings(), 2, 6, 7, 8)),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                        ("Test2.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 2)),
                        ("LinqPortal.Test.cs", Portal),
                    },
                },
                BatchFixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 0)),
                        ("Test1.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 1)),
                        ("Test2.cs", string.Format(FixedTemplate.ReplaceLineEndings(), 2)),
                        ("LinqPortal.Test.cs", Portal),
                    },
                },
                NumberOfIncrementalIterations = 9,
            };

            for (int i = 0; i < 3; i++)
            {
                int offset = i * 3;
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: offset));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: offset + 1));
                test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: offset + 2));
            }

            // TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            //       It's available in Roslyn version (4.4.0 or later).
            // test.FixAllScope = FixAllScope.Solution;
            await test.RunAsync();
        }
    }
}
