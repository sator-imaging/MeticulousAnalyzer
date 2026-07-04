// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8002_NullSuppressionAnalyzerTests_BranchCoverage
    {
        // TODO: Branch coverage for NullSuppressionAnalyzer is capped at 75% due to a structural limitation.
        //
        // The uncovered branch is the early-return guard condition:
        //   if (context.Node is not PostfixUnaryExpressionSyntax node || !node.IsKind(SuppressNullableWarningExpression))
        //
        // Since the analyzer registers exclusively for SyntaxKind.SuppressNullableWarningExpression,
        // the Roslyn framework guarantees that context.Node is always a PostfixUnaryExpressionSyntax
        // of that specific kind. Therefore, the true-path (early return) of this guard is architecturally
        // unreachable via the public analyzer testing API, making it impossible to cover that branch.
    }
}
