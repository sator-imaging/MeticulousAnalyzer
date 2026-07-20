// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCs = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class SMA0080_InternalNamespaceAccessAnalyzerTests_ExpressionKinds
    {
        [TestMethod]
        public async Task SMA0080_Violation_TargetTypedNew()
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
            Foo.InternalType x = {|#0:new()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_CastToInternalType()
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
        public void M(object o)
        {
            var x = {|#0:(Foo.InternalType)o|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_IsTypePattern()
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
        public void M(object o)
        {
            var b = {|#0:o is Foo.InternalType|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_DeclarationIsPattern()
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
        public void M(object o)
        {
            if ({|#0:o is {|#1:Foo.InternalType t|}|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_DefaultValue()
        {
            var test = @"
namespace Foo
{
    internal struct InternalStruct
    {
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:default(Foo.InternalStruct)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalStruct", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_EventAssignment()
        {
            var test = @"
using System;

namespace Foo
{
    internal class Publisher
    {
        public event EventHandler Raised;
    }
}

namespace Foo.Bar
{
    internal static class PublisherBridge
    {
        internal static {|#1:Publisher|} Instance = {|#2:null|}!;
    }

    public class Consumer
    {
        public void M()
        {
            {|#3:PublisherBridge.Instance.Raised|} += (s, e) => { };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("Publisher", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("Publisher", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(3).WithArguments("Raised", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_EventReferenceInDelegate()
        {
            var test = @"
using System;

namespace Foo
{
    internal class Publisher
    {
        public event EventHandler Raised;
    }

    internal static class PublisherSource
    {
        internal static Publisher Instance = new Publisher();
    }
}

namespace Foo.Bar
{
    internal static class PublisherBridge
    {
        internal static {|#1:Publisher|} Instance = {|#2:null|}!;
    }

    public class Consumer
    {
        public void M()
        {
            EventHandler copy = {|#0:PublisherBridge.Instance.{|#3:Raised|}|};
        }
    }
}
";
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS0070")
                .WithLocation(3)
                .WithArguments("Foo.Publisher.Raised", "Foo.Publisher");

            await VerifyCS.VerifyAnalyzerAsync(test,
                expectedCompiler,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("Publisher", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("Publisher", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Raised", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_ArrayCreation()
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
            var a = {|#0:new Foo.InternalType[1]|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_GenericFactoryInvocation()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
        public InternalType() { }
    }

    internal static class Factory
    {
        public static InternalType Create() => new InternalType();
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.Factory.Create()|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Create", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Compliant_TypeParameterObjectCreation()
        {
            var test = @"
namespace Foo.Bar
{
    internal class InternalType
    {
        public InternalType() { }
    }

    internal static class Factory<T> where T : new()
    {
        internal static T Create() => new T();
    }

    public class Consumer
    {
        public void M()
        {
            Factory<InternalType>.Create();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }


        [TestMethod]
        public async Task SMA0080_Violation_TypeOfInternalArray()
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
            var t = {|#0:typeof(Foo.InternalType[])|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_NameOfInternalType()
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
            var n = {|#0:nameof(Foo.InternalType)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_IndexerOnInternalType()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
        public int this[int index] => index;
    }
}

namespace Foo.Bar
{
    internal static class LocalHolder
    {
        internal static {|#1:Foo.InternalType|} Instance = {|#2:null|}!;
    }

    public class Consumer
    {
        public void M()
        {
            var x = {|#0:LocalHolder.Instance[0]|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("this", "Foo.Bar", "Foo"));
        }


        [TestMethod]
        public async Task SMA0080_Compliant_NameOfLocalWithInternalType()
        {
            var test = @"
namespace Foo.Bar
{
    internal class InternalType
    {
    }

    internal static class Helper
    {
        internal static void M(InternalType local)
        {
            var n = nameof(local);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0080_Violation_MethodParameterDeclarationType()
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
    internal static class Helper
    {
        internal static void M({|#0:Foo.InternalType|} local) { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_RecursivePattern()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
        public int Value { get; }
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M(object o)
        {
            if ({|#0:o is {|#1:Foo.InternalType { {|#2:Value|}: 0 }|}|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("InternalType", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("Value", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_BinaryPattern()
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
        public void M(object o)
        {
            if ({|#0:o is {|#1:Foo.InternalType|} or null|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }


        [TestMethod]
        public async Task SMA0080_Violation_NestedRecursivePatternPropertySubpattern()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
    }

    public class PublicType
    {
        internal InternalType Property { get; }
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M(object o)
        {
            if ({|#0:o is Foo.PublicType { {|#1:Property|}: {|#2:Foo.InternalType _|} }|})
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("Property", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_GenericTypeArgument()
        {
            var test = @"
using System.Collections.Generic;

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
            var x = {|#0:default(List<Foo.InternalType>)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }


        [TestMethod]
        public async Task SMA0080_Violation_SwitchDeclarationPattern()
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
        public void M(object o)
        {
            switch (o)
            {
                case {|#0:Foo.InternalType t|}:
                    break;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_GenericTypeFieldOnGenericClass()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
    }

    public class GenericClass<T>
    {
        public static int Field;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var x = {|#0:Foo.GenericClass<Foo.InternalType>.Field|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalType", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_SizeOfInternalType()
        {
            var test = @"
namespace Foo
{
    internal struct InternalStruct
    {
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public unsafe void M()
        {
            var s = {|#0:sizeof(Foo.InternalStruct)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("InternalStruct", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_MethodGroupReference()
        {
            var test = @"
using System;

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
            Action a = {|#0:Foo.InternalHelper.Run|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Run", "Foo.Bar", "Foo"));
        }

        [TestMethod]
        public async Task SMA0080_Violation_NameOfInternalTypeMember()
        {
            var test = @"
namespace Foo
{
    internal class InternalType
    {
        public static void Method() { }
        public static int Field;
        public static int Property { get; set; }
        public static event System.EventHandler Event;
    }
}

namespace Foo.Bar
{
    public class Consumer
    {
        public void M()
        {
            var n1 = {|#0:nameof(Foo.InternalType.Method)|};
            var n2 = {|#1:nameof(Foo.InternalType.Field)|};
            var n3 = {|#2:nameof(Foo.InternalType.Property)|};
            var n4 = {|#3:nameof(Foo.InternalType.Event)|};
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic().WithLocation(0).WithArguments("Method", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(1).WithArguments("Field", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(2).WithArguments("Property", "Foo.Bar", "Foo"),
                VerifyCS.Diagnostic().WithLocation(3).WithArguments("Event", "Foo.Bar", "Foo"));
        }
    }
}
