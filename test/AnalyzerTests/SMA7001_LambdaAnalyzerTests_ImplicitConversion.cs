// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = global::StaticMemberAnalyzer.Tests.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7001_LambdaAnalyzerTests_ImplicitConversion
    {
        [TestMethod]
        public async Task SMA7001_Compliant_ExplicitDelegateCreation_NewAction()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    void M()
    {
        Action a = new Action(InstanceMethod);
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_StaticFieldOfActionType()
        {
            var test = @"
using System;
public class C
{
    static Action<object> StaticField = static (obj) => { };
    void M()
    {
        // Contravariant conversion from Action<object> to Action<string>
        // Static field member but not a method reference - compliant
        Action<string> a = StaticField;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_StaticPropertyOfFuncType()
        {
            var test = @"
using System;
public class C
{
    static Func<string> StaticProp => static () => ""hello"";
    void M()
    {
        // Covariant conversion from Func<string> to Func<object>
        // Static property member but not a method reference - compliant
        Func<object> f = StaticProp;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Violation_InstanceMethodViaBaseClass()
        {
            var test = @"
using System;
public class Base
{
    public void DoWork() { }
}
public class Derived : Base
{
    void M()
    {
        Action a = {|#0:DoWork|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_NullLiteralAssignment_ImplicitConversion()
        {
            // null literal implicitly converted to Action triggers SMA7001
            var test = @"
using System;
public class C
{
    void M()
    {
        Action a = {|#0:null|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_NonDelegateTargetType_ObjectAssignment()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        object o = 42;
        string s = ""hello"";
        int[] arr = new int[] { 1, 2, 3 };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Violation_InstancePropertyOfDelegateType()
        {
            var test = @"
using System;
public class C
{
    public Action<object> InstanceProp => static (obj) => { };
    void M()
    {
        // Contravariant conversion from Action<object> to Action<string>
        // Instance property is not static, so violation
        Action<string> a = {|#0:InstanceProp|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<string>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_StaticMethodInvocationResult_AssignedToDelegate()
        {
            var test = @"
using System;
public class C
{
    static Action<object> GetAction() => static (obj) => { };
    void M()
    {
        // Static method invocation result with contravariant delegate conversion
        // IsStaticMember uses IInvocationOperation path - static invocation is compliant
        Action<string> a = GetAction();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
