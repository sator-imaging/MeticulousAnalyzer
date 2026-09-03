// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MidFlowBranchAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_MidFlowBranch = "SMA8030";
        public const string RuleId_StateChangeInEarlyReturn = "SMA8031";

        private const string SuppressionComment = "// Early exit";

        private static readonly DiagnosticDescriptor Rule = new(
            RuleId_MidFlowBranch,
            new LocalizableResourceString(nameof(Resources.SMA8030_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8030_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MidFlowBranchAnalyzer),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8030_Description), Resources.ResourceManager, typeof(Resources)));

        private static readonly DiagnosticDescriptor Rule_StateChangeInEarlyReturn = new(
            RuleId_StateChangeInEarlyReturn,
            new LocalizableResourceString(nameof(Resources.SMA8031_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8031_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(MidFlowBranchAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8031_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, Rule_StateChangeInEarlyReturn);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeBlock, SyntaxKind.Block);
        }

        private static void AnalyzeBlock(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not BlockSyntax block)
                return;

            bool isMainFlowStarted = false;
            bool hasDeclarationInCurrentSequence = false;
            bool hasSeenIf = false;

            foreach (var statement in block.Statements)
            {
                if (statement is EmptyStatementSyntax)
                {
                    continue;
                }

                if (statement is LocalDeclarationStatementSyntax ||
                    (statement is ExpressionStatementSyntax exprStmt && exprStmt.Expression is AssignmentExpressionSyntax assign && IsTupleDeclaration(assign)))
                {
                    if (isMainFlowStarted)
                    {
                        continue;
                    }

                    if (hasDeclarationInCurrentSequence && hasSeenIf)
                    {
                        isMainFlowStarted = true;
                    }
                    else
                    {
                        hasDeclarationInCurrentSequence = true;
                    }
                    continue;
                }

                if (statement is IfStatementSyntax ifStmt)
                {
                    hasSeenIf = true;
                    if (isMainFlowStarted || ifStmt.Else != null)
                    {
                        isMainFlowStarted = true;
                        if (!HasEarlyExitSuppression(ifStmt))
                        {
                            CheckAndReportMidFlowBranches(context, ifStmt);
                        }
                    }
                    else
                    {
                        CheckStateChangeInEarlyReturnIf(context, ifStmt);

                        if (ContainsBranch(ifStmt))
                        {
                            hasDeclarationInCurrentSequence = false;
                        }
                        else
                        {
                            isMainFlowStarted = true;
                        }
                    }
                }
                else
                {
                    isMainFlowStarted = true;
                }
            }
        }

        private static bool HasEarlyExitSuppression(IfStatementSyntax ifStmt)
        {
            var comment = Core.GetFirstSingleLineCommentTrivia(ifStmt);

            // SyntaxTrivia and TextSpan are struct. `!= default` invokes Equals including nested structs' Equals.
            // Checking Length is enough and efficient.
            return comment.Span.Length >= SuppressionComment.Length
                && comment.ToString().StartsWith(SuppressionComment, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckStateChangeInEarlyReturnIf(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStmt)
        {
            if (ifStmt.Statement is BlockSyntax ifBlock)
            {
                CheckEarlyReturnBlock(context, ifBlock);
            }

            if (ifStmt.Else != null)
            {
                if (ifStmt.Else.Statement is IfStatementSyntax elseIf)
                {
                    CheckStateChangeInEarlyReturnIf(context, elseIf);
                }
                else if (ifStmt.Else.Statement is BlockSyntax elseBlock)
                {
                    CheckEarlyReturnBlock(context, elseBlock);
                }
            }
        }

        private static void CheckEarlyReturnBlock(SyntaxNodeAnalysisContext context, BlockSyntax block)
        {
            bool hasDisallowedStatement = false;

            foreach (var statement in block.Statements)
            {
                var branchLoc = GetBranchLocation(statement);
                if (branchLoc != null)
                {
                    if (hasDisallowedStatement)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule_StateChangeInEarlyReturn, branchLoc));
                    }
                    break;
                }

                if (statement is EmptyStatementSyntax)
                {
                    continue;
                }
                else if (statement is LocalDeclarationStatementSyntax)
                {
                    continue;
                }
                else if (statement is ExpressionStatementSyntax exprStmt)
                {
                    if (exprStmt.Expression is AssignmentExpressionSyntax assign &&
                        (IsTupleDeclaration(assign) || IsOutParameterAssignment(context, assign)))
                    {
                        continue;
                    }
                }

                hasDisallowedStatement = true;
            }
        }

        private static Location? GetBranchLocation(SyntaxNode node)
        {
            return node switch
            {
                ReturnStatementSyntax returnStmt => returnStmt.ReturnKeyword.GetLocation(),
                YieldStatementSyntax yieldStmt => yieldStmt.YieldKeyword.GetLocation(),
                ContinueStatementSyntax continueStmt => continueStmt.ContinueKeyword.GetLocation(),
                BreakStatementSyntax breakStmt => breakStmt.BreakKeyword.GetLocation(),
                GotoStatementSyntax gotoStmt => gotoStmt.GotoKeyword.GetLocation(),
                ThrowStatementSyntax throwStmt => throwStmt.ThrowKeyword.GetLocation(),
                _ => null,
            };
        }

        private static bool IsOutParameterAssignment(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assign)
        {
            if (assign.Left is TupleExpressionSyntax)
            {
                return false;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(assign.Left).Symbol;
            return symbol is IParameterSymbol param && param.RefKind == RefKind.Out;
        }

        private static bool IsTupleDeclaration(AssignmentExpressionSyntax syntax)
        {
            if (syntax.Left is TupleExpressionSyntax tuple)
            {
                foreach (var arg in tuple.Arguments)
                {
                    if (arg.Expression is not DeclarationExpressionSyntax)
                    {
                        return false;
                    }
                }

                return true;
            }

            return syntax.Left is DeclarationExpressionSyntax;
        }

        private static bool ShouldDescendInto(SyntaxNode node)
        {
            return !(node is LocalFunctionStatementSyntax
                or AnonymousFunctionExpressionSyntax
                or ForStatementSyntax
                or ForEachStatementSyntax
                or ForEachVariableStatementSyntax
                or WhileStatementSyntax
                or DoStatementSyntax);
        }

        private static bool ContainsBranch(SyntaxNode node)
        {
            if (node is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax or GotoStatementSyntax)
                return true;

            foreach (var descendant in node.DescendantNodes(static x => ShouldDescendInto(x)))
            {
                if (descendant is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax or GotoStatementSyntax)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckAndReportMidFlowBranches(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStmt)
        {
            if (AllBranchesBranch(ifStmt))
            {
                return;
            }

            CollectAndReportBranchesInIfBranch(context, ifStmt);
        }

        private static void CollectAndReportBranchesInIfBranch(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStmt)
        {
            IfStatementSyntax? currentIf = ifStmt;
            while (currentIf != null)
            {
                ReportBranchesInStatement(context, currentIf.Statement);

                if (currentIf.Else != null)
                {
                    if (currentIf.Else.Statement is IfStatementSyntax elseIf)
                    {
                        currentIf = elseIf;
                    }
                    else
                    {
                        ReportBranchesInStatement(context, currentIf.Else.Statement);
                        currentIf = null;
                    }
                }
                else
                {
                    currentIf = null;
                }
            }
        }

        private static void ReportBranchesInStatement(SyntaxNodeAnalysisContext context, StatementSyntax branchStatement)
        {
            CheckAndReportNode(context, branchStatement);
            foreach (var node in branchStatement.DescendantNodes(static x => ShouldDescendInto(x)))
            {
                CheckAndReportNode(context, node);
            }
        }

        private static void CheckAndReportNode(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            if (node is ReturnStatementSyntax returnStmt)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, returnStmt.ReturnKeyword.GetLocation()));
            }
            else if (node is YieldStatementSyntax yieldStmt)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, yieldStmt.YieldKeyword.GetLocation()));
            }
            else if (node is ContinueStatementSyntax continueStmt)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, continueStmt.ContinueKeyword.GetLocation()));
            }
            else if (node is BreakStatementSyntax breakStmt)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, breakStmt.BreakKeyword.GetLocation()));
            }
            else if (node is GotoStatementSyntax gotoStmt)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, gotoStmt.GotoKeyword.GetLocation()));
            }
            else if (node is ThrowStatementSyntax throwStmt)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, throwStmt.ThrowKeyword.GetLocation()));
            }
        }

        private static bool AllBranchesBranch(IfStatementSyntax ifStmt)
        {
            IfStatementSyntax? current = ifStmt;
            while (current != null)
            {
                if (!BranchGuaranteesBranch(current.Statement))
                    return false;

                if (current.Else == null)
                    return false;

                if (current.Else.Statement is IfStatementSyntax elseIf)
                {
                    current = elseIf;
                }
                else
                {
                    return BranchGuaranteesBranch(current.Else.Statement);
                }
            }

            return false;
        }

        private static bool BranchGuaranteesBranch(StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
            {
                foreach (var stmt in block.Statements)
                {
                    if (StatementGuaranteesBranch(stmt))
                        return true;
                }
                return false;
            }

            return StatementGuaranteesBranch(statement);
        }

        private static bool StatementGuaranteesBranch(StatementSyntax statement)
        {
            if (statement is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax or GotoStatementSyntax)
                return true;

            if (statement is IfStatementSyntax innerIf)
            {
                return AllBranchesBranch(innerIf);
            }

            if (statement is BlockSyntax block)
            {
                return BranchGuaranteesBranch(block);
            }

            return false;
        }
    }
}
