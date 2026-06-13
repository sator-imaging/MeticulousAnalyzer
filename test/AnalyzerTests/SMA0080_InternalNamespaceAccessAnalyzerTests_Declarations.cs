// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class SMA0080_InternalNamespaceAccessAnalyzerTests_Declarations
    {
        [TestMethod]
        public async Task SMA0080_Violation_FieldDeclarationType()
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
    internal class Consumer
    {
        internal {|#0:Foo.InternalType|} Field;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_PropertyDeclarationType()
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
    internal class Consumer
    {
        internal {|#0:Foo.InternalType|} Property { get; set; }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithSpan(13, 18, 13, 34).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithSpan(13, 46, 13, 49).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithSpan(13, 51, 13, 54).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_MethodReturnType()
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
    internal class Consumer
    {
        internal {|#0:Foo.InternalType|} M() => default;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithSpan(13, 42, 13, 49).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_MethodParameterType()
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
    internal class Consumer
    {
        internal void M({|#0:Foo.InternalType|} value) { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_BaseClassDeclaration()
        {
            var test = @"
namespace Foo
{
    internal class InternalBase
    {
    }
}

namespace Foo.Bar
{
    internal class Consumer : {|#0:Foo.InternalBase|}
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalBase", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_InterfaceImplementation()
        {
            var test = @"
namespace Foo
{
    internal interface IInternal
    {
    }
}

namespace Foo.Bar
{
    internal class Consumer : {|#0:Foo.IInternal|}
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("IInternal", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Compliant_FieldDeclarationInSameNamespace()
        {
            var test = @"
namespace Foo.Bar
{
    internal class InternalType
    {
    }

    public class Consumer
    {
        internal InternalType Field;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Violation_EventDeclarationType()
        {
            var test = @"
namespace Foo
{
    internal delegate void InternalHandler();
}

namespace Foo.Bar
{
    internal class Consumer
    {
        internal event {|#0:Foo.InternalHandler|} Raised;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalHandler", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_ClassTypeParameterConstraint()
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
    internal class Consumer<T> where T : {|#0:Foo.InternalType|}
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_MethodTypeParameterConstraint()
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
    internal class Consumer
    {
        internal void M<T>() where T : {|#0:Foo.InternalType|} { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }
    }
}
