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
        public const string RuleId = "SMA8030";

        private static readonly DiagnosticDescriptor Rule = new(
            RuleId,
            new LocalizableResourceString(nameof(Resources.SMA8030_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8030_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8030_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

            foreach (var statement in block.Statements)
            {
                if (statement is LocalDeclarationStatementSyntax or EmptyStatementSyntax ||
                    (statement is ExpressionStatementSyntax exprStmt && exprStmt.Expression is AssignmentExpressionSyntax))
                {
                    continue;
                }

                if (statement is IfStatementSyntax ifStmt)
                {
                    if (isMainFlowStarted)
                    {
                        CheckAndReportMidFlowBranches(context, ifStmt);
                    }
                    else
                    {
                        if (!ContainsBranch(ifStmt))
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

        private static bool ContainsBranch(SyntaxNode node)
        {
            if (node is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax)
                return true;

            foreach (var descendant in node.DescendantNodes(static n => !(n is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)))
            {
                if (descendant is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax)
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
            foreach (var node in branchStatement.DescendantNodes(static n => !(n is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)))
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
            if (statement is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax)
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
