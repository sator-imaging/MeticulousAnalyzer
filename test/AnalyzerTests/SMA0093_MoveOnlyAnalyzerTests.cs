// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MoveOnlyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0093_MoveOnlyAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0093_Violation_ReferenceTypes()
        {
            var test = @"
namespace Test
{
    class {|#0:MoveOnlyClass|}
    {
        public MoveOnlyClass Move() => this;
    }

    record {|#1:MoveOnlyRecord|}
    {
        public MoveOnlyRecord Move() => this;
    }
}
";

            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_InvalidTypeDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyClass");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_InvalidTypeDeclaration)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyRecord");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0093_Violation_NoCopyAttribute_ReferenceTypes()
        {
            var test = @"
using System;

namespace Test
{
    [AttributeUsage(AttributeTargets.Class)]
    class NoCopyAttribute : Attribute { }

    [NoCopyAttribute]
    class {|#0:CustomNoCopyClass|}
    {
        public CustomNoCopyClass Move() => this;
    }

    [NoCopy]
    record {|#1:CustomNoCopyRecord|}
    {
        public CustomNoCopyRecord Move() => this;
    }
}
";

            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_InvalidTypeDeclaration)
                .WithLocation(markupKey: 0)
                .WithArguments("CustomNoCopyClass");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_InvalidTypeDeclaration)
                .WithLocation(markupKey: 1)
                .WithArguments("CustomNoCopyRecord");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }
    }
}
