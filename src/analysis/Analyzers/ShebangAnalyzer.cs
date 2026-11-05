// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
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
    }
}
