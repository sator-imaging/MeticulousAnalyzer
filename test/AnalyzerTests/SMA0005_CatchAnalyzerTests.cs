// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.CatchAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test.AnalyzerTests
{
    [TestClass]
    public class SMA0005_CatchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0005_Violation_NoThrowInCatch()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|} (InvalidOperationException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA0005_Violation_NoThrowInCatch_WithoutType()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|} { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA0005_Violation_NestedCatch()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        finally
        {
            try { }
            {|#0:catch|} { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA0005_Violation_SuppressionMissingReason()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        // Ignore exception:
        {|#0:catch|} { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA0005_Compliant_ThrowExists()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        catch (Exception ex)
        {
            throw;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0005_Compliant_ThrowNewExists()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        catch (Exception ex)
        {
            throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0005_Compliant_SuppressionWithReason()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        // Ignore exception: Reason here
        catch { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0005_Compliant_ThrowInNestedBlock()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        catch
        {
            if (true) throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0005_Violation_ThrowInNestedCatch()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|}
        {
            try { }
            catch { throw; }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA0005_Compliant_ThrowExpression()
        {
            var test = @"
using System;
class C
{
    string M(object o)
    {
        try { return o.ToString(); }
        catch
        {
            return o?.ToString() ?? throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
