// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.CatchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8011_CatchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8011_Violation_NoThrowInCatch_CatchAll()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|} { }

        try { }
        {|#1:catch|} (Exception) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Violation_NestedCatch_TryFinally()
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
            {|#1:catch|} (Exception) { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Violation_SuppressionMissingReason()
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

        try { }
        // Ignore exception:
        {|#1:catch|} (Exception) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Violation_Suppression_NotAllowed()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        // Ignore exception: catch-all cannot be suppressed
        {|#0:catch|} { }

        try { }
        // Ignore exception: catch(Exception) cannot be suppressed
        {|#1:catch|} (Exception) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Violation_3Pair_Suppression()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        // Ignore exception: catch-all cannot be suppressed
        {|#0:catch|} { }

        try { }
        // Ignore exception: catch(Exception) cannot be suppressed
        {|#1:catch|} (Exception) { }

        try { }
        // Ignore exception: catch(ArgumentException) CAN be suppressed
        catch (ArgumentException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Compliant_ThrowExists()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        catch { throw; }

        try { }
        catch (Exception ex) { throw; }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8011_Compliant_ThrowNewExists()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        catch { throw new Exception(); }

        try { }
        catch (Exception ex) { throw new Exception(); }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8011_Violation_ThrowInPartialIf()
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

        try { }
        {|#1:catch|} (Exception)
        {
            if (true) throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Compliant_ThrowInBothBranches()
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

        try { }
        catch (Exception)
        {
            if (DateTime.Now.Second % 2 == 0) throw new Exception(""even"");
            else throw new Exception(""odd"");
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8011_Violation_ThrowInNestedCatch()
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

        try { }
        {|#1:catch|} (Exception)
        {
            try { }
            catch { throw; }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Violation_ThrowExpressionInNullCoalesce()
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

        try { return o.ToString(); }
        {|#1:catch|} (Exception)
        {
            return o?.ToString() ?? throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8011_Compliant_ThrowInTryOfTryFinally()
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

        try { }
        catch (Exception)
        {
            try { throw new Exception(); }
            finally { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8011_Compliant_ThrowInFinallyOfTryFinally()
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

        try { }
        catch (Exception)
        {
            try { }
            finally { throw new Exception(); }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8011_Violation_ThrowInTryButSwallowedByCatch()
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

        try { }
        {|#2:catch|} (Exception)
        {
            try { throw new Exception(); }
            {|#3:catch|} (Exception) { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(2),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(3));
        }

        [TestMethod]
        public async Task SMA8011_Violation_VariableDeclaration()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|} (Exception error) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8011_Violation_Suppression_VariableDeclaration_NotAllowed()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        // Ignore exception: catch(Exception error) cannot be suppressed
        {|#0:catch|} (Exception error) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0));
        }
    }
}
