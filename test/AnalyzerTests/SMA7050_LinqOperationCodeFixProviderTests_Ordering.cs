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
    public class SMA7050_LinqOperationCodeFixProviderTests_Ordering
    {
        [TestMethod]
        public async Task SMA7050_CodeFix_AlphabeticalMethodOrdering()
        {
            var test = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var filtered = list.Where(x => x > 0);
            var count = list.Count();
            var any = list.Any();
        }
    }
}
";
            var fixtest = @"
namespace MyCompany.MyProduct
{
" + "    " + @"
    using System.Collections.Generic;

    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var filtered = list.Where(x => x > 0);
            var count = list.Count();
            var any = list.Any();
        }
    }
}
";
            var expectedPortal = @"using System;
using System.Collections.Generic;

namespace MyCompany
{
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }
    }
}
";

            var testObj = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { ("Test0.cs", test) },
                    ExpectedDiagnostics =
                    {
                        VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 0)
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        ("Test0.cs", fixtest),
                        ("LinqPortal.MyCompany.cs", expectedPortal)
                    }
                },
            };

            await testObj.RunAsync();
        }
    }
}
