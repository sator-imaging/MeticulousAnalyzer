// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<FlakyInitializationAnalyzer>;

    [TestClass]
    public class SMA0003_FlakyInitializationAnalyzerTests_CrossFile
    {
        [TestMethod]
        public async Task SMA0003_Violation_PartialStruct_ReadingFieldInAnotherFile()
        {
            var source1 = @"
namespace Test
{
    public partial struct PartialStruct
    {
        public static int InMainFile = {|#0:InAnotherFile|};
        public static int OkToRead = InMainFile;
    }
}
";
            var source2 = @"
namespace Test
{
    partial struct PartialStruct
    {
        public readonly static int InAnotherFile = 310;
    }
}
";
            var expected = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_AnotherFile).WithLocation(markupKey: 0).WithArguments("InAnotherFile");

            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            test.ExpectedDiagnostics.Add(expected);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Violation_PartialClass_NestedOperatorsCrossFile()
        {
            var source1 = @"
namespace Test
{
    public partial class InterFileRef
    {
        public readonly static int CrossRef = 10 + {|#0:OtherField|} + 20;
    }
}
";
            var source2 = @"
namespace Test
{
    partial class InterFileRef
    {
        public readonly static int OtherField = 310;
    }
}
";
            var expected = VerifyCS.Diagnostic(FlakyInitializationAnalyzer.RuleId_AnotherFile).WithLocation(markupKey: 0).WithArguments("OtherField");

            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            test.ExpectedDiagnostics.Add(expected);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_CrossFile_ConstFieldReference()
        {
            var source1 = @"
namespace Test
{
    public partial class CTest
    {
        public static string Value = CONST_STR;
    }
}
";
            var source2 = @"
namespace Test
{
    partial class CTest
    {
        public const string CONST_STR = ""Hello, world."";
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_CrossFile_NameOfReference()
        {
            var source1 = @"
namespace Test
{
    public partial class CTest
    {
        public static string NameOf = """" + nameof(FieldInOtherFile) + """";
    }
}
";
            var source2 = @"
namespace Test
{
    partial class CTest
    {
        public readonly static float FieldInOtherFile = 0.31f;
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_CrossFile_TypeOfReference()
        {
            var source1 = @"
using System;

namespace Test
{
    public partial class CTest
    {
        public static Type TypeField = typeof(Nested);
    }
}
";
            var source2 = @"
namespace Test
{
    partial class CTest
    {
        public static class Nested { }
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_CrossFile_StaticMethodReference()
        {
            var source1 = @"
using System;

namespace Test
{
    public partial class CTest
    {
        public static Action StaticAction = MethodInOtherFile;
    }
}
";
            var source2 = @"
namespace Test
{
    partial class CTest
    {
        public static void MethodInOtherFile() { }
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_CrossFile_AccessInsideLambdaBody()
        {
            var source1 = @"
using System;

namespace Test
{
    public partial class CTest
    {
        public static Func<int> FuncDef = () => FieldInOtherFile;
    }
}
";
            var source2 = @"
namespace Test
{
    partial class CTest
    {
        public static int FieldInOtherFile = 100;
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_CrossFile_AccessInPropertyGetterSetter()
        {
            var source1 = @"
namespace Test
{
    public partial class CTest
    {
        static float f;
        public static float Getter { get => f + FieldInOtherFile; }
        public static float Setter { set => f = FieldInOtherFile; }
    }
}
";
            var source2 = @"
namespace Test
{
    partial class CTest
    {
        public readonly static float FieldInOtherFile = 0.31f;
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task SMA0003_Compliant_PartialStruct_ReadingFieldInSameFile()
        {
            var source1 = @"
namespace Test
{
    public partial struct PartialStruct
    {
        public static int InMainFile = 310;
        public static int ReadingSameFile = InMainFile;
    }
}
";
            var source2 = @"
namespace Test
{
    partial struct PartialStruct
    {
        public readonly static int InAnotherFile = 100;
    }
}
";
            var test = new VerifyCS.Test
            {
                TestCode = source1,
            };
            test.TestState.Sources.Add(source2);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
