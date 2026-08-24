// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.LiteralBranchAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8023_LiteralBranchAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8023_Violation_SwitchCase_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            switch (some)
            {
                case {|#0:'a'|}:
                    break;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'a'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_BinaryEquals_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some == {|#0:'a'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'a'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_ConstantPattern_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is {|#0:'a'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'a'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_BinaryAnd_CharLiterals()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some >= {|#0:'a'|} && some <= {|#1:'z'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'a'"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 1).WithArguments(arguments: "'z'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_RelationalPattern_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is >= {|#0:'a'|} and <= {|#1:'z'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'a'"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 1).WithArguments(arguments: "'z'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_BinaryGreaterThan_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some > {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_BinaryEquals_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some == {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_ConstantPattern_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Violation_RelationalPattern_NullCharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some is > {|#0:'\0'|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Compliant_ConstantChar()
        {
            var test = @"
namespace Test
{
    public class C
    {
        private const char NullChar = '\0';

        public void M(char some)
        {
            if (some == NullChar)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8023_Violation_EmptyWhyPrefix()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some == {|#0:'\0'|} /* Why: */)
            {
            }

            if (some == {|#1:'a'|} /*Why: missing leading space is not allowed */)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 0).WithArguments(arguments: "'\\0'"),
                VerifyCS.Diagnostic(diagnosticId: LiteralBranchAnalyzer.RuleId_LiteralBranchChar).WithLocation(markupKey: 1).WithArguments(arguments: "'a'")
            );
        }

        [TestMethod]
        public async Task SMA8023_Compliant_TrailingTriviaComment_CharLiteral()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M(char some)
        {
            if (some == '\0' /* Why: suppression */)
            {
            }

            if (some == 'a' /* why: lower case suppression */)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
