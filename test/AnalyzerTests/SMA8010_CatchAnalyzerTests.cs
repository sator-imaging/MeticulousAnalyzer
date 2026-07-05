// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.CatchAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8010_CatchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8010_Violation_NoThrowInCatch()
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
        public async Task SMA8010_Violation_NoThrowInCatch_WithoutType()
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
        public async Task SMA8010_Violation_NestedCatch()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try
        {
            try { }
            {|#0:catch|} { }
        }
        finally
        {
            try { }
            {|#1:catch|} { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8010_Violation_SuppressionMissingReason()
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
        public async Task SMA8010_Compliant_ThrowExists()
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
        public async Task SMA8010_Compliant_ThrowNewExists()
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
        public async Task SMA8010_Compliant_SuppressionWithReason()
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
        public async Task SMA8010_Violation_ThrowInPartialIf()
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
            if (true) throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8010_Compliant_ThrowInBothBranches()
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
            if (DateTime.Now.Second % 2 == 0) throw new Exception(""even"");
            else throw new Exception(""odd"");
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8010_Violation_ThrowInNestedCatch()
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
        public async Task SMA8010_Violation_ThrowExpressionInNullCoalesce()
        {
            var test = @"
using System;
class C
{
    string M(object o)
    {
        try { return o.ToString(); }
        {|#0:catch|}
        {
            return o?.ToString() ?? throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8010_Compliant_ThrowInTryOfTryFinally()
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
            try { throw new Exception(); }
            finally { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8010_Compliant_ThrowInFinallyOfTryFinally()
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
            try { }
            finally { throw new Exception(); }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8010_Violation_ThrowInTryButSwallowedByCatch()
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
            try { throw new Exception(); }
            {|#1:catch|} { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(1));
        }
    }
}
