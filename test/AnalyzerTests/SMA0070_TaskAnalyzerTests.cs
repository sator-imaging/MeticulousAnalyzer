// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.TaskAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0070_TaskAnalyzerTests
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

        [TestMethod]
        public async Task SMA0070_Compliant_NonTaskType_LocalVariable()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            var x = 42;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Compliant_NonTaskType_Discarded()
        {
            var test = @"
namespace Test
{
    class Program
    {
        int GetValue() => 42;
        void Method()
        {
            _ = GetValue();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Violation_ValueTask_Discarded()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        ValueTask GetValueTask() => default;
        void Method()
        {
            _ = {|#0:GetValueTask()|};
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
        public async Task SMA0070_Violation_GenericTask_Discarded()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            _ = {|#0:Task.FromResult(42)|};
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
        public async Task SMA0070_Compliant_Discarded_InlineComment()
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
            _ = Task.FromResult(0);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Violation_CustomTaskSubclass()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class MyTask : Task
    {
        public MyTask() : base(() => {}) {}
    }

    class Program
    {
        async Task Method()
        {
            var {|#0:t|} = new MyTask();
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
        public async Task SMA0070_Compliant_Task_NoInitializer()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            Task t;
            t = Task.Run(() => {});
            await t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Violation_Task_InLambda_AwaitedInsideLambda()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            Func<Task> fn = async () =>
            {
                var {|#0:t|} = Task.Run(() => {});
                await t;
            };
            await fn();
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
        public async Task SMA0070_Violation_Task_InLambda_NotAwaited()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            Func<Task> fn = async () =>
            {
                var {|#0:t|} = Task.Run(() => {});
            };
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
        public async Task SMA0070_Violation_Task_ReturnedFromLambda()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        void Method()
        {
            Func<Task> fn = () =>
            {
                var {|#0:t|} = Task.Run(() => {});
                return t;
            };
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
        public async Task SMA0070_Violation_GenericValueTask_NotAwaited()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var {|#0:t|} = new ValueTask<int>(42);
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
        public async Task SMA0070_Compliant_Task_AwaitedInTryCatch()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method()
        {
            var t = Task.Run(() => {});
            try
            {
                await t;
            }
            catch (Exception)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Compliant_NonTaskAssignment_ToVariable()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        int _field;
        void Method()
        {
            _field = 10;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0070_Compliant_StringType_LocalVariable()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void Method()
        {
            var s = ""hello"";
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

    }
}
