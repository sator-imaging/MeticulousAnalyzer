// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ResourceTest
    {
        [TestMethod]
        public void CoverageBoost_ResourcesAndBurstLinq()
        {
            // --- Resource classes via reflection ---
            var analysisAsm = typeof(Analysis.Analyzers.FlakyInitializationAnalyzer).Assembly;
            var codefixAsm = typeof(CodeFixes.Providers.EnumObfuscationCodeFixProvider).Assembly;

            var resourceTypes = new[]
            {
                analysisAsm.GetType("SatorImaging.StaticMemberAnalyzer.Analysis.Resources"),
                codefixAsm.GetType("SatorImaging.StaticMemberAnalyzer.CodeFixes.CodeFixResources"),
            };

            foreach (var rt in resourceTypes)
            {
                Assert.IsNotNull(rt);
                var rmProp = rt.GetProperty("ResourceManager", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(rmProp);
                Assert.IsNotNull(rmProp.GetValue(null));

                var props = rt.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.PropertyType == typeof(string));
                foreach (var p in props)
                    Assert.IsNotNull(p.GetValue(null), $"{rt.Name}.{p.Name}");
            }

            // --- BurstLinq.Select and Any with size variants: 0, 10, 100 ---
            foreach (var size in new[] { 0, 10, 100 })
            {
                var data = ImmutableArray.CreateRange(Enumerable.Range(0, size));

                // Select
                var selected = data.Select(x => x * 2);
                Assert.AreEqual(size, selected.Length);
                if (size > 0)
                    Assert.AreEqual(0, selected[0]);

                // Any (ImmutableArray, parameterless)
                Assert.AreEqual(size > 0, data.Any());

                // Any (IEnumerable via IReadOnlyCollection path)
                var list = new List<int>(Enumerable.Range(0, size));
                Assert.AreEqual(size > 0, ((IEnumerable<int>)list).Any());

                // Any (IEnumerable via pure enumerator path)
                Assert.AreEqual(size > 0, PureEnumerable(size).Any());
            }

            // default ImmutableArray edge cases
            var def = default(ImmutableArray<int>);
            Assert.AreEqual(0, def.Select(x => x).Length);
            Assert.IsFalse(def.Any());
        }

        private static IEnumerable<int> PureEnumerable(int count)
        {
            for (int i = 0; i < count; i++)
                yield return i;
        }
    }
}
