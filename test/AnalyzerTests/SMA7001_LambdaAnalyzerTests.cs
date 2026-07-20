// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7001_LambdaAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7001_Violation_ImplicitConversionFromInstanceMethod()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    static void StaticMethod() { }

    void M()
    {
        Action a1 = {|#0:InstanceMethod|};
        Action a2 = {|#1:StaticMethod|};
    }
}
";
            var expected0 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            var expected1 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 1)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7001_Violation_ImplicitConversionFromInstanceMethodInArgument()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod() { }
    void Call(Action a) { }

    void M()
    {
        Call({|#0:InstanceMethod|});
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_ImplicitConversionWithGenericAction()
        {
            var test = @"
using System;
public class C
{
    void InstanceMethod(int i) { }
    void M()
    {
        Action<int> a = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_ImplicitConversionWithGenericFunc()
        {
            var test = @"
using System;
public class C
{
    int InstanceMethod() => 0;
    void M()
    {
        Func<int> f = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Func<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_NonActionFuncDelegateConversion()
        {
            var test = @"
using System;
public delegate void MyDelegate();
public class C
{
    void InstanceMethod() { }
    void M()
    {
        MyDelegate d = {|#0:InstanceMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDelegate");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_ImplicitConversionFromInstanceField()
        {
            var test = @"
using System;
public class C
{
    public Action<object> InstanceField;
    void M()
    {
        // Contravariant conversion from Action<object> to Action<string>
        Action<string> a = {|#0:InstanceField|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action<string>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_ExplicitDelegateCreation()
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
        public async Task SMA7001_Compliant_LambdaAssignedToDelegate()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        Action a = static () => { };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Violation_StaticMethodOfActionFuncAssignedToAction()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod() { }
    void M()
    {
        Action a = {|#0:StaticMethod|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_StaticMethodOfFuncAssignedToFunc()
        {
            var test = @"
using System;
public class C
{
    static int StaticFunc() => 42;
    void M()
    {
        Func<int> f = {|#0:StaticFunc|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Func<int>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_StaticMethodAssignedToCustomDelegate()
        {
            var test = @"
using System;
public delegate void MyDelegate();
public class C
{
    static void StaticMethod() { }
    void M()
    {
        MyDelegate d = StaticMethod;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_StaticMethodWithParamsAssignedToCustomDelegate()
        {
            var test = @"
using System;
public delegate int MyFunc(int x);
public class C
{
    static int StaticMethod(int x) => x;
    void M()
    {
        MyFunc f = StaticMethod;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Violation_InstanceMethodWithReceiverConversion()
        {
            var test = @"
using System;
public class Other { public void DoWork() { } }
public class C
{
    void M()
    {
        var obj = new Other();
        Action a = {|#0:obj.DoWork|};
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Violation_StaticMethodInArgumentToActionParam()
        {
            var test = @"
using System;
public class C
{
    static void StaticMethod() { }
    void Call(Action a) { }
    void M()
    {
        Call({|#0:StaticMethod|});
    }
}
";
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("Action");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_NonDelegateTargetType()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        object o = ""hello"";
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7001_Compliant_StaticMethodAssignedToNonActionFuncDelegate()
        {
            var test = @"
using System;
public delegate void MyDelegate();
public class C
{
    static void StaticMethod() { }
    void M()
    {
        MyDelegate d = StaticMethod;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
