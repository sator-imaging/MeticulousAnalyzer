// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<LinqOperationAnalyzer>;

    [TestClass]
    public class SMA7050_LinqOperationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7050_Violation_SystemLinq()
        {
            var source = @"
{|#0:using System.Linq;|}

public class C
{
}
";
            var expected = VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 0);
            await VerifyAnalyzerInNonTestProjectAsync(source, expected);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_TestProject()
        {
            var test = @"
using System.Linq;

public class C
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_SuppressionComment()
        {
            var test = @"
// Allow linq operation
using System.Linq;

public class C
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_SuppressionCommentWithReason()
        {
            var test = @"
// Allow linq operation: Need Linq Enumerable in legacy helper
using System.Linq;

public class C
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_OtherNamespaces()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public class C
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_StaticUsing()
        {
            var test = @"
using static System.Linq.Enumerable;

public class C
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_AliasUsing()
        {
            var test = @"
using Linq = System.Linq.Enumerable;

public class C
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Compliant_InsideNamespace_Suppressed()
        {
            var test = @"
namespace Test
{
    // Allow linq operation
    using System.Linq;

    public class C
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7050_Violation_InsideNamespace()
        {
            var test = @"
namespace Test
{
    {|#0:using System.Linq;|}

    public class C
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(LinqOperationAnalyzer.RuleId_LinqOperation).WithLocation(markupKey: 0);
            await VerifyAnalyzerInNonTestProjectAsync(test, expected);
        }

        private static async Task VerifyAnalyzerInNonTestProjectAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };
            test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectAssemblyName(projectId, "Application"));
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
