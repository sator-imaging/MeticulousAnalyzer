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
    public class SMA0070_TaskAnalyzerTests_Coverage
    {
        // TARGET: IsTaskAwaitedOrReturned - AccessorDeclarationSyntax enclosingMember path
        // When a task is returned from a property getter accessor
        [TestMethod]
        public async Task SMA0070_Compliant_Task_ReturnedFromPropertyGetter()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        Task Prop
        {
            get
            {
                var t = Task.Run(() => {});
                return t;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // TARGET: IsTaskAwaitedOrReturned - task not awaited in accessor
        // Property getter with task variable not awaited
        [TestMethod]
        public async Task SMA0070_Violation_Task_NotAwaitedInPropertyGetter()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        int Prop
        {
            get
            {
                var {|#0:t|} = Task.Run(() => {});
                return 42;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(TaskAnalyzer.RuleId_MissingAwait)
                .WithLocation(markupKey: 0)
                .WithArguments("t");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // TARGET: IsTaskAwaitedOrReturned - ConditionalSuccessor path (line 283)
        // Task variable that is conditionally awaited in nested conditions creates
        // more complex CFG with conditional successors
        [TestMethod]
        public async Task SMA0071_Violation_Task_ConditionalSuccessorPath()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        async Task Method(bool a, bool b)
        {
            var {|#0:t|} = Task.Run(() => {});
            if (a)
            {
                if (b)
                {
                    await t;
                }
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

        // TARGET: IsTaskAwaitedOrReturned - AnonymousFunctionExpressionSyntax enclosingMember path
        // When a task is created inside a lambda
        [TestMethod]
        public async Task SMA0070_Violation_Task_InsideLambda()
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
            Func<Task> func = async () =>
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

        // TARGET: IsTaskAwaitedOrReturned - task returned in multiple branches
        // Tests the IReturnOperation detection in CFG blocks via if/else returns
        [TestMethod]
        public async Task SMA0070_Compliant_Task_ReturnedViaTernary()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        Task Method(bool condition)
        {
            var t = Task.Run(() => {});
            if (condition)
                return t;
            else
                return t;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // TARGET: IsTaskAwaitedOrReturned - task returned on some paths, not all
        // Tests the inAllCodePaths = false branch (line 283) via ConditionalSuccessor
        [TestMethod]
        public async Task SMA0071_Violation_Task_ReturnedOnSomePathsOnly()
        {
            var test = @"
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        Task Method(bool condition)
        {
            var {|#0:t|} = Task.Run(() => {});
            if (condition)
            {
                return t;
            }
            return Task.CompletedTask;
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
