// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCs = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.ExplicitNumberDeclarationAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8001_ExplicitNumberDeclarationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8001_Violation_VarWithPrimitiveNumbers()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            {|#0:var|} v_sbyte = (sbyte)1;
            {|#1:var|} v_byte = (byte)1;
            {|#2:var|} v_short = (short)1;
            {|#3:var|} v_ushort = (ushort)1;
            {|#4:var|} v_int = 1;
            {|#5:var|} v_uint = 1u;
            {|#6:var|} v_long = 1L;
            {|#7:var|} v_ulong = 1UL;
            {|#8:var|} v_float = 1.0f;
            {|#9:var|} v_double = 1.0;
            {|#10:var|} v_decimal = 1.0m;
            {|#11:var|} v_nint = (nint)1;
            {|#12:var|} v_nuint = (nuint)1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("v_sbyte"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(1).WithArguments("v_byte"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(2).WithArguments("v_short"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(3).WithArguments("v_ushort"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(4).WithArguments("v_int"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(5).WithArguments("v_uint"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(6).WithArguments("v_long"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(7).WithArguments("v_ulong"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(8).WithArguments("v_float"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(9).WithArguments("v_double"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(10).WithArguments("v_decimal"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(11).WithArguments("v_nint"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(12).WithArguments("v_nuint")
            );
        }

        [TestMethod]
        public async Task SMA8001_Compliant_ExplicitTypeWithPrimitiveNumbers()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            sbyte v_sbyte = 1;
            byte v_byte = 1;
            short v_short = 1;
            ushort v_ushort = 1;
            int v_int = 1;
            uint v_uint = 1;
            long v_long = 1;
            ulong v_ulong = 1;
            float v_float = 1;
            double v_double = 1;
            decimal v_decimal = 1;
            nint v_nint = 1;
            nuint v_nuint = 1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8001_Compliant_OutDiscardAssignment()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class C
    {
        public void M(Dictionary<string, int> dict)
        {
            if (dict.TryGetValue(""key"", out _))
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8001_Compliant_VarWithNonNumberTypes()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            var v_string = ""string"";
            var v_char = 'c';
            var v_bool = true;
            var v_obj = new object();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8001_Compliant_MultipleVariablesInOneDeclaration()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            // Note: var cannot be used with multiple variables in one declaration in C#.
            // But our analyzer handles VariableDeclarationSyntax which can have multiple variables.
            // If someone uses a custom language or some weird scenario where it happens, we are ready.
            // Actually, 'var x = 1, y = 2;' is not valid C#.
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8001_Violation_VarWithNumberFromMembers()
        {
            var test = @"
namespace Test
{
    public class Some
    {
        public int intField;
        public float floatProperty { get; set; }
        public decimal MethodReturnsDecimal() => 0m;
    }

    public class C
    {
        public void M(Some some)
        {
            {|#0:var|} foo = some.intField;
            {|#1:var|} bar = some.floatProperty;
            {|#2:var|} baz = some.MethodReturnsDecimal();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("foo"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(1).WithArguments("bar"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(2).WithArguments("baz")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_OutVarDeclaration()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class C
    {
        public void M(Dictionary<string, int> dict)
        {
            if (dict.TryGetValue(""key"", out {|#0:var|} value))
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("value")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_ForEachVariable()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach ({|#0:var|} item in new int[] { 1, 2, 3 })
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("item")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_Deconstruction()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            {|#0:var|} (a, b) = (1, 2.0);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("a"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("b")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_ForEachDeconstruction()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach ({|#0:var|} (x, y) in new (int, int)[] { (1, 2) })
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("x"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("y")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_ForEachPartialDeconstructionWithVar()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach ((_, {|#0:var|} x) in new (int, int)[] { (1, 2) })
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("x")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_ForEachDeconstructionWithMixedVar()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach (({|#0:var|} _, {|#1:var|} x) in new (int, int)[] { (1, 2) })
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("_"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(1).WithArguments("x")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_DeconstructionWithDiscard()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            {|#0:var|} (_, b) = (1, 2);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("_"),
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("b")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_OutVarWithDiscard()
        {
            var test = @"
using System.Collections.Generic;

namespace Test
{
    public class C
    {
        public void M(Dictionary<string, int> dict)
        {
            if (dict.TryGetValue(""key"", out {|#0:var|} _))
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("_")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_ForEachWithDiscard()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            foreach ({|#0:var|} _ in new int[] { 1, 2, 3 })
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("_")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_DiscardVariableDeclaration()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            {|#0:var|} _ = 1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("_")
            );
        }

        [TestMethod]
        public async Task SMA8001_Violation_PartialDeconstructionWithVar()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            (_, {|#0:var|} x) = (1, 2);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber).WithLocation(0).WithArguments("x")
            );
        }


        [TestMethod]
        public async Task SMA8001_Compliant_PureDiscardAssignment()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            _ = 1;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
