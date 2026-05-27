// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.EnumAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0028_AnalyzerTests
    {
#if STMG_ENABLE_KOTLIN_ENUM
        [TestMethod]
        public async Task SMA0028_Violate_EnumLike()
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
