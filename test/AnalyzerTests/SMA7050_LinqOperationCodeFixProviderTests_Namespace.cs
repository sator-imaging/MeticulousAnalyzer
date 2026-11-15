// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<LinqOperationCodeFixProviderTestDiagnosticAnalyzer, LinqOperationCodeFixProvider>;

    [TestClass]
    public class SMA7050_LinqOperationCodeFixProviderTests_Namespace
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
            var conditionalCount = list?.Count();
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
            var conditionalCount = list?.Count();
        }
    }
}
";
            var expectedPortal = @"using System;
using System.Collections.Generic;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count(source);
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
    internal static partial class LinqPortal
    {
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where(source, predicate);
        }

        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count(source);
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

            testObj.SolutionTransforms.Add((solution, projectId) =>
                solution.WithProjectDefaultNamespace(projectId, string.Empty));
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
    internal static partial class LinqPortal
    {
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count(source);
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

            testObj.SolutionTransforms.Add((solution, projectId) =>
                solution.WithProjectDefaultNamespace(projectId, string.Empty));
            await testObj.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_NoNamespace_GeneratesGlobalLinqPortal()
        {
            var source = @"{|#0:using System.Linq;|}
using System.Collections.Generic;

public class C
{
    public int M(IEnumerable<int> items) => items.Count();
}
";
            var fixedSource = @"using System.Collections.Generic;

public class C
{
    public int M(IEnumerable<int> items) => items.Count();
}
";
            var expectedPortal = @"using System;
using System.Collections.Generic;

internal static partial class LinqPortal
{
    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
    {
        return global::System.Linq.Enumerable.Count(source);
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
                        ("LinqPortal.cs", expectedPortal)
                    }
                }
            };

            test.SolutionTransforms.Add((solution, projectId) =>
                solution.WithProjectDefaultNamespace(projectId, string.Empty));
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ProjectDefaultNamespace_UsesTopLevelNamespace()
        {
            var source = @"
using ProjectRoot;
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public int M(IEnumerable<int> items) => items.Count();
    }
}
";
            var fixedSource = @"
using ProjectRoot;
namespace MyCompany.MyProduct
{
    using System.Collections.Generic;

    public class C
    {
        public int M(IEnumerable<int> items) => items.Count();
    }
}
";
            var namespaceAnchor = @"namespace ProjectRoot
{
    internal sealed class NamespaceAnchor { }
}
";
            var expectedPortal = @"using System;
using System.Collections.Generic;

namespace ProjectRoot
{
    internal static partial class LinqPortal
    {
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count(source);
        }
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
                        ("NamespaceAnchor.cs", namespaceAnchor)
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
                        ("NamespaceAnchor.cs", namespaceAnchor),
                        ("LinqPortal.ProjectRoot.cs", expectedPortal)
                    }
                }
            };
            test.SolutionTransforms.Add((solution, projectId) =>
                solution.WithProjectDefaultNamespace(projectId, "ProjectRoot.Some"));

            await test.RunAsync();
        }
    }
}
