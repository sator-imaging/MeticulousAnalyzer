using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

namespace StaticMemberAnalyzer.Test
{
    // This verifier is a wrapper around CSharpAnalyzerVerifier to disable the automatic #pragma warning disable test.
    public static class FileHeaderCommentAnalyzerVerifier
    {
        public static DiagnosticResult Diagnostic()
            => CSharpAnalyzerVerifier<FileHeaderCommentAnalyzer, MSTestVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<FileHeaderCommentAnalyzer, MSTestVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpAnalyzerVerifier<FileHeaderCommentAnalyzer, MSTestVerifier>.Diagnostic(descriptor);

        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerVerifier<FileHeaderCommentAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestBehaviors |= TestBehaviors.SkipSuppressionCheck;

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
