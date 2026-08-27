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
    public sealed class MidFlowReturnAnalyzer : DiagnosticAnalyzer
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

            AnalyzeBlockStatements(context, block);
        }

        private static void AnalyzeBlockStatements(SyntaxNodeAnalysisContext context, BlockSyntax block)
        {
            bool isMainFlowStarted = block.Parent is not (MethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or AnonymousMethodExpressionSyntax
                or SimpleLambdaExpressionSyntax
                or ParenthesizedLambdaExpressionSyntax
                or AccessorDeclarationSyntax
                or ConstructorDeclarationSyntax
                or DestructorDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax);

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
                        CheckAndReportMidFlowReturns(context, ifStmt);
                    }
                    else
                    {
                        if (!ContainsReturn(ifStmt))
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

        private static bool ContainsReturn(SyntaxNode node)
        {
            if (node is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax)
                return true;

            foreach (var descendant in node.DescendantNodes(static n => !(n is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)))
            {
                if (descendant is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckAndReportMidFlowReturns(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStmt)
        {
            if (AllBranchesReturn(ifStmt))
            {
                return;
            }

            CollectAndReportReturnsInIfBranch(context, ifStmt);
        }

        private static void CollectAndReportReturnsInIfBranch(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStmt)
        {
            IfStatementSyntax? currentIf = ifStmt;
            while (currentIf != null)
            {
                ReportReturnsInStatement(context, currentIf.Statement);

                if (currentIf.Else != null)
                {
                    if (currentIf.Else.Statement is IfStatementSyntax elseIf)
                    {
                        currentIf = elseIf;
                    }
                    else
                    {
                        ReportReturnsInStatement(context, currentIf.Else.Statement);
                        currentIf = null;
                    }
                }
                else
                {
                    currentIf = null;
                }
            }
        }

        private static void ReportReturnsInStatement(SyntaxNodeAnalysisContext context, StatementSyntax branchStatement)
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
        }

        private static bool AllBranchesReturn(IfStatementSyntax ifStmt)
        {
            IfStatementSyntax? current = ifStmt;
            while (current != null)
            {
                if (!BranchGuaranteesReturn(current.Statement))
                    return false;

                if (current.Else == null)
                    return false;

                if (current.Else.Statement is IfStatementSyntax elseIf)
                {
                    current = elseIf;
                }
                else
                {
                    return BranchGuaranteesReturn(current.Else.Statement);
                }
            }

            return false;
        }

        private static bool BranchGuaranteesReturn(StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
            {
                foreach (var stmt in block.Statements)
                {
                    if (StatementGuaranteesReturn(stmt))
                        return true;
                }
                return false;
            }

            return StatementGuaranteesReturn(statement);
        }

        private static bool StatementGuaranteesReturn(StatementSyntax statement)
        {
            if (statement is ReturnStatementSyntax or YieldStatementSyntax or ThrowStatementSyntax)
                return true;

            if (statement is IfStatementSyntax innerIf)
            {
                return AllBranchesReturn(innerIf);
            }

            if (statement is BlockSyntax block)
            {
                return BranchGuaranteesReturn(block);
            }

            return false;
        }
    }
}
