// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests
{
    public static class DebugAssertAnalyzerVerifier
    {
        public static DiagnosticResult Diagnostic()
            => CSharpAnalyzerVerifier<DebugAssertAnalyzer, MSTestVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<DebugAssertAnalyzer, MSTestVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpAnalyzerVerifier<DebugAssertAnalyzer, MSTestVerifier>.Diagnostic(descriptor);

        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
            => await VerifyAnalyzerAsync(source, "ProductionProject", expected);

        public static async Task VerifyAnalyzerAsync(string source, string assemblyName, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerVerifier<DebugAssertAnalyzer>.Test
            {
                TestCode = source,
            };
            test.SolutionTransforms.Add((solution, projectId) =>
                solution.WithProjectAssemblyName(projectId, assemblyName));

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
