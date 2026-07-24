// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MethodImplAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA7020_MethodImplAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7020_Violation_Method_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
    public void MyMethod()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyMethod");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Violation_Constructor_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
    public TestClass()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Violation_PropertyAccessor_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    public int MyProp
    {
        [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        get => 42;
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyProp.get");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Violation_IndexerAccessor_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    public int this[int index]
    {
        [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        get => index;
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("this.get");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Violation_Method_WithCombinedInliningFlags()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.NoOptimization)|}]
    public void MyMethod()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyMethod");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Compliant_Method_WithoutAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void MyMethod()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7020_Compliant_InternalMethod_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

internal class TestClass
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MyMethod()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
