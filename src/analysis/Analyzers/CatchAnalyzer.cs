// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CatchAnalyzer : DiagnosticAnalyzer
    {
        private const string SuppressionComment = "// Ignore exception:";

        public const string RuleId_CatchWithoutThrow = "SMA8010";
        private static readonly DiagnosticDescriptor Rule_CatchWithoutThrow = new(
            RuleId_CatchWithoutThrow,
            new LocalizableResourceString(nameof(Resources.SMA8010_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8010_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8010_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_CatchWithoutThrow);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
        }

        private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not CatchClauseSyntax catchClause)
            {
                return;
            }

            if (GuaranteesThrow(catchClause.Block))
            {
                return;
            }

            if (Core.IsSuppressedByComment(catchClause, SuppressionComment))
            {
                var comments = Core.GetPrecedingComments(catchClause);
                if (!comments.TrimEnd().EndsWith(SuppressionComment.Substring(2), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_CatchWithoutThrow, catchClause.CatchKeyword.GetLocation()));
        }

        private static bool GuaranteesThrow(SyntaxNode? node)
        {
            if (node == null) return false;

            if (node is ThrowStatementSyntax) return true;

            if (node is BlockSyntax block)
            {
                foreach (var stmt in block.Statements)
                {
                    if (GuaranteesThrow(stmt)) return true;
                }
            }

            if (node is IfStatementSyntax ifStmt)
            {
                return ifStmt.Else != null && GuaranteesThrow(ifStmt.Statement) && GuaranteesThrow(ifStmt.Else.Statement);
            }

            // Note: ThrowExpressionSyntax is NOT considered as guaranteed throw to satisfy "Don't allow throw in null-coalescing operator"
            // and because it's usually part of expressions that might not be evaluated or don't guarantee block-level throw.

            return false;
        }
    }
}
