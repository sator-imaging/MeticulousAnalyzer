// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class ConfigTest_InternalNamespaceAccessAnalyzer
    {
        private static async Task VerifyWithConfigAsync(string source, string namespaces, string types, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            var config = "is_global = true\n";
            if (!string.IsNullOrEmpty(namespaces))
                config += $"{Core.Config_VisibleInternalNamespaces} = {namespaces}\n";
            if (!string.IsNullOrEmpty(types))
                config += $"{Core.Config_VisibleInternalTypes} = {types}\n";

            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static Task VerifyWithConfigAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            return VerifyWithConfigAsync(source, namespaces: "", types: "", expected: expected);
        }

        private static Task VerifyWithConfigAsync(string source, string namespaces, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            return VerifyWithConfigAsync(source, namespaces: namespaces, types: "", expected: expected);
        }

        private static Task VerifyWithConfigAsync(string source, string types)
        {
            return VerifyWithConfigAsync(source, namespaces: "", types: types);
        }

        [TestMethod]
        public async Task SMA0080_Config_VisibleInternalNamespaces_MultipleValues_CaseSensitive()
        {
            var test = @"
namespace Foo.Common
{
    internal class InternalType { public static int Value; }
}
namespace Foo.Internal
{
    internal class InternalType { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.Common.InternalType.Value;
            var y = Foo.Internal.InternalType.Value;
        }
    }
}
";
            await VerifyWithConfigAsync(test, namespaces: "Common, Internal");
        }

        [TestMethod]
        public async Task SMA0080_Config_VisibleInternalTypes_MultipleValues_CaseSensitive()
        {
            var test = @"
namespace Foo
{
    internal class Shared { public static int Value; }
    internal class Helpers { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.Shared.Value;
            var y = Foo.Helpers.Value;
        }
    }
}
";
            await VerifyWithConfigAsync(test, types: "Shared, Helpers");
        }

        [TestMethod]
        public async Task SMA0080_Config_SRType_Hardcoded_NoConfigRequired()
        {
            var test = @"
namespace Foo
{
    internal class SR { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.SR.Value;
        }
    }
}
";
            await VerifyWithConfigAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Config_NoConfigPresent_Violation_Common()
        {
            var test = @"
namespace Foo.Common
{
    internal class InternalType { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Common.InternalType.Value|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic().WithLocation(markupKey: 0).WithArguments("Value", "Foo.Bar", "Foo.Common");
            await VerifyWithConfigAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0080_Config_NestedSRType_Allowed()
        {
            var test = @"
namespace Foo
{
    internal class Outer
    {
        internal class SR { public static int Value; }
    }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.Outer.SR.Value;
        }
    }
}
";
            await VerifyWithConfigAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Config_SRType_Itself_StillReported_Violation()
        {
            var test = @"
namespace Foo
{
    internal class SR { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:typeof(Foo.SR)|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic().WithLocation(markupKey: 0).WithArguments("SR", "Foo.Bar", "Foo");
            await VerifyWithConfigAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0080_Config_VisibleInternalNamespaces_CacheHit()
        {
            var test = @"
namespace Foo.Common
{
    internal class InternalType { public static int Value; }
}
namespace Foo.Internal
{
    internal class InternalType { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.Common.InternalType.Value;
            var y = Foo.Internal.InternalType.Value;
        }
    }
}
";
            // Uses the same "Common, Internal" namespaces to hit the TryGetValue configuration cache
            await VerifyWithConfigAsync(test, namespaces: "Common, Internal");
        }

        [TestMethod]
        public async Task SMA0080_Config_VisibleInternalNamespaces_EmptyAndWhitespaceConfiguration()
        {
            var test = @"
namespace Foo.Common
{
    internal class InternalType { public static int Value; }
}
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Common.InternalType.Value|};
        }
    }
}
";
            // Empty commas/whitespace value evaluates to null configuration
            var expected = VerifyCS.Diagnostic().WithLocation(markupKey: 0).WithArguments("Value", "Foo.Bar", "Foo.Common");
            await VerifyWithConfigAsync(test, namespaces: " , ", expected);
        }
    }
}
