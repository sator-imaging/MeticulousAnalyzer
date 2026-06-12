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
        static MethodInfo GetIt() => null;

        public void M()
        {
            var {|#0:m|} = {|#1:GetIt|}();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("m", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetIt", "System.Reflection.MethodInfo")
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
        public void M(MethodInfo method)
        {
            method.{|#0:Invoke|}(null, null);
            _ = method.{|#1:Name|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("Invoke", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("Name", "System.Reflection.MethodInfo")
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
            System.Func<MemberInfo[]> {|#0:f|} = typeof(C).{|#1:GetMembers|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("f", "System.Reflection.MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "System.Reflection.MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_TypeOf()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            _ = {|#0:typeof(MethodInfo)|};
            _ = {|#1:typeof(System.Reflection.BindingFlags)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("typeof", "System.Reflection.MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("typeof", "System.Reflection.BindingFlags")
            );
        }

        [TestMethod]
        public async Task SMA7003_Compliant_MemberDeclarationsWithoutUsage()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        private FieldInfo _field;
        public PropertyInfo Prop { get; set; }
        public EventInfo[] Events;

        public MemberInfo M(ParameterInfo parameter) => null;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Violation_LocalDeclaration()
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
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("method", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_ArgumentType()
        {
            var test = @"
using System.Collections.Generic;
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M(List<MethodInfo> list)
        {
            Helper({|#0:list|});
        }

        static void Helper(List<MethodInfo> list) { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("list", "System.Reflection.MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7003_Violation_EnumArgument_FieldReference()
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
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("Public", "System.Reflection.BindingFlags"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(2).WithArguments("Instance", "System.Reflection.BindingFlags")
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
