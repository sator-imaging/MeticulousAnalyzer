// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ShebangAnalyzer : DiagnosticAnalyzer
    {
        private const string TargetFileName = "build.cs";

        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_MissingShebang = "SMA0051";
        private static readonly DiagnosticDescriptor Rule_MissingShebang = new(
            RuleId_MissingShebang,
            new LocalizableResourceString(nameof(Resources.SMA0051_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0051_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(ShebangAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0051_MessageFormat), Resources.ResourceManager, typeof(Resources)));

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_MissingShebang
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            if (!IsTargetFile(tree.FilePath))
            {
                return;
            }

            var text = tree.GetText(context.CancellationToken);
            if (HasShebang(text))
            {
                return;
            }

            var span = GetDiagnosticLocation(tree, text, context.CancellationToken);

            var location = Location.Create(tree, span);
            context.ReportDiagnostic(Diagnostic.Create(Rule_MissingShebang, location, TargetFileName));
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            var additionalFiles = context.Options.AdditionalFiles;
            if (additionalFiles.IsDefaultOrEmpty)
            {
                return;
            }

            CSharpParseOptions? parseOptions = null;
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                if (tree.Options is not CSharpParseOptions options)
                {
                    continue;
                }

                parseOptions = options;
                break;
            }

            parseOptions ??= CSharpParseOptions.Default;

            var ct = context.CancellationToken;
            for (int i = 0, count = additionalFiles.Length; i < count; i++)
            {
                var file = additionalFiles[i];
                var filePath = file.Path;

                if (!IsTargetFile(filePath))
                {
                    continue;
                }

                var text = file.GetText(ct);
                if (text is null || HasShebang(text))
                {
                    continue;
                }

                var tree = CSharpSyntaxTree.ParseText(text, options: parseOptions, path: filePath, ct);
                var span = GetDiagnosticLocation(tree, text, ct);

                var location = Location.Create(filePath, span, tree.GetLineSpan(span).Span);
                context.ReportDiagnostic(Diagnostic.Create(Rule_MissingShebang, location, TargetFileName));
            }
        }

        private static bool IsTargetFile(string filePath)
        {
            // perf: checking end of the path can eliminate 99.99% of the files
            return filePath.EndsWith(TargetFileName, StringComparison.Ordinal)
                && (
                    filePath.EndsWith("\\" + TargetFileName, StringComparison.Ordinal)
                    || filePath.EndsWith("/" + TargetFileName, StringComparison.Ordinal)
                );
        }

        private static bool HasShebang(SourceText text)
        {
            return text.Length >= 2 && text[0] == '#' && text[1] == '!';
        }

        private static TextSpan GetDiagnosticLocation(SyntaxTree tree, SourceText text, CancellationToken ct)
        {
            var firstLine = text.Lines[0];
            var span = firstLine.Span;
            if (span.IsEmpty)
            {
                var root = tree.GetRoot(ct);
                var firstToken = root.GetFirstToken(includeZeroWidth: false,
                                                    includeSkipped: true,
                                                    includeDirectives: true,
                                                    includeDocumentationComments: true);

                span = firstToken.Span;
                if (span.IsEmpty)
                {
                    span = root.FullSpan;
                }
            }

            return span;
        }
    }
}
