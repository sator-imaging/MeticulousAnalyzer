// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.RequiredNamedArgumentAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8005_RequiredNamedArgumentAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8005_Violation_CapacitySuffixParameterAtAnyPosition()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void First(int bufferCapacity, string other) {}
        public void Middle(string first, int maxCAPACITY, string last) {}
        public void Last(string other, int capacity) {}

        public void Test()
        {
            First({|#0:1|}, ""other"");
            Middle(""first"", {|#1:2|}, ""last"");
            Last(""other"", {|#2:3|});
        }
    }
}
";
            var expected = new[]
            {
                VerifyCS.Diagnostic(RequiredNamedArgumentAnalyzer.RuleId_RequiredNamedArgument).WithLocation(0).WithArguments("bufferCapacity"),
                VerifyCS.Diagnostic(RequiredNamedArgumentAnalyzer.RuleId_RequiredNamedArgument).WithLocation(1).WithArguments("maxCAPACITY"),
                VerifyCS.Diagnostic(RequiredNamedArgumentAnalyzer.RuleId_RequiredNamedArgument).WithLocation(2).WithArguments("capacity"),
            };
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA8005_Compliant_NamedOrOtherParameter()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void Some(int bufferCapacity, int count) {}
        public void Some(string capacity) {}
        public void Some(int size) {}

        public void Test()
        {
            Some(bufferCapacity: 1, count: 2);
            Some(""value"");
            Some(3);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8005_Violation_AttributeConstructorCapacityArgument()
        {
            var test = @"
using System;

[AttributeUsage(AttributeTargets.All)]
public sealed class TestAttribute : Attribute
{
    public TestAttribute(string name, int capacity) {}
}

[Test(""name"", {|#0:1|})]
public class CTest {}
";
            var expected = VerifyCS.Diagnostic(RequiredNamedArgumentAnalyzer.RuleId_RequiredNamedArgument)
                .WithLocation(0)
                .WithArguments("capacity");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
