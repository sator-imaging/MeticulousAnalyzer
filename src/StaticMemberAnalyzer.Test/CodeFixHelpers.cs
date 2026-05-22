// Licensed under the MIT License
using Microsoft.CodeAnalysis.CodeFixes;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    public static class CodeFixHelpers
    {
        public static FixAllProvider BatchFixAllProvider => WellKnownFixAllProviders.BatchFixer;
    }
}
