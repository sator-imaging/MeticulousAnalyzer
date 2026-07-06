// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.CatchAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8010_CatchAnalyzerTests
    {
        [TestMethod]
        public async Task CatchAnalyzer_Violation_NoThrowInCatch()
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

        try { }
        {|#2:catch|} (ArgumentException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(2));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Violation_NestedCatch()
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

        try { }
        {|#2:catch|} (ArgumentException)
        {
            try { }
            {|#3:catch|} { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(2),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(3));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Violation_SuppressionMissingReason()
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

        try { }
        // Ignore exception:
        {|#2:catch|} (ArgumentException) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(2));
        }

        [TestMethod]
        public async Task CatchAnalyzer_SuppressionByComment()
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
        public async Task CatchAnalyzer_Violation_VariableDeclaration()
        {
            var test = @"
using System;
class C
{
    void M()
    {
        try { }
        {|#0:catch|} (Exception error) { }

        try { }
        {|#1:catch|} (ArgumentException argError) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(1));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Suppression_VariableDeclaration()
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

        try { }
        // Ignore exception: catch(ArgumentException argError) CAN be suppressed
        catch (ArgumentException argError) { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Compliant_ThrowExists()
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

        try { }
        catch (ArgumentException ex) { throw; }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CatchAnalyzer_Compliant_ThrowNewExists()
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

        try { }
        catch (ArgumentException ex) { throw new Exception(); }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CatchAnalyzer_Violation_ThrowInPartialIf()
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

        try { }
        {|#2:catch|} (ArgumentException)
        {
            if (true) throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(2));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Compliant_ThrowInBothBranches()
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
        public async Task CatchAnalyzer_Violation_ThrowInNestedCatch()
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

        try { }
        {|#2:catch|} (ArgumentException)
        {
            try { }
            catch { throw; }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(2));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Violation_ThrowExpressionInNullCoalesce()
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

        try { return o.ToString(); }
        {|#2:catch|} (ArgumentException)
        {
            return o?.ToString() ?? throw new Exception();
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(2));
        }

        [TestMethod]
        public async Task CatchAnalyzer_Compliant_ThrowInTryOfTryFinally()
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
        public async Task CatchAnalyzer_Compliant_ThrowInFinallyOfTryFinally()
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
        public async Task CatchAnalyzer_Violation_ThrowInTryButSwallowedByCatch()
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

        try { }
        {|#4:catch|} (ArgumentException)
        {
            try { throw new Exception(); }
            {|#5:catch|} (ArgumentException) { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(0),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(1),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(2),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchAll).WithLocation(3),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(4),
                VerifyCS.Diagnostic(CatchAnalyzer.RuleId_CatchWithoutThrow).WithLocation(5));
        }
    }
}
