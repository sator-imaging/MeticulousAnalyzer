// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCs = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.TaskAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0071_TaskAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0071_Violation_Task_AwaitedOnAllPaths()
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
        public async Task SMA0071_Violation_Task_AwaitedInElseOnly()
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
                // not awaited here
            }
            else
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
        public async Task SMA0071_Compliant_Task_AwaitedInTryWithCatch()
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
                return;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0071_Violation_Task_ReturnedInOneBranch()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task<int> Method(bool condition)
        {
            var {|#0:t|} = Task.FromResult(0);
            if (condition)
            {
                return await t;
            }
            return 1;
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
        public async Task SMA0071_Violation_ValueTask_AwaitedInOneBranch()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(bool condition)
        {
            var {|#0:t|} = new ValueTask(Task.CompletedTask);
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
        public async Task SMA0071_Violation_Task_AwaitedInSwitchCase()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(int value)
        {
            var {|#0:t|} = Task.Run(() => {});
            switch (value)
            {
                case 1:
                    await t;
                    break;
                default:
                    break;
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
        public async Task SMA0071_Violation_Task_AwaitedInLoop()
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
            for (int i = 0; i < 1; i++)
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

    }
}
