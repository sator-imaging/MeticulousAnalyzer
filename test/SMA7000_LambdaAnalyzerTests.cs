// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<LambdaAnalyzer>;

    [TestClass]
    public class SMA7000_LambdaAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7000_Compliant_StaticLambda()
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
        public async Task SMA7000_Compliant_LambdaCapturingVariableCommentInArgument()
        {
            var test = @"
using System;
public class C
{
    void Foo(Action a) { }
    void M()
    {
        int x = 0;
        Foo(
            // Allow allocation
            () => { x++; }
        );
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7000_Compliant_LambdaCapturingVariableCommentInArgumentWithParams()
        {
            var test = @"
using System;
public class C
{
    void Foo(int i, Action<int> a) { }
    void M()
    {
        int x = 0;
        Foo(1,
            // Allow allocation
            (args) => { x++; }
        );
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7000_Compliant_NonActionFuncStaticMethodConversionNot()
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
        public async Task SMA7000_Compliant_LambdaCapturingVariableComment()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        int x = 0;
        // Allow allocation
        Action a = () => { x++; };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
