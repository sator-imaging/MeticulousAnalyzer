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

        public const string RuleId_CatchWithoutThrow = "SMA0005";
        private static readonly DiagnosticDescriptor Rule_CatchWithoutThrow = new(
            RuleId_CatchWithoutThrow,
            new LocalizableResourceString(nameof(Resources.SMA0005_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0005_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0005_Description), Resources.ResourceManager, typeof(Resources)));

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

            if (HasThrowStatement(catchClause.Block))
            {
                return;
            }

            if (Core.IsSuppressedByComment(catchClause, SuppressionComment))
            {
                var comments = Core.GetPrecedingComments(catchClause);
                if (!comments.TrimEnd().EndsWith("Ignore exception:", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_CatchWithoutThrow, catchClause.CatchKeyword.GetLocation()));
        }

        private static bool HasThrowStatement(BlockSyntax? block)
        {
            if (block == null) return false;

            // check for throw statement or expression inside this block, but not inside nested catch clauses, lambdas or local functions
            foreach (var descendant in block.DescendantNodes(node =>
                node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or CatchClauseSyntax)))
            {
                if (descendant is ThrowStatementSyntax or ThrowExpressionSyntax)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
