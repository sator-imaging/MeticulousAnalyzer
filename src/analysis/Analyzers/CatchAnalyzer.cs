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
        private const string SuppressionCommentPrefix = "Ignore exception:";
        private const string SuppressionComment = "// " + SuppressionCommentPrefix;

        public const string RuleId_CatchWithoutThrow = "SMA8010";
        private static readonly DiagnosticDescriptor Rule_CatchWithoutThrow = new(
            RuleId_CatchWithoutThrow,
            new LocalizableResourceString(nameof(Resources.SMA8010_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8010_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8010_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_CatchAll = "SMA8011";
        private static readonly DiagnosticDescriptor Rule_CatchAll = new(
            RuleId_CatchAll,
            new LocalizableResourceString(nameof(Resources.SMA8011_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8011_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8011_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_CatchWithoutThrow,
            Rule_CatchAll);

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

            // 1. If the catch block re-throws or guarantees a throw, it's compliant regardless of the exception type.
            if (GuaranteesThrow(catchClause.Block))
            {
                return;
            }

            // 2. Identify "catch-all" blocks: `catch { ... }` or `catch (System.Exception ex) { ... }`.
            var isCatchAll = catchClause.Declaration == null;
            if (catchClause.Declaration != null)
            {
                var typeSymbol = context.SemanticModel.GetTypeInfo(catchClause.Declaration.Type).Type;
                var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
                isCatchAll = SymbolEqualityComparer.Default.Equals(typeSymbol, exceptionType);
            }

            // 3. Catch-all blocks without a throw (SMA8011) are NOT ignorable by comment.
            if (isCatchAll)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_CatchAll, catchClause.CatchKeyword.GetLocation()));
                return;
            }

            // 4. Other catch blocks without a throw (SMA8010) CAN be suppressed by a specific comment.
            if (Core.IsSuppressedByComment(catchClause, SuppressionComment))
            {
                var comments = Core.GetPrecedingComments(catchClause);
                if (!comments.EndsWith(SuppressionCommentPrefix, StringComparison.OrdinalIgnoreCase))
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

            // TODO: To be determined.
            /*
            // "Wrapper" statements whose body determines whether they guarantee a throw.
            if (node is UsingStatementSyntax usingStmt) return GuaranteesThrow(usingStmt.Statement);
            if (node is LockStatementSyntax lockStmt) return GuaranteesThrow(lockStmt.Statement);
            if (node is FixedStatementSyntax fixedStmt) return GuaranteesThrow(fixedStmt.Statement);
            if (node is CheckedStatementSyntax checkedStmt) return GuaranteesThrow(checkedStmt.Block);
            if (node is UnsafeStatementSyntax unsafeStmt) return GuaranteesThrow(unsafeStmt.Block);
            if (node is LabeledStatementSyntax labeledStmt) return GuaranteesThrow(labeledStmt.Statement);
            */

            if (node is BlockSyntax block)
            {
                foreach (var stmt in block.Statements)
                {
                    if (GuaranteesThrow(stmt)) return true;
                }
            }

            if (node is IfStatementSyntax ifStmt)
            {
                // Don't allow if-only statement. e.g., `if (condition) throw new...`.
                // All code paths must throw.
                return ifStmt.Else != null && GuaranteesThrow(ifStmt.Statement) && GuaranteesThrow(ifStmt.Else.Statement);
            }

            // Try-finally (no-catch) does not swallow exceptions; treat it like a wrapper, but ignore try/catch nesting.
            if (node is TryStatementSyntax tryStmt && tryStmt.Catches.Count == 0)
            {
                return GuaranteesThrow(tryStmt.Block)
                    || (tryStmt.Finally != null && GuaranteesThrow(tryStmt.Finally.Block));
            }

            // Note: ThrowExpressionSyntax is NOT considered as guaranteed throw to satisfy "Don't allow throw in null-coalescing operator"
            // and because it's usually part of expressions that might not be evaluated or don't guarantee block-level throw.

            return false;
        }
    }
}
