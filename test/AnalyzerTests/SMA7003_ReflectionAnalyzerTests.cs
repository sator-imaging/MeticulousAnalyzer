using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReflectionAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA7003_ReflectionAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7003_Violation_Invocation_ReturnsReflectionType()
        {
            var test = @"
namespace Test
{
    public class Bar { }

    public class C
    {
        public void M(object foo)
        {
            foo.GetType().{|#0:GetMembers|}();
            typeof(Bar).{|#1:GetMembers|}();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("GetMembers", "System.Reflection.MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "System.Reflection.MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_Invocation_UserMethodReturnsReflectionType()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        static {|#0:MethodInfo|} GetIt() => null;

        public void M()
        {
            var {|#1:m|} = {|#2:GetIt|}();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("MethodInfo", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("m", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(2).WithArguments("GetIt", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_Invocation_ConditionalAccess()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(System.Type type)
        {
            var {|#0:methods|} = type?.{|#1:GetMethods|}();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("methods", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMethods", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_MemberReference_PropertyReturnsReflectionType()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(System.Type type)
        {
            var {|#0:asm|} = type.{|#1:Assembly|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("asm", "System.Reflection.Assembly"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("Assembly", "System.Reflection.Assembly")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_MemberReference_DeclaredInReflectionType()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M({|#0:MethodInfo|} method)
        {
            method.{|#1:Invoke|}(null, null);
            _ = method.{|#2:Name|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("MethodInfo", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("Invoke", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(2).WithArguments("Name", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_MemberReference_MethodGroup()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            System.Func<{|#0:MemberInfo|}[]> f = typeof(C).{|#1:GetMembers|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("MemberInfo", "System.Reflection.MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "System.Reflection.MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_TypeReference_Typeof()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            _ = typeof({|#0:MethodInfo|});
            _ = typeof(System.Reflection.{|#1:BindingFlags|});
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("MethodInfo", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("BindingFlags", "System.Reflection.BindingFlags")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_TypeReference_MemberDeclarations()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        private {|#0:FieldInfo|} _field;
        public {|#1:PropertyInfo|} Prop { get; set; }
        public {|#2:EventInfo|}[] Events;

        public {|#3:MemberInfo|} M({|#4:ParameterInfo|} parameter) => null;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("FieldInfo", "System.Reflection.FieldInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("PropertyInfo", "System.Reflection.PropertyInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(2).WithArguments("EventInfo", "System.Reflection.EventInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(3).WithArguments("MemberInfo", "System.Reflection.MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(4).WithArguments("ParameterInfo", "System.Reflection.ParameterInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_TypeReference_LocalDeclaration()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            {|#0:MethodInfo|} method = null;
            _ = method;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("MethodInfo", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_TypeReference_GenericTypeArgument()
        {
            var test = @"
using System.Collections.Generic;
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M(List<{|#0:MethodInfo|}> list) { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("MethodInfo", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_EnumArgument_ReportedOnceAsTypeReference()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            typeof(C).{|#0:GetMembers|}({|#1:BindingFlags|}.Public | {|#2:BindingFlags|}.Instance);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("GetMembers", "System.Reflection.MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("BindingFlags", "System.Reflection.BindingFlags"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(2).WithArguments("BindingFlags", "System.Reflection.BindingFlags")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_ForEachVarDeclaration()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach (var {|#0:member|} in typeof(C).{|#1:GetMembers|}())
            {
                _ = member;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("member", "System.Reflection.MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "System.Reflection.MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Compliant_GetTypeAndNonReflectionMembers()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(object foo)
        {
            var type = foo.GetType();
            _ = type.Name;
            _ = type.FullName;
            _ = typeof(string);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_AttributeUsage()
        {
            var test = @"
using System.Reflection;

[assembly: AssemblyDescription(""attribute from System.Reflection is exempt"")]

namespace Test
{
    [Obfuscation(Exclude = true)]
    public class C
    {
        public void M()
        {
            _ = typeof(ObfuscationAttribute);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_UserDeclaredReflectionLikeNamespace()
        {
            var test = @"
namespace Some.System.Reflection
{
    public class MemberInfo { }
}

namespace Test
{
    public class C
    {
        public void M()
        {
            var m = new Some.System.Reflection.MemberInfo();
            _ = typeof(Some.System.Reflection.MemberInfo);
            _ = m;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
