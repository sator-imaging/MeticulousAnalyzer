// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ReadOnlyVariableAnalyzerRelaxationTests
    {
        [TestMethod]
        public async Task UriMethodCall_DoesNotReportDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        void M(Uri uri)
        {
            _ = uri.ToString();
            _ = uri.GetHashCode();
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task VersionMethodCall_DoesNotReportDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        void M(Version v)
        {
            _ = v.ToString();
            _ = v.GetHashCode();
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task TypeMethodCall_DoesNotReportDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        void M(Type t)
        {
            _ = t.ToString();
            _ = t.GetHashCode();
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task GuidMethodCall_DoesNotReportDiagnostic()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        void M(Guid g)
        {
            _ = g.ToString();
            _ = g.ToByteArray();
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }

        private static async Task VerifyWithRuleEnabledAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            test.TestState.AnalyzerConfigFiles.Add(
                ("/.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = error"));

            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var compilationOptions = project?.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                var specificOptions = compilationOptions.SpecificDiagnosticOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall,
                    ReportDiagnostic.Error);

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
