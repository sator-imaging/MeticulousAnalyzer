using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReflectionAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA7011_ReflectionAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7010_Violation_LocalDeclaration()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            MethodInfo {|#0:method|} = null;
            _ = method;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("method", "MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7011_Violation_MultiDeclaratorVariableDeclaration()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        static MethodInfo GetIt() => null;

        public void M()
        {
            var {|#0:a|}, {|#1:b|} = {|#2:GetIt()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("a", "MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(1).WithArguments("b", "MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(2).WithArguments("GetIt", "MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7011_Violation_ForEachVarDeclaration()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach (var {|#0:member|} in {|#1:typeof(C).GetMembers()|})
            {
                _ = member;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("member", "MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "MemberInfo")
            );
        }
    }
}
