// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.TaskAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class TaskAnalyzerUnitTests
    {
        [TestMethod]
        public async Task Task_NotAwaited_ReportsDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var {|#0:t|} = Task.Run(() => {});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Task_Awaited_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var t = Task.Run(() => {});
            await t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_Returned_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        Task Method()
        {
            var t = Task.Run(() => {});
            return t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_NotAwaitedOnAllPaths_ReportsDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(bool condition)
        {
            var {|#0:t|} = Task.Run(() => {});
            if (condition)
            {
                await t;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_NotAllCodePathsAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Task_AwaitedOnAllPaths_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(bool condition)
        {
            var t = Task.Run(() => {});
            if (condition)
            {
                await t;
            }
            else
            {
                await t;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_SuppressedByComment_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            // Don't await
            var t = Task.Run(() => {});
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task ValueTask_NotAwaited_ReportsDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var {|#0:t|} = new ValueTask();
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task GenericTask_NotAwaited_ReportsDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var {|#0:t|} = Task.FromResult(0);
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Task_InsideBranch_Awaited_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(bool condition)
        {
            if (condition)
            {
                var t = Task.Run(() => {});
                await t;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CompletedTask_NotAwaited_ReportsDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var {|#0:t|} = Task.CompletedTask;
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task CompletedTask_Awaited_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var t = Task.CompletedTask;
            await t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_AwaitedInBranchAndAfterBranch_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(bool condition)
        {
            var t = Task.Run(() => {});

            if (condition)
            {
                await t;
                return;
            }

            await t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_AwaitedInBranchAndReturned_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task<int> Method(bool condition)
        {
            var t = Task.FromResult(0);

            if (condition)
            {
                await t;
                return 0;
            }

            await t;
            return 1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_ReturnedInBranchAndAwaitedAfterBranch_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task<int> Method(bool condition)
        {
            var t = Task.FromResult(0);

            if (condition)
            {
                return await t;
            }

            return t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Suppression_NotFirstComment_ReportsDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            // some other comment
            // Don't await
            var {|#0:t|} = Task.Run(() => {});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Suppression_WithNewline_ReportsNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            // Don't await

            var t = Task.Run(() => {});
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Task_NotSuppressedByPrecedingLineEndComment()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void DoNothing() {}
        async Task Method()
        {
            DoNothing(); // Don't await
            var {|#0:t|} = Task.Run(() => {});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Task_NotSuppressedByPrecedingLineEndCommentWithBlankLine()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void DoNothing() {}
        async Task Method()
        {
            DoNothing(); // Don't await

            var {|#0:t|} = Task.Run(() => {});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Task_NotSuppressedByMultiLineComment()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            /* Don't await */
            var {|#0:t|} = Task.Run(() => {});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
