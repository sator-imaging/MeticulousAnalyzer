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
    public class SMA0070_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0070_Violation_Task_Awaited()
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
        public async Task SMA0070_Compliant_Task_Awaited()
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
        public async Task SMA0070_Compliant_Task_Returned()
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
        public async Task SMA0070_Compliant_Task_AwaitedOnAllPaths()
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
        public async Task SMA0070_Compliant_Task_Comment()
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
        public async Task SMA0070_Violation_ValueTask_Awaited()
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
        public async Task SMA0070_Violation_GenericTask_Awaited()
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
        public async Task SMA0070_Compliant_Task_InsideBranch_Awaited()
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
        public async Task SMA0070_Violation_CompletedTask_Awaited()
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
        public async Task SMA0070_Compliant_CompletedTask_Awaited()
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
        public async Task SMA0070_Compliant_Task_AwaitedInBranchAndAfterBranch()
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
        public async Task SMA0070_Compliant_Task_AwaitedInBranchAndReturned()
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
        public async Task SMA0070_Compliant_Task_ReturnedInBranchAndAwaitedAfterBranch()
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

            await t;
            return 0;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Violation_Suppression_FirstComment()
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
        public async Task SMA0070_Compliant_Suppression_WithNewline()
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
        public async Task SMA0070_Violation_Task_PrecedingLineEndComment()
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
        public async Task SMA0070_Violation_Task_PrecedingLineEndCommentWithBlankLine()
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
        public async Task SMA0070_Violation_Task_MultiLineComment()
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

        [TestMethod]
        public async Task SMA0070_Violation_Task_Discarded()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            _ = {|#0:Task.Run(() => {})|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("_");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0070_Violation_Task_Variable_Discarded()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            var {|#0:_|} = Task.Run(() => {});
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("_");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0070_Compliant_Task_Discarded_Comment()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            // Don't await
            _ = Task.Run(() => {});
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

    }
}
