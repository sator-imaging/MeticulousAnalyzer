// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.CatchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
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
        {|#0:catch|} (ArgumentException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8010_Violation_NestedCatch_TryFinally()
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
            {|#0:catch|} (ArgumentException) { }
        }
        finally
        {
            try { }
            {|#1:catch|} (ArgumentException) { }
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
        {|#0:catch|} (ArgumentException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
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
        catch (ArgumentException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
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
        catch (ArgumentException ex)
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
        catch (ArgumentException ex)
        {
            throw new Exception();
        }
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
        {|#0:catch|} (ArgumentException)
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
        catch (ArgumentException)
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
        {|#0:catch|} (ArgumentException)
        {
            try { }
            catch (ArgumentException) { throw; }
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
        {|#0:catch|} (ArgumentException)
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
        catch (ArgumentException)
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
        catch (ArgumentException)
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
        {|#0:catch|} (ArgumentException)
        {
            try { throw new Exception(); }
            {|#1:catch|} (ArgumentException) { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8010_Violation_VariableDeclaration()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|} (ArgumentException argError) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8010_Compliant_Suppression_VariableDeclaration()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        // Ignore exception: Reason here
        catch (ArgumentException argError) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
