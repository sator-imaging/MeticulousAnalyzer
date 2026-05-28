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
    public class SMA0022_AnalyzerTests
    {
        [TestMethod]
        public async Task SMA0022_Violation_CastToGenericEnum()
        {
            var test = @"
using System;
using System.Reflection;

namespace Test
{
    public class CTest
    {
        public void Test<T>() where T : Enum
        {
            var x = {|#0:(T)(object)1|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_CastToGenericEnum).WithLocation(markupKey: 0).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
