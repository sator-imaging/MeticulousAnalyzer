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
    public class SMA7050_LinqOperationCodeFixProviderTests_ExistingFile
    {
        private static Task VerifyOverloadOrderAsync(string source, string fixedSource, string existingPortal, string expectedPortal)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { ("Test0.cs", source), ("LinqPortal.MyCompany.cs", existingPortal) },
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
                        ("LinqPortal.MyCompany.cs", expectedPortal)
                    }
                }
            };
            // The analyzer skips projects whose assembly name contains "Test".
            CSharpVerifierHelper.SetAssemblyName(test, "Application");
            return test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_FormattedExistingMethods()
        {
            const string source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var any = items.Any(x => x > 0);
            var select = items.Select((x, index) => x + index);
        }
    }
}
";
            const string fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var any = items.Any(x => x > 0);
            var select = items.Select((x, index) => x + index);
        }
    }
}
";
            const string existingPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(
            this global::System.Collections.Generic.IEnumerable<TSource> source // formatted
        )
        {
            return global::System.Linq.Enumerable.Any(source);
        }

        // Select documentation
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }
    }
}
";
            const string expectedPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(
            this global::System.Collections.Generic.IEnumerable<TSource> source // formatted
        )
        {
            return global::System.Linq.Enumerable.Any(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }

        // Select documentation
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Select{TSource, TResult}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, int, TResult})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }
    }
}
";
            await VerifyOverloadOrderAsync(source, fixedSource, existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_ShorterSignatureBeforeFormattedExistingOverload()
        {
            const string source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var any = items.Any();
        }
    }
}
";
            const string fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var any = items.Any();
        }
    }
}
";
            const string existingPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        // Existing overload
        public static bool Any<TSource>(
            this global::System.Collections.Generic.IEnumerable<TSource> source,
            global::System.Func<TSource, bool> predicate
        )
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }
    }
}
";
            const string expectedPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any(source);
        }

        // Existing overload
        public static bool Any<TSource>(
            this global::System.Collections.Generic.IEnumerable<TSource> source,
            global::System.Func<TSource, bool> predicate
        )
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }
    }
}
";
            await VerifyOverloadOrderAsync(source, fixedSource, existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_SharedBoundaryBetweenExistingMethods()
        {
            const string source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var select = items.Select(x => x + 1);
            var any = items.Any(x => x > 0);
        }
    }
}
";
            const string fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var select = items.Select(x => x + 1);
            var any = items.Any(x => x > 0);
        }
    }
}
";
            const string existingPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any(source);
        }

        // Existing indexed overload
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }
    }
}
";
            const string expectedPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Select{TSource, TResult}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, TResult})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }

        // Existing indexed overload
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }
    }
}
";
            await VerifyOverloadOrderAsync(source, fixedSource, existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_NewMethodsShareInsertionPosition()
        {
            const string source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var anyPredicate = items.Any(x => x > 0);
            var selectIndex = items.Select((x, index) => x + index);
            var any = items.Any();
            var select = items.Select(x => x + 1);
        }
    }
}
";
            const string fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var anyPredicate = items.Any(x => x > 0);
            var selectIndex = items.Select((x, index) => x + index);
            var any = items.Any();
            var select = items.Select(x => x + 1);
        }
    }
}
";
            const string existingPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
    }
}
";
            const string expectedPortal = @"using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Select{TSource, TResult}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, TResult})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Select{TSource, TResult}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, int, TResult})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }
    }
}
";
            await VerifyOverloadOrderAsync(source, fixedSource, existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ExistingLinqPortalFile_AddsNewMember_AtLast()
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
    using Enumerable = System.Linq.Enumerable;
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
            var existingPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
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
            var expectedMergedPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }

        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Count{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    Sources =
                    {
                        ("Test0.cs", test),
                        ("LinqPortal.MyCompany.cs", existingPortal)
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
                        ("Test0.cs", fixtest),
                        ("LinqPortal.MyCompany.cs", expectedMergedPortal)
                    }
                },
            };

            CSharpVerifierHelper.SetAssemblyName(testObj, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await testObj.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ExistingLinqPortalFile_AddsMissingOverloadBesideExistingMethod()
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
            var count = list.Count(x => x > 0);
        }
    }
}
";
            var fixtest = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public void M()
        {
            IEnumerable<int> list = new List<int>();
            var count = list.Count(x => x > 0);
        }
    }
}
";
            var existingPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
        }

        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }
    }
}
";
            var expectedMergedPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Count{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Count(source, predicate);
        }

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
                    Sources =
                    {
                        ("Test0.cs", test),
                        ("LinqPortal.MyCompany.cs", existingPortal)
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
                        ("Test0.cs", fixtest),
                        ("LinqPortal.MyCompany.cs", expectedMergedPortal)
                    }
                },
            };

            CSharpVerifierHelper.SetAssemblyName(testObj, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await testObj.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ExistingLinqPortalFile_AddsSameArityDifferentParameterTypes()
        {
            var source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var filtered = items.Where((value, index) => index > 0);
        }
    }
}
";
            var fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public void M(IEnumerable<int> items)
        {
            var filtered = items.Where((value, index) => index > 0);
        }
    }
}
";
            var existingPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }
    }
}
";
            var expectedPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Where{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, int, bool})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where(source, predicate);
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
                        ("LinqPortal.MyCompany.cs", existingPortal)
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
                        ("LinqPortal.MyCompany.cs", expectedPortal)
                    }
                }
            };

            CSharpVerifierHelper.SetAssemblyName(test, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ExistingLinqPortalFile_PreservesUserDefinedElements()
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
    using Enumerable = System.Linq.Enumerable;
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
            var existingPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    class UserDefined { }

    /// <summary>
    /// User xml doc for LinqPortal.
    /// </summary>
    // User comment before LinqPortal
    internal static partial class LinqPortal
    {
        // User field comment
        public static readonly int UserField = 42;

        /// <summary>
        /// User property doc.
        /// </summary>
        public static string UserProperty => ""test"";

        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }
        #region Preserve close-brace trivia
        #endregion
    }
}
";
            var expectedMergedPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    class UserDefined { }

    /// <summary>
    /// User xml doc for LinqPortal.
    /// </summary>
    // User comment before LinqPortal
    internal static partial class LinqPortal
    {
        // User field comment
        public static readonly int UserField = 42;

        /// <summary>
        /// User property doc.
        /// </summary>
        public static string UserProperty => ""test"";

        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }
        #region Preserve close-brace trivia
        #endregion

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Count{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    Sources =
                    {
                        ("Test0.cs", test),
                        ("LinqPortal.MyCompany.cs", existingPortal)
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
                        ("Test0.cs", fixtest),
                        ("LinqPortal.MyCompany.cs", expectedMergedPortal)
                    }
                },
            };

            CSharpVerifierHelper.SetAssemblyName(testObj, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await testObj.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ExistingLinqPortalFile_DoesNotAddExistingForwarder()
        {
            var source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public bool M(IEnumerable<int> items) => items.Any();
    }
}
";
            var fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public bool M(IEnumerable<int> items) => items.Any();
    }
}
";
            var existingPortal = @"using System;
using System.Collections.Generic;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
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
                        ("LinqPortal.MyCompany.cs", existingPortal)
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
                        ("LinqPortal.MyCompany.cs", existingPortal)
                    }
                }
            };

            CSharpVerifierHelper.SetAssemblyName(test, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_ExistingLinqPortalFile_InsertsAnyOverloadBeforeCount()
        {
            var source = @"
namespace MyCompany.MyProduct
{
    {|#0:using System.Linq;|}
    using System.Collections.Generic;

    public class C
    {
        public bool M(IEnumerable<int> items) => items.Any(value => value > 0);
    }
}
";
            var fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
    using System.Collections.Generic;

    public class C
    {
        public bool M(IEnumerable<int> items) => items.Any(value => value > 0);
    }
}
";
            var existingPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }

        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
        }
    }
}
";
            var expectedPortal = @"using System;
using System.Collections.Generic;
using global::System.Runtime.CompilerServices;

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }

        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
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
                        ("LinqPortal.MyCompany.cs", existingPortal)
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
                        ("LinqPortal.MyCompany.cs", expectedPortal)
                    }
                }
            };

            CSharpVerifierHelper.SetAssemblyName(test, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_WrongCasePortalFile_CreatesExactPortalFile()
        {
            var source = @"
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
            var fixedSource = @"
namespace MyCompany.MyProduct
{
    using Enumerable = System.Linq.Enumerable;
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
            var wrongCasePortal = "// This is not the exact portal file.";
            var expectedPortal = LinqOperationCodeFixProvider.GeneratedFileHeader + @"namespace MyCompany
{
    internal static partial class LinqPortal
    {
        /// <inheritdoc cref=""global::System.Linq.Enumerable.Count{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        ("LinqPortal.mycompany.cs", wrongCasePortal)
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
                        ("LinqPortal.mycompany.cs", wrongCasePortal),
                        ("LinqPortal.MyCompany.cs", expectedPortal)
                    }
                }
            };

            CSharpVerifierHelper.SetAssemblyName(test, "Application"); // The analyzer skips projects whose assembly name contains "Test".
            await test.RunAsync();
        }
    }
}
