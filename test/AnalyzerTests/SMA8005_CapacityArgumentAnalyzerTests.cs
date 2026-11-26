// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.CapacityArgumentAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8005_CapacityArgumentAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8005_Violation_CapacityParameterAtAnyPosition()
        {
            var test = @"
namespace Test
{
    public class CTest
    {
        public void First(int capacity, string other) {}
        public void Middle(string first, int capacity, string last) {}
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
                VerifyCS.Diagnostic(CapacityArgumentAnalyzer.RuleId_CapacityArgument).WithLocation(0).WithArguments("capacity"),
                VerifyCS.Diagnostic(CapacityArgumentAnalyzer.RuleId_CapacityArgument).WithLocation(1).WithArguments("capacity"),
                VerifyCS.Diagnostic(CapacityArgumentAnalyzer.RuleId_CapacityArgument).WithLocation(2).WithArguments("capacity"),
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
        public void Some(int capacity, int count) {}
        public void Some(string capacity) {}
        public void Some(int size) {}

        public void Test()
        {
            Some(capacity: 1, count: 2);
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
            var expected = VerifyCS.Diagnostic(CapacityArgumentAnalyzer.RuleId_CapacityArgument)
                .WithLocation(0)
                .WithArguments("capacity");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
