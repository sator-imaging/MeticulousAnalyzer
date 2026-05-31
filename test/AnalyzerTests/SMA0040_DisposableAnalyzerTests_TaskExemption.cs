// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_TaskExemption
    {
        [TestMethod]
        public async Task SMA0040_Compliant_Task_CompletedTask()
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
            _ = Task.CompletedTask;
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_Task_CompletedTaskGetAwaiter()
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
            _ = Task.CompletedTask.GetAwaiter();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_Task_NewTaskAction()
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
            _ = new Task(() => { });
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_Task_NewTaskOfTFunc()
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
            _ = new Task<int>(() => 0);
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ValueTask_WrappingTask()
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
            _ = new ValueTask(Task.CompletedTask);
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ValueTask_AsTask()
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
            _ = new ValueTask(Task.CompletedTask).AsTask();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ValueTaskOfT_WrappingTask()
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
            _ = new ValueTask<int>(Task.FromResult(0));
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ValueTaskOfT_AsTask()
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
            _ = new ValueTask<int>(Task.FromResult(0)).AsTask();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
