// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    /// <summary>
    /// Branch coverage for NullSuppressionAnalyzer is capped at 75% due to a structural limitation.
    ///
    /// The uncovered branch is the early-return guard condition:
    /// <code>
    /// if (context.Node is not PostfixUnaryExpressionSyntax node || !node.IsKind(SuppressNullableWarningExpression))
    /// </code>
    ///
    /// Since the analyzer registers exclusively for <c>SyntaxKind.SuppressNullableWarningExpression</c>,
    /// the Roslyn framework guarantees that <c>context.Node</c> is always a <c>PostfixUnaryExpressionSyntax</c>
    /// of that specific kind. Therefore, the true-path (early return) of this guard is architecturally
    /// unreachable via the public analyzer testing API, making it impossible to cover that branch.
    /// </summary>
    [TestClass]
    public class SMA8002_NullSuppressionAnalyzerTests_BranchCoverage
    {
    }
}
