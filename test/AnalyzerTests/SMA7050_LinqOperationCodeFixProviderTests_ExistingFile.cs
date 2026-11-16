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
        private const string AnySourceForwarder = @"        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any(source);
        }";

        private const string AnyPredicateForwarder = @"        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Any(source, predicate);
        }";

        private const string SelectSourceForwarder = @"        /// <inheritdoc cref=""global::System.Linq.Enumerable.Select{TSource, TResult}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, TResult})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }";

        private const string SelectIndexForwarder = @"        /// <inheritdoc cref=""global::System.Linq.Enumerable.Select{TSource, TResult}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, int, TResult})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, int, TResult> selector)
        {
            return global::System.Linq.Enumerable.Select(source, selector);
        }";

        private static async Task VerifyOverloadOrderAsync(string statements, string existingPortal, string expectedPortal)
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
" + statements + @"
        }
    }
}
";
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
                        ("Test0.cs", source.Replace("{|#0:using System.Linq;|}", "using Enumerable = System.Linq.Enumerable;")),
                        ("LinqPortal.MyCompany.cs", expectedPortal)
                    }
                }
            };
            CSharpVerifierHelper.SetAssemblyName(test, "Application");
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_FormattedExistingMethods()
        {
            var existingPortal = @"namespace MyCompany
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
            var expectedPortal = existingPortal
                .Replace("        // Select documentation", AnyPredicateForwarder + "\n\n        // Select documentation")
                .Replace("    }\n}\n", "\n" + SelectIndexForwarder + "\n    }\n}\n");
            await VerifyOverloadOrderAsync(
                "            var any = items.Any(x => x > 0);\n            var select = items.Select((x, index) => x + index);",
                existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_ShorterSignatureBeforeFormattedExistingOverload()
        {
            var existingPortal = @"namespace MyCompany
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
            var expectedPortal = existingPortal.Replace("        // Existing overload", AnySourceForwarder + "\n\n        // Existing overload");
            await VerifyOverloadOrderAsync("            var any = items.Any();", existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_SharedBoundaryBetweenExistingMethods()
        {
            var existingPortal = @"namespace MyCompany
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
            var expectedPortal = existingPortal.Replace(
                "        // Existing indexed overload",
                AnyPredicateForwarder + "\n\n" + SelectSourceForwarder + "\n\n        // Existing indexed overload");
            await VerifyOverloadOrderAsync(
                "            var select = items.Select(x => x + 1);\n            var any = items.Any(x => x > 0);",
                existingPortal, expectedPortal);
        }

        [TestMethod]
        public async Task SMA7050_CodeFix_OverloadOrder_NewMethodsShareInsertionPosition()
        {
            var existingPortal = @"namespace MyCompany
{
    internal static partial class LinqPortal
    {
    }
}
";
            var expectedPortal = existingPortal.Replace(
                "    }\n}\n",
                AnySourceForwarder + "\n\n" + AnyPredicateForwarder + "\n\n"
                + SelectSourceForwarder + "\n\n" + SelectIndexForwarder + "\n    }\n}\n");
            await VerifyOverloadOrderAsync(
                "            var anyPredicate = items.Any(x => x > 0);\n"
                + "            var selectIndex = items.Select((x, index) => x + index);\n"
                + "            var any = items.Any();\n"
                + "            var select = items.Select(x => x + 1);",
                existingPortal, expectedPortal);
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

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static int Count<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Count<TSource>(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Count{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static global::System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source, global::System.Func<TSource, bool> predicate)
        {
            return global::System.Linq.Enumerable.Where<TSource>(source, predicate);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Where{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, int, bool})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        public static bool Any<TSource>(this global::System.Collections.Generic.IEnumerable<TSource> source)
        {
            return global::System.Linq.Enumerable.Any<TSource>(source);
        }

        /// <inheritdoc cref=""global::System.Linq.Enumerable.Any{TSource}(global::System.Collections.Generic.IEnumerable{TSource}, global::System.Func{TSource, bool})""/>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
var expectedPortal = @"// <auto-generated>
//   Generated by SatorImaging.MeticulousAnalyzer
// </auto-generated>

#nullable enable
#pragma warning disable IDE0001 // Simplify name

namespace MyCompany
{
    internal static partial class LinqPortal
    {
        /// <inheritdoc cref=""global::System.Linq.Enumerable.Count{TSource}(global::System.Collections.Generic.IEnumerable{TSource})""/>
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
