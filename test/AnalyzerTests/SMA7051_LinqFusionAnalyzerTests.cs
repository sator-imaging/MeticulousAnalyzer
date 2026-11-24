// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<LinqFusionAnalyzer>;

    [TestClass]
    public class SMA7051_LinqFusionAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7051_Violation_WhereAny()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public bool M(IEnumerable<int> items)
        => {|#0:items.Where(x => x > 0).Any()|};
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };
            CSharpVerifierHelper.SetAssemblyName(test, "Application");
            test.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(LinqFusionAnalyzer.RuleId_LinqFusion)
                    .WithLocation(markupKey: 0));
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA7051_Compliant_UnmatchedTerminal()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public int M(IEnumerable<int> items)
        => items.Where(x => x > 0).Count();
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
