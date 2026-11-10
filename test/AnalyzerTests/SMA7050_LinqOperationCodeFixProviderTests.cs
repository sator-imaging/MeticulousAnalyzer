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
    public class SMA7050_LinqOperationCodeFixProviderTests
    {
        [TestMethod]
        public async Task SMA7050_CodeFix_SimpleLinqCall()
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
            var count = list.Count();
        }
    }
}
";
            var fixtest = @"
namespace MyCompany.MyProduct
{
    using System.Collections.Generic;

    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var count = list.Count();
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
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
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

        [TestMethod]
        public async Task SMA7050_CodeFix_MultipleLinqMethods()
        {
            var test = @"
namespace App.Features
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
        }
    }
}
";
            var fixtest = @"
namespace App.Features
{
    using System.Collections.Generic;

    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var filtered = list.Where(x => x > 0);
            var count = list.Count();
        }
    }
}
";
            var expectedPortal = @"using System;
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
                        ("LinqPortal.App.cs", expectedPortal)
                    }
                },
            };

            await testObj.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_RootLevelUsing()
        {
            var test = @"{|#0:using System.Linq;|}
using System.Collections.Generic;

namespace MyCompany.Root
{
    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var count = list.Count();
        }
    }
}
";
            var fixtest = @"using System.Collections.Generic;

namespace MyCompany.Root
{
    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var count = list.Count();
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
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
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
