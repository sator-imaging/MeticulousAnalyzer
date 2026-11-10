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
    public class SMA7050_LinqOperationCodeFixProviderTests_References
    {
        [TestMethod]
        public async Task SMA7050_CodeFix_ForwardsStaticQueryableAndOverloadedMethods()
        {
            var source = @"
namespace App.Features
{
    // Leading comment for the LINQ import
    {|#0:using System.Linq;|} // Trailing comment for the LINQ import
    using System;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items, IQueryable<int> query)
        {
            var range = Enumerable.Range(0, 10);
            IOrderedEnumerable<int> ordered = range.OrderBy(value => value);
            Func<IEnumerable<int>, int> count = Enumerable.Count;
            Func<int> countItems = items.Count;
            var indexed = items.Where((value, index) => index > 0);
            var filtered = query.Where(value => value > 0);
        }
    }
}
";
            var fixedSource = @"
namespace App.Features
{
    // Leading comment for the LINQ import
     // Trailing comment for the LINQ import
    using System;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items, global::System.Linq.IQueryable<int> query)
        {
            var range = global::App.LinqPortal.Range(0, 10);
            global::System.Linq.IOrderedEnumerable<int> ordered = range.OrderBy(value => value);
            Func<IEnumerable<int>, int> count = global::App.LinqPortal.Count;
            Func<int> countItems = items.Count;
            var indexed = items.Where((value, index) => index > 0);
            var filtered = query.Where(value => value > 0);
        }
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
        public static global::System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, TKey> keySelector)
        {
            return global::System.Linq.Enumerable.OrderBy<TSource, TKey>(source, keySelector);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<int> Range(int start, int count)
        {
            return global::System.Linq.Enumerable.Range(start, count);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Linq.IQueryable<TSource> Where<TSource>(this global::System.Linq.IQueryable<TSource> source, global::System.Linq.Expressions.Expression<global::System.Func<TSource, bool>> predicate)
        {
            return global::System.Linq.Queryable.Where<TSource>(source, predicate);
        }
    }
}
";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { ("Test0.cs", source) },
                    ExpectedDiagnostics =
                    {
                        VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 0)
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", fixedSource),
                        ("LinqPortal.App.cs", portal)
                    }
                }
            };

            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_UsesVisiblePortalAcrossNamespaces()
        {
            var source = @"{|#0:using System.Linq;|}
using System.Collections.Generic;

namespace A
{
    public class C0
    {
        public int M(IEnumerable<int> items) => items.Count();
    }
}

namespace B
{
    public class C1
    {
        public bool M(IEnumerable<int> items) => items.Any();
    }
}
";
            var fixedSource = @"
using System.Collections.Generic;

namespace A
{
    public class C0
    {
        public int M(IEnumerable<int> items) => items.Count();
    }
}

namespace B
{
    public class C1
    {
        public bool M(IEnumerable<int> items) => items.Any();
    }
}
";
            var unrelatedPortal = @"namespace A
{
    public static partial class LinqPortal
    {
    }
}
";
            var portal = @"using System;
using System.Collections.Generic;

public static partial class LinqPortal
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
    {
        return global::System.Linq.Enumerable.Any<TSource>(source);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
    {
        return global::System.Linq.Enumerable.Count<TSource>(source);
    }
}
";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test0.cs", source),
                        ("LinqPortal.A.cs", unrelatedPortal)
                    },
                    ExpectedDiagnostics =
                    {
                        VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 0)
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", fixedSource),
                        ("LinqPortal.A.cs", unrelatedPortal),
                        ("LinqPortal.cs", portal)
                    }
                }
            };

            await test.RunAsync();
        }
    }
}
