// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<AnonymousObjectCreationAnalyzer>;

    [TestClass]
    public class SMA7030_AnonymousObjectCreationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7030_Compliant_Tuple()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        var x = (some: ""Foo"", other: 42);
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7030_Violation_AnonymousObject()
        {
            var test = @"
using System;
public class C
{
    void M()
    {
        var x = {|#0:new { some = ""Foo"", other = 42 }|};
    }
}
";
            var expected = VerifyCS.Diagnostic(AnonymousObjectCreationAnalyzer.RuleId_AnonymousObject).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7030_Violation_AnonymousObjectAsMethodArgument()
        {
            var test = @"
using System;
public class C
{
    void Foo(object o) { }
    void M()
    {
        Foo({|#0:new { some = ""Foo"", other = 42 }|});
    }
}
";
            var expected = VerifyCS.Diagnostic(AnonymousObjectCreationAnalyzer.RuleId_AnonymousObject).WithLocation(markupKey: 0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
