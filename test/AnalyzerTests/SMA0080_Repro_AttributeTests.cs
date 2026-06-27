// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class SMA0080_Repro_AttributeTests
    {
        [TestMethod]
        public async Task SMA0080_Compliant_AttributeUsage()
        {
            var test = @"
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
}

namespace Foo.Bar
{
    // Allowed: attribute type usage as attribute
    [Foo.InternalAttribute]
    public class Consumer
    {
    }

    [Foo.InternalAttribute(1)]
    public class ConsumerWithArgs
    {
    }

    // Allowed: named arguments of the attribute itself
    [Foo.InternalAttribute(Named = 1)]
    public class ConsumerWithNamedArg
    {
    }

    // Violation: internal member used as argument value
    [Foo.InternalAttribute({|#0:Foo.InternalType.Value|})]
    public class ConsumerWithInternalArg
    {
    }

    // Violation: internal type used in typeof() as argument value
    [Foo.InternalAttribute({|#1:typeof(Foo.InternalType)|})]
    public class ConsumerWithInternalTypeArg
    {
    }

    // Violation: internal member used as named argument value
    [Foo.InternalAttribute(Named = {|#2:Foo.InternalType.Value|})]
    public class ConsumerWithNamedInternalArg
    {
    }

    // Check attribute on other members
    public class MemberConsumer
    {
        [Foo.InternalAttribute]
        public void M() {}

        [Foo.InternalAttribute]
        public int F;
    }

    // Violation: attribute type used as field or in typeof() outside attribute syntax
    internal class TypeConsumer
    {
        public {|#3:Foo.InternalAttribute|} attr;
        public System.Type type = {|#4:typeof(Foo.InternalAttribute)|};
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Value", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("Value", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(3).WithArguments("InternalAttribute", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(4).WithArguments("InternalAttribute", "Foo.Bar", "Foo")
            );
        }
    }
}
