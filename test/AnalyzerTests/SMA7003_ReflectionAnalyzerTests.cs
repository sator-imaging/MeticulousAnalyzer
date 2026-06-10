// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<ReflectionAnalyzer>;

    [TestClass]
    public class SMA7003_ReflectionAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7003_Violation_GetMembersInvocation()
        {
            var test = @"
using System;
public class C
{
    void M(object foo, Type barType)
    {
        foo.GetType().{|#0:GetMembers|}();
        typeof(Bar).{|#1:GetMembers|}();
    }
}
class Bar { }
";
            var expected0 = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("MemberInfo[]");
            var expected1 = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(1)
                .WithArguments("MemberInfo[]");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7003_Violation_MemberReferenceInSystemReflection()
        {
            var test = @"
using System.Reflection;
public class C
{
    void M()
    {
        var flags = BindingFlags.{|#0:Public|};
    }
}
";
            var expected = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("Public");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Violation_LocalDeclarationWithExplicitType()
        {
            var test = @"
using System.Reflection;
public class C
{
    void M()
    {
        {|#0:MemberInfo|}[] members = null;
    }
}
";
            var expected = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("MemberInfo[]");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Violation_VarDeclarationWithInferredReflectionType()
        {
            var test = @"
using System;
public class C
{
    void M(object foo)
    {
        var {|#0:members|} = foo.GetType().{|#1:GetMembers|}();
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("MemberInfo[]");
            var expected1 = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(1)
                .WithArguments("MemberInfo[]");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7003_Violation_TypeReference()
        {
            var test = @"
using System.Reflection;
public class C
{
    void M()
    {
        var t = typeof({|#0:MemberInfo|});
    }
}
";
            var expected = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("MemberInfo");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Violation_UsingDirective()
        {
            var test = @"
using System.{|#0:Reflection|};
public class C { }
";
            var expected = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("System.Reflection");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Violation_AssemblyPropertyReference()
        {
            var test = @"
using System;
public class C
{
    void M(Type type)
    {
        var assembly = type.{|#0:Assembly|};
    }
}
";
            var expected = VerifyCS.Diagnostic(ReflectionAnalyzer.RuleId_SystemReflectionUsage)
                .WithLocation(0)
                .WithArguments("Assembly");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_GetTypeWithoutReflectionReturn()
        {
            var test = @"
using System;
public class C
{
    void M(object foo)
    {
        var type = foo.GetType();
        var name = type.Name;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_NonReflectionCode()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        var text = ""hello"";
        var length = text.Length;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
