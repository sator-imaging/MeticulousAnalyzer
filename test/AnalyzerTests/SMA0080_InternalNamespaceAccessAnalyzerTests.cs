// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class SMA0080_InternalNamespaceAccessAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0080_Violation_AccessParentNamespaceInternalMember()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.InternalType.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_AccessSiblingNamespaceInternalMember()
        {
            var test = @"
namespace Foo.Other
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Other.InternalType.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo.Other"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_ObjectCreationOfInternalType()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:new Foo.InternalType()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_InvokeInternalMethod()
        {
            var test = @"
namespace Foo
{
    internal static class InternalHelper
    {
        internal static void Run() { }
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            {|#0:Foo.InternalHelper.Run()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Run", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_TypeOfInternalType()
        {
            var test = @"
using System;

namespace Foo
{
    internal class InternalType
    {
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var t = {|#0:typeof(Foo.InternalType)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Compliant_AccessWithinSameNamespace()
        {
            var test = @"
namespace Foo.Bar
{
    internal class InternalType
    {
        public static int Value;
    }

    public class Consumer
    {
        public void M()
        {
            var x = InternalType.Value;
            var y = new InternalType();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_AccessPublicTypeFromOtherNamespace()
        {
            var test = @"
namespace Foo
{
    public class PublicType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.PublicType.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_ProtectedInternalWithinSameNamespace()
        {
            var test = @"
namespace Foo.Bar
{
    internal class InternalType
    {
        protected internal static int Value;
    }

    public class Consumer
    {
        public void M()
        {
            var x = InternalType.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Violation_DeeplyNestedNamespaceShowsFullPathInMessage()
        {
            var test = @"
namespace Foo.Declared
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar.Baz
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Declared.InternalType.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar.Baz", "Foo.Declared"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_PublicMemberOnNestedInternalType()
        {
            var test = @"
namespace Foo
{
    internal class OuterInternal
    {
        public class NestedPublic
        {
            public static int Value;
        }
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.OuterInternal.NestedPublic.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Compliant_AccessCoreNamespaceInternalMember()
        {
            var test = @"
namespace Foo.Core
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.Core.InternalType.Value;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Violation_AccessCommonNamespaceInternalMember_NoConfig()
        {
            var test = @"
namespace Foo.Common
{
    internal class InternalType
    {
        public static int Value;
    }
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
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo.Common"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_AccessCoreSubNamespaceInternalMember()
        {
            var test = @"
namespace Foo.Core.Sub
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Core.Sub.InternalType.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo.Core.Sub"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_CoreNamespaceAccessOtherInternalMember()
        {
            var test = @"
namespace Foo.Bar
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Core
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Bar.InternalType.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Core", "Foo.Bar"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_AccessInternalNamespaceInternalMember_NoConfig()
        {
            var test = @"
namespace Foo.Internal
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Internal.InternalType.Value|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo.Internal"));
        }

        [TestMethod]
        public async Task SMA0080_Compliant_GeneratedCodeExemption()
        {
            var testSource = @"
namespace Foo
{
    internal class InternalType
    {
        public static int Value;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.InternalType.Value;
        }
    }
}
";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { ("Test.g.cs", testSource) }
                }
            };

            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA0080_Violation_AccessInternalDefinedInGeneratedCode()
        {
            var genSource = @"
namespace Foo
{
    internal class InternalType
    {
        public static int Value;
    }
}
";
            var consumerSource = @"
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.InternalType.Value|};
        }
    }
}
";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Test.g.cs", genSource),
                        ("Consumer.cs", consumerSource),
                    }
                }
            };

            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo"));
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA0080_Compliant_GeneratedCodeExemption_InternalDefinedInNormalCode()
        {
            var normalSource = @"
namespace Foo
{
    internal class InternalType
    {
        public static int Value;
    }
}
";
            var genConsumerSource = @"
namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = Foo.InternalType.Value;
        }
    }
}
";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        ("Normal.cs", normalSource),
                        ("Consumer.g.cs", genConsumerSource),
                    }
                }
            };

            await test.RunAsync();
        }

    }
}
