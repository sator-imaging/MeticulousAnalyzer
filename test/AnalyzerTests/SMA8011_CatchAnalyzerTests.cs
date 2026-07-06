// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpAnalyzerVerifier<SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.CatchAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
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
        public async Task SMA8011_Violation_SuppressionByComment_NotAllowed()
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
    }
}
