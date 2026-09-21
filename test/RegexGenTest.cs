// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis;

namespace SatorImaging.MeticulousAnalyzer.Tests
{
    [TestClass]
    public class RegexGenTest
    {
        private static readonly string[] TargetStrings = new[]
        {
            "Length",
            "Count",
            "Index",
            "Remove",
            "Search",
            "Add",
            "Exchange",
            "Decrement",
            "Increment",
            "Width",
            "Height",
            "Depth",
            "Size",
            "Capacity"
        };

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_TargetTextOnly()
        {
            var regex = GeneratedRegexPolyfill.IsExcemptionNameForZeroComparison();
            Assert.IsNotNull(regex);

            foreach (var target in TargetStrings)
            {
                Assert.IsTrue(regex.IsMatch(target), $"Expected match for '{target}'");
                Assert.IsTrue(regex.IsMatch(target.ToLowerInvariant()), $"Expected match for '{target.ToLowerInvariant()}'");
                Assert.IsTrue(regex.IsMatch(target.ToUpperInvariant()), $"Expected match for '{target.ToUpperInvariant()}'");
            }
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_TargetTextFencedRandomChars()
        {
            var regex = GeneratedRegexPolyfill.IsExcemptionNameForZeroComparison();

            foreach (var target in TargetStrings)
            {
                string input = $"abc_{target}_xyz123";
                Assert.IsTrue(regex.IsMatch(input), $"Expected match for '{input}'");

                string lowerInput = $"---{target.ToLowerInvariant()}+++";
                Assert.IsTrue(regex.IsMatch(lowerInput), $"Expected match for '{lowerInput}'");

                string upperInput = $"***{target.ToUpperInvariant()}###";
                Assert.IsTrue(regex.IsMatch(upperInput), $"Expected match for '{upperInput}'");
            }
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_TargetTextAppearsMultipleTimesWithRandomChars()
        {
            var regex = GeneratedRegexPolyfill.IsExcemptionNameForZeroComparison();

            for (int i = 0; i < TargetStrings.Length - 1; i++)
            {
                string first = TargetStrings[i];
                string second = TargetStrings[i + 1];
                string input = $"prefix_{first}_middle_{second}_suffix";

                Assert.IsTrue(regex.IsMatch(input), $"Expected match for '{input}'");
                var matches = regex.Matches(input);
                Assert.AreEqual(2, matches.Count, $"Expected exactly 2 matches for '{input}'");
            }

            string allJoined = string.Join("_random_", TargetStrings);
            var allMatches = regex.Matches(allJoined);
            Assert.AreEqual(TargetStrings.Length, allMatches.Count, $"Expected {TargetStrings.Length} matches for joined target string");
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_RandomCharsOnly()
        {
            var regex = GeneratedRegexPolyfill.IsExcemptionNameForZeroComparison();
            string[] nonMatchingInputs = new[]
            {
                "qwertyuiop",
                "1234567890",
                "FooBar",
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
            var regex = GeneratedRegexPolyfill.IsExcemptionNameForZeroComparison();
            Assert.IsFalse(regex.IsMatch(""), "Expected no match for empty string");
        }

        [TestMethod]
        public void IsExcemptionNameForZeroComparison_LongRandomCharsWithoutTargetText()
        {
            var regex = GeneratedRegexPolyfill.IsExcemptionNameForZeroComparison();

            var rand = new System.Random(42);
            char[] chars = new char[1024];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)rand.Next('a', 'z' + 1);
            }
            string longInput = new string(chars);

            // Verify regex doesn't throw on random chars
            regex.IsMatch(longInput);
        }
    }
}
