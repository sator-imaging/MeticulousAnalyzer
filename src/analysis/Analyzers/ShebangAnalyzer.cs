// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.IO;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ShebangAnalyzer : DiagnosticAnalyzer
    {
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

            context.RegisterSyntaxTreeAction(Analyze);
            context.RegisterCompilationAction(AnalyzeCompilation);
        }


        private static void Analyze(SyntaxTreeAnalysisContext context)
        {
            var filePath = context.Tree.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var fileName = Path.GetFileName(filePath);
            if (fileName != "build.cs")
            {
                return;
            }

            var text = context.Tree.GetText(context.CancellationToken);
            if (text.Length >= 2 && text[0] == '#' && text[1] == '!')
            {
                return; // Has shebang at the beginning of the file
            }

            var firstLine = text.Lines[0];
            var span = firstLine.Span;
            if (span.IsEmpty)
            {
                var root = context.Tree.GetRoot(context.CancellationToken);
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

            var location = Location.Create(context.Tree, span);
            context.ReportDiagnostic(Diagnostic.Create(Rule_MissingShebang, location, fileName));
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
                if (tree.Options is CSharpParseOptions options)
                {
                    parseOptions = options;
                    break;
                }
            }
            parseOptions ??= CSharpParseOptions.Default;

            for (int i = 0, count = additionalFiles.Length; i < count; i++)
            {
                var file = additionalFiles[i];
                var filePath = file.Path;
                if (string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                var fileName = Path.GetFileName(filePath);
                if (fileName != "build.cs")
                {
                    continue;
                }

                var text = file.GetText(context.CancellationToken);
                if (text is null)
                {
                    continue;
                }

                if (text.Length >= 2 && text[0] == '#' && text[1] == '!')
                {
                    continue; // Has shebang at the beginning of the file
                }

                var tree = CSharpSyntaxTree.ParseText(text, parseOptions, filePath, context.CancellationToken);
                var firstLine = text.Lines[0];
                var span = firstLine.Span;
                if (span.IsEmpty)
                {
                    var root = tree.GetRoot(context.CancellationToken);
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

                var location = Location.Create(filePath, span, tree.GetLineSpan(span).Span);
                context.ReportDiagnostic(Diagnostic.Create(Rule_MissingShebang, location, fileName));
            }
        }
    }
}
