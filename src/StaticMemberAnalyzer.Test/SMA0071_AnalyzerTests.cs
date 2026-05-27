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
    public class SMA0071_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0071_Violate_Task_NotAwaitedOnAllPaths()
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

    }
}
