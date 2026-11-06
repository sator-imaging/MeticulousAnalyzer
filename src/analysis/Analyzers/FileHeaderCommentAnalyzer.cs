// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FileHeaderCommentAnalyzer : DiagnosticAnalyzer
    {
        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_MissingFileHeaderComment = "SMA0050";
        private static readonly DiagnosticDescriptor Rule_MissingFileHeaderComment = new(
            RuleId_MissingFileHeaderComment,
            new LocalizableResourceString(nameof(Resources.SMA0050_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0050_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(FileHeaderCommentAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0050_MessageFormat), Resources.ResourceManager, typeof(Resources)));

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_MissingFileHeaderComment
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            context.RegisterSyntaxTreeAction(Analyze);
        }


        private static void Analyze(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);
            if (root.FullSpan.IsEmpty)
            {
                return; // Empty file
            }

            var text = context.Tree.GetText(context.CancellationToken);

            // TODO: Roslyn version used in this project does not support shebang classification (ShebangDirectiveTrivia)
            //       and instead parses a leading '#!' as a preprocessor directive (e.g. BadDirectiveTrivia).
            //       Therefore, we check the raw source text for the '#!' prefix rather than relying on Roslyn's classification.
            var hasShebang = text.Length >= 2 && text[0] == '#' && text[1] == '!';

            var triviaList = root.GetLeadingTrivia();
            var skipShebang = hasShebang;

            foreach (var trivia in triviaList)
            {
                if (skipShebang)
                {
                    // Skip the shebang directive trivia at the start of the file.
                    skipShebang = false;
                    continue;
                }

                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                        return; // Found a comment, OK.
                }

                break;
            }

            // If we are here, it means no comment was found before the first token,
            // or the file consists only of whitespace and/or directives.
            var lineIndex = hasShebang && text.Lines.Count > 1 ? 1 : 0;
            var targetLine = text.Lines[lineIndex];
            var span = targetLine.Span;
            if (span.IsEmpty)
            {
                var firstToken = root.GetFirstToken(includeZeroWidth: false,
                                                    includeSkipped: true,
                                                    includeDirectives: !hasShebang,
                                                    includeDocumentationComments: true);

                span = firstToken.Span;
                if (span.IsEmpty)
                {
                    span = root.FullSpan;
                }
            }

            var location = Location.Create(context.Tree, span);
            context.ReportDiagnostic(Diagnostic.Create(Rule_MissingFileHeaderComment, location));
        }
    }
}
