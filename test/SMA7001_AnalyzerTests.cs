// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7001_AnalyzerTests
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
            var expected0 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
            var expected1 = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 1)
                .WithArguments("System.Action");
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action");
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<int>");
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Func<int>");
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
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
            var expected = VerifyCS.Diagnostic(LambdaAnalyzer.RuleId_ImplicitConversionToDelegate)
                .WithLocation(markupKey: 0)
                .WithArguments("System.Action<string>");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
