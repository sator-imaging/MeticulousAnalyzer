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
    public class SMA0026_EnumAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0026_Violation_EnumObfuscation()
        {
            var test = @"
namespace Test
{
    public enum {|#0:ETest|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0026_Violation_EnumObfuscation_EmptyAttribute()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation()]
    public enum {|#0:ETest|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0026_Violation_EnumObfuscation_NoParams()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation]
    public enum {|#0:ETest|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0026_Violation_EnumObfuscation_WrongParams()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(StripAfterObfuscation = true)]
    public enum {|#0:ETest|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0026_Violation_EnumObfuscation_MissingApplyToMembers()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true)]
    public enum {|#0:ETest|} { Value }
}
";
            var expected = VerifyCS.Diagnostic(EnumAnalyzer.RuleId_EnumObfuscation).WithLocation(markupKey: 0).WithArguments("ETest");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0026_Compliant_EnumObfuscation_Complete()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum ETest { Value }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0026_Compliant_EnumObfuscation_ExpressionTrue()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    [Obfuscation(ApplyToMembers = ""A"" != ""B"", Exclude = true)]
    public enum ETest { Value }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

    }
}
