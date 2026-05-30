// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ResourceTest
    {
        [TestMethod]
        public void AllResourceStringProperties_ReturnNonEmpty()
        {
            var analysisAssembly = typeof(SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.FlakyInitializationAnalyzer).Assembly;
            var codefixAssembly = typeof(SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.EnumObfuscationCodeFixProvider).Assembly;

            var resourceClasses = new[]
            {
                analysisAssembly.GetType("SatorImaging.StaticMemberAnalyzer.Analysis.Resources"),
                codefixAssembly.GetType("SatorImaging.StaticMemberAnalyzer.CodeFixes.CodeFixResources"),
            };

            foreach (var resourceType in resourceClasses)
            {
                Assert.IsNotNull(resourceType, "Resource type not found");

                // Verify ResourceManager is not null
                var rmProp = resourceType.GetProperty("ResourceManager", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(rmProp, $"ResourceManager property not found on {resourceType.FullName}");
                var rmValue = rmProp.GetValue(null);
                Assert.IsNotNull(rmValue, $"ResourceManager is null on {resourceType.FullName}");

                // Get all static string properties (excluding Culture)
                var stringProps = resourceType
                    .GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.PropertyType == typeof(string) && p.Name != "Culture")
                    .ToArray();

                Assert.IsTrue(stringProps.Length > 0, $"No string properties found on {resourceType.FullName}");

                foreach (var prop in stringProps)
                {
                    var value = (string)prop.GetValue(null);
                    Assert.IsFalse(string.IsNullOrEmpty(value), $"{resourceType.FullName}.{prop.Name} returned null or empty");
                }
            }
        }
    }
}
