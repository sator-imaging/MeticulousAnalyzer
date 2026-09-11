// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ReflectionAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
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
            MethodInfo {|#0:a|}, {|#1:b|} = {|#2:GetIt()|};
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

        [TestMethod]
        public async Task SMA7011_Violation_TupleDeclaration()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        static (MethodInfo, PropertyInfo) GetTuple() => (null, null);

        public void M()
        {
            var {|#0:(a, b)|} = {|#1:GetTuple()|};
            (MethodInfo {|#2:mi|}, PropertyInfo {|#3:pi|}) = {|#4:GetTuple()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("(a, b)", "(MethodInfo a, PropertyInfo b)"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetTuple", "MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(2).WithArguments("mi", "MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(3).WithArguments("pi", "PropertyInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(4).WithArguments("GetTuple", "MethodInfo")
            );
        }
    }
}
