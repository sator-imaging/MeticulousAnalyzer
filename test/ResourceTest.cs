// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.CodeFixes;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ResourceTest
    {
        [TestMethod]
        public void ResourceProperties_ReturnNonEmpty()
        {
            // Analysis Resources - ResourceManager
            Assert.IsNotNull(Resources.ResourceManager);

            // Analysis Resources - representative string properties
            Assert.IsFalse(string.IsNullOrEmpty(Resources.SMA0001_Title));
            Assert.IsFalse(string.IsNullOrEmpty(Resources.SMA0001_Description));
            Assert.IsFalse(string.IsNullOrEmpty(Resources.SMA0010_Title));
            Assert.IsFalse(string.IsNullOrEmpty(Resources.SMA0020_MessageFormat));
            Assert.IsFalse(string.IsNullOrEmpty(Resources.SMA0030_Title));
            Assert.IsFalse(string.IsNullOrEmpty(Resources.SMA0040_Description));

            // CodeFix Resources - ResourceManager and all string properties
            Assert.IsNotNull(CodeFixResources.ResourceManager);
            Assert.IsFalse(string.IsNullOrEmpty(CodeFixResources.CodeFix_EnumObfuscation));
            Assert.IsFalse(string.IsNullOrEmpty(CodeFixResources.CodeFix_NamedArgument));
            Assert.IsFalse(string.IsNullOrEmpty(CodeFixResources.CodeFix_NullSuppression));

            // BurstLinq coverage - Select on ImmutableArray
            foreach (var size in new[] { 0, 10, 100 })
            {
                var arr = size == 0 ? default : ImmutableArray.CreateRange(new int[size]);
                var selected = BurstLinq.Select(arr, x => x + 1);
                Assert.AreEqual(size == 0 ? 0 : size, selected.Length);
            }

            // BurstLinq coverage - Any() parameterless on ImmutableArray
            foreach (var size in new[] { 0, 10, 100 })
            {
                var arr = size == 0 ? default : ImmutableArray.CreateRange(new int[size]);
                var result = BurstLinq.Any(arr);
                Assert.AreEqual(size > 0, result);
            }

            // BurstLinq coverage - Any() parameterless on IEnumerable (IReadOnlyCollection path)
            foreach (var size in new[] { 0, 10, 100 })
            {
                var list = new List<int>(new int[size]);
                var result = BurstLinq.Any((IEnumerable<int>)list);
                Assert.AreEqual(size > 0, result);
            }

            // BurstLinq coverage - Any() parameterless on IEnumerable (pure enumerator path)
            foreach (var size in new[] { 0, 10, 100 })
            {
                var result = BurstLinq.Any(Generate(size));
                Assert.AreEqual(size > 0, result);
            }
        }

        static IEnumerable<int> Generate(int count)
        {
            for (int i = 0; i < count; i++)
                yield return i;
        }
    }
}
