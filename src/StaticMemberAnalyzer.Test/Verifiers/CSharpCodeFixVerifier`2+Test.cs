// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace StaticMemberAnalyzer.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
        {
            public FixAllProvider? FixAllProvider { get; set; }

            public Task VerifyAsync() => RunAsync();

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            {
                var providers = base.GetCodeFixProviders();
                if (FixAllProvider == null) return providers;

                return providers.Select(p => new FixAllProviderWrapper(p, FixAllProvider));
            }

            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });
            }
        }

        private class FixAllProviderWrapper : CodeFixProvider
        {
            private readonly CodeFixProvider _inner;
            private readonly FixAllProvider _fixAll;

            public FixAllProviderWrapper(CodeFixProvider inner, FixAllProvider fixAll)
            {
                _inner = inner;
                _fixAll = fixAll;
            }

            public override ImmutableArray<string> FixableDiagnosticIds => _inner.FixableDiagnosticIds;
            public override FixAllProvider GetFixAllProvider() => _fixAll;
            public override Task RegisterCodeFixesAsync(CodeFixContext context) => _inner.RegisterCodeFixesAsync(context);
        }
    }
}
