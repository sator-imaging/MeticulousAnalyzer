// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<MethodImplAnalyzer>;

    [TestClass]
    public class SMA7003_MethodImplAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7003_Compliant_Method_Standard()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_Method_Internal()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void M()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_Method_Private()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void M()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Compliant_Method_Protected()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void M()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7003_Violation_Method_AggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void {|#0:M|}()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_MethodImplAggressiveInlining)
                .WithLocation(markupKey: 0)
                .WithArguments("M");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Violation_Constructor_AggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public {|#0:C|}()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_MethodImplAggressiveInlining)
                .WithLocation(markupKey: 0)
                .WithArguments("C");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7003_Violation_PropertyAccessor_AggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    public string Prop
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        {|#0:get|} => null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        {|#1:set|} { }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_MethodImplAggressiveInlining)
                .WithLocation(markupKey: 0)
                .WithArguments("Prop.get");
            var expected1 = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_MethodImplAggressiveInlining)
                .WithLocation(markupKey: 1)
                .WithArguments("Prop.set");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA7003_Violation_IndexerAccessor_AggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;
public class C
{
    public string this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        {|#0:get|} => null;

        [MethodImpl(256)]
        {|#1:set|} { }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_MethodImplAggressiveInlining)
                .WithLocation(markupKey: 0)
                .WithArguments("this.get");
            var expected1 = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_MethodImplAggressiveInlining)
                .WithLocation(markupKey: 1)
                .WithArguments("this.set");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
