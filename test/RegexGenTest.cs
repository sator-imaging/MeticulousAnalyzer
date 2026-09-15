// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis;

namespace SatorImaging.MeticulousAnalyzer.Tests
{
    [TestClass]
    public class RegexGenTest
    {
        [TestMethod]
        public void IsExcemptionNameForZeroComparison_TargetTextOnly()
        {
            var regex = RegexGen.IsExcemptionNameForZeroComparison();
            Assert.IsNotNull(regex);

            string[] targets = new[] { "Length", "Count", "Index", "Remove", "Search", "Add", "Exchange", "Decrement", "Increment" };
            foreach (var target in targets)
            {
                Assert.IsTrue(regex.IsMatch(target), $"Expected match for '{target}'");
                Assert.IsTrue(regex.IsMatch(target.ToLowerInvariant()), $"Expected match for '{target.ToLowerInvariant()}'");
                Assert.IsTrue(regex.IsMatch(target.ToUpperInvariant()), $"Expected match for '{target.ToUpperInvariant()}'");
            }
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_TargetTextFencedRandomChars()
        {
            var regex = RegexGen.IsExcemptionNameForZeroComparison();
            string[] fencedInputs = new[]
            {
                "abcLengthxyz",
                "123Count456",
                "_Index_",
                "fooRemoveBar",
                "prefixSearchSuffix",
                "xAddy",
                "!!!Exchange???",
                "---Decrement+++",
                "   Increment   "
            };

            foreach (var input in fencedInputs)
            {
                Assert.IsTrue(regex.IsMatch(input), $"Expected match for '{input}'");
            }
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_TargetTextAppearsMultipleTimesWithRandomChars()
        {
            var regex = RegexGen.IsExcemptionNameForZeroComparison();
            string[] multiInputs = new[]
            {
                "Length_and_Count_and_Index",
                "abcRemove123Search456",
                "Add_Exchange_Decrement_Increment",
                "length_Count_INDEX"
            };

            foreach (var input in multiInputs)
            {
                Assert.IsTrue(regex.IsMatch(input), $"Expected match for '{input}'");
                var matches = regex.Matches(input);
                Assert.IsTrue(matches.Count > 1, $"Expected multiple matches for '{input}'");
            }
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_RandomCharsOnly()
        {
            var regex = RegexGen.IsExcemptionNameForZeroComparison();
            string[] nonMatchingInputs = new[]
            {
                "qwertyuiop",
                "1234567890",
                "FooBar",
                "Size",
                "Capacity",
                "CustomMember"
            };

            foreach (var input in nonMatchingInputs)
            {
                Assert.IsFalse(regex.IsMatch(input), $"Expected no match for '{input}'");
            }
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_EmptyString()
        {
            var regex = RegexGen.IsExcemptionNameForZeroComparison();
            Assert.IsFalse(regex.IsMatch(""), "Expected no match for empty string");
        }
    }
}
