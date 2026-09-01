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
    public sealed class StateChangeInEarlyReturnAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "SMA8031";

        private static readonly DiagnosticDescriptor Rule = new(
            RuleId,
            new LocalizableResourceString(nameof(Resources.SMA8031_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA8031_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.CategoryPrefix + nameof(StateChangeInEarlyReturnAnalyzer),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA8031_Description), Resources.ResourceManager, typeof(Resources)));

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

            if (block.Parent is not (IfStatementSyntax or ElseClauseSyntax or SwitchSectionSyntax or CatchClauseSyntax))
                return;

            bool hasExitingStatement = false;
            foreach (var statement in block.Statements)
            {
                if (IsExitingStatement(statement))
                {
                    hasExitingStatement = true;
                    break;
                }
            }

            if (!hasExitingStatement)
                return;

            bool hasDisallowedStatement = false;
            foreach (var statement in block.Statements)
            {
                if (!IsAllowedStatement(context, statement))
                {
                    hasDisallowedStatement = true;
                    break;
                }
            }

            if (!hasDisallowedStatement)
                return;

            foreach (var statement in block.Statements)
            {
                if (IsExitingStatement(statement))
                {
                    var location = GetExitKeywordLocation(statement);
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                }
            }
        }

        private static bool IsExitingStatement(StatementSyntax statement)
        {
            return statement is ReturnStatementSyntax
                or YieldStatementSyntax
                or ContinueStatementSyntax
                or BreakStatementSyntax
                or ThrowStatementSyntax
                or GotoStatementSyntax;
        }

        private static Location GetExitKeywordLocation(StatementSyntax statement)
        {
            return statement switch
            {
                ReturnStatementSyntax s => s.ReturnKeyword.GetLocation(),
                YieldStatementSyntax s => s.YieldKeyword.GetLocation(),
                ContinueStatementSyntax s => s.ContinueKeyword.GetLocation(),
                BreakStatementSyntax s => s.BreakKeyword.GetLocation(),
                ThrowStatementSyntax s => s.ThrowKeyword.GetLocation(),
                GotoStatementSyntax s => s.GotoKeyword.GetLocation(),
                _ => statement.GetLocation(),
            };
        }

        private static bool IsAllowedStatement(SyntaxNodeAnalysisContext context, StatementSyntax statement)
        {
            if (statement is EmptyStatementSyntax)
                return true;

            if (IsExitingStatement(statement))
                return true;

            if (IsDeclarationStatement(statement))
                return true;

            if (IsOutParameterAssignment(context, statement))
                return true;

            return false;
        }

        private static bool IsDeclarationStatement(StatementSyntax statement)
        {
            if (statement is LocalDeclarationStatementSyntax or LocalFunctionStatementSyntax)
            {
                return true;
            }

            if (statement is ExpressionStatementSyntax exprStmt &&
                exprStmt.Expression is AssignmentExpressionSyntax assign)
            {
                return IsTupleDeclaration(assign);
            }

            return false;
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

        private static bool IsOutParameterAssignment(SyntaxNodeAnalysisContext context, StatementSyntax statement)
        {
            if (statement is not ExpressionStatementSyntax exprStmt)
                return false;

            if (exprStmt.Expression is not AssignmentExpressionSyntax assign)
                return false;

            if (assign.Left is TupleExpressionSyntax)
                return false;

            var targetSyntax = UnwrapParentheses(assign.Left);
            var symbol = context.SemanticModel.GetSymbolInfo(targetSyntax).Symbol;
            if (symbol is IParameterSymbol parameterSymbol && parameterSymbol.RefKind == RefKind.Out)
            {
                return true;
            }

            return false;
        }

        private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
        {
            var current = expression;
            while (current is ParenthesizedExpressionSyntax parenthesized)
            {
                current = parenthesized.Expression;
            }
            return current;
        }
    }
}
