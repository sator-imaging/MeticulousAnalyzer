// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0028_EnumAnalyzerTests
    {
#if STMG_ENABLE_KOTLIN_ENUM
        [TestMethod]
        public async Task SMA0028_Violation_EnumLike()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public sealed class {|#0:ETest|}
    {
        public static readonly ETest Value = new ETest();
        public static ETest[] Entries => new[] { Value };
        public ETest() {}
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumLike).WithLocation(0).WithArguments("ETest", "constructor is not 'private' or 'protected'");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
#endif

    }
}
