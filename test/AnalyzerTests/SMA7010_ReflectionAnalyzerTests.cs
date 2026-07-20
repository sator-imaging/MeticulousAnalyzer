// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCs = CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ReflectionAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA7010_ReflectionAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7010_Violation_Invocation_ReturnsReflectionType()
        {
            var test = @"
namespace Test
{
    public class Bar { }

    public class C
    {
        public void M(object foo)
        {
            {|#0:foo.GetType().GetMembers()|};
            {|#1:typeof(Bar).GetMembers()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("GetMembers", "MemberInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Violation_Invocation_UserMethodReturnsReflectionType()
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
            var {|#0:m|} = {|#1:GetIt()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("m", "MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetIt", "MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Violation_Invocation_ConditionalAccess()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(System.Type type)
        {
            var {|#0:methods|} = type?{|#1:.GetMethods()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("methods", "MethodInfo[]"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMethods", "MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Violation_MemberReference_PropertyReturnsReflectionType()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(System.Type type)
        {
            var {|#0:asm|} = {|#1:type.Assembly|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("asm", "Assembly"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("Assembly", "Assembly")
            );
        }

        [TestMethod]
        public async Task SMA7010_Violation_MemberReference_DeclaredInReflectionType()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M(MethodInfo method)
        {
            {|#0:method.Invoke(null, null)|};
            _ = {|#1:method.Name|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("Invoke", "MethodInfo"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("Name", "MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Violation_MemberReference_MethodGroup()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            System.Func<MemberInfo[]> {|#0:f|} = {|#1:typeof(C).GetMembers|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionVariable).WithLocation(0).WithArguments("f", "Func<MemberInfo[]>"),
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(1).WithArguments("GetMembers", "MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Compliant_MemberDeclarationsWithoutUsage()
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
        public async Task SMA7010_Violation_ArgumentType()
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
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("list", "MethodInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Violation_EnumArgument_FieldReference()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            {|#0:typeof(C).GetMembers(BindingFlags.Public | BindingFlags.Instance)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage).WithLocation(0).WithArguments("GetMembers", "MemberInfo")
            );
        }

        [TestMethod]
        public async Task SMA7010_Compliant_EnumMemberAccess()
        {
            var test = @"
using System.Reflection;

namespace Test
{
    public class C
    {
        public void M()
        {
            _ = BindingFlags.Public;
            _ = BindingFlags.Public | BindingFlags.Instance;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7010_Compliant_GetTypeAndNonReflectionMembers()
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
        public async Task SMA7010_Compliant_AttributeUsage()
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
        public async Task SMA7010_Compliant_UserDeclaredReflectionLikeNamespace()
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
