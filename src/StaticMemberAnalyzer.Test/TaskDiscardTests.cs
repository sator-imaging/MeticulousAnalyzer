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
    public class TaskDiscardTests
    {
        [TestMethod]
        public async Task Task_Discarded_ReportsDiagnostic()
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
        public async Task Task_Variable_Discarded_ReportsDiagnostic()
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
        public async Task Task_Discarded_SuppressedByComment_ReportsNoDiagnostic()
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
