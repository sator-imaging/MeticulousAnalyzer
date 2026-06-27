// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class SMA0080_InternalNamespaceAccessAnalyzerTests_Attributes
    {
        private const string AttributeSource = @"
namespace Foo
{
    internal class InternalAttribute : System.Attribute
    {
        public InternalAttribute() {}
        public InternalAttribute(int x) {}
        public InternalAttribute(System.Type t) {}
        public int Named { get; set; }
    }

    internal class InternalType
    {
        public const int Value = 1;
    }

    internal enum AttributeOption
    {
        Bar
    }

    internal class TypeArgumentAttribute : System.Attribute
    {
        public TypeArgumentAttribute(System.Type t) {}
    }
}

namespace Other
{
    internal class OtherInternalType
    {
        public const int Value = 2;
    }
}
";

        [TestMethod]
        public async Task SMA0080_Compliant_Attribute_SimpleUsage()
        {
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute]
    public class Consumer
    {
        [Foo.InternalAttribute]
        public void M() {}

        [Foo.InternalAttribute]
        public int F;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_Attribute_WithPositionalLiteral()
        {
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute(1)]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_Attribute_WithNamedArgument()
        {
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute(Named = 1)]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_Attribute_PositionalArgument_InternalMemberInSameNamespace()
        {
            // InternalType is in Foo, same as InternalAttribute.
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute(Foo.InternalType.Value)]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_Attribute_PositionalArgument_InternalEnumInSameNamespace()
        {
            // AttributeOption is in Foo, same as InternalAttribute.
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute((int)Foo.AttributeOption.Bar)]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Compliant_Attribute_PositionalArgument_InternalTypeInTypeOfInSameNamespace()
        {
            // InternalType is in Foo, same as TypeArgumentAttribute.
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.TypeArgumentAttribute(typeof(Foo.InternalType))]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Violation_Attribute_PositionalArgument_InternalMemberInDifferentNamespace()
        {
            // OtherInternalType is in Other, while InternalAttribute is in Foo.
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute({|#0:Other.OtherInternalType.Value|})]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Other"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_Attribute_NamedArgumentValue_InternalMemberInDifferentNamespace()
        {
            // OtherInternalType is in Other, while InternalAttribute is in Foo.
            var test = AttributeSource + @"
namespace Foo.Bar
{
    [Foo.InternalAttribute(Named = {|#0:Other.OtherInternalType.Value|})]
    public class Consumer
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Other"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_AttributeType_UsageAsField()
        {
            var test = AttributeSource + @"
namespace Foo.Bar
{
    public class Consumer
    {
        public {|#0:Foo.InternalAttribute|} attr;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                DiagnosticResult.CompilerError("CS0052").WithSpan(40, 38, 40, 42).WithArguments("Foo.Bar.Consumer.attr", "Foo.InternalAttribute"),
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalAttribute", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_AttributeType_UsageInTypeOf()
        {
            var test = AttributeSource + @"
namespace Foo.Bar
{
    public class Consumer
    {
        public System.Type type = {|#0:typeof(Foo.InternalAttribute)|};
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalAttribute", "Foo.Bar", "Foo"));
        }
    }
}
