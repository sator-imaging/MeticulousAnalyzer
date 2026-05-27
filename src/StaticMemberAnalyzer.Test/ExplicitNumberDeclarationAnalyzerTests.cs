using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ExplicitNumberDeclarationAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ExplicitNumberDeclarationAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8001_Violate_VarWithPrimitiveNumbers_ReportsDiagnostics()
        {
            var test = @"
namespace Test
{
    public class C
    {
        public void M()
        {
            var {|#0:v_sbyte|} = (sbyte)1;
            var {|#1:v_byte|} = (byte)1;
            var {|#2:v_short|} = (short)1;
            var {|#3:v_ushort|} = (ushort)1;
            var {|#4:v_int|} = 1;
            var {|#5:v_uint|} = 1u;
            var {|#6:v_long|} = 1L;
            var {|#7:v_ulong|} = 1UL;
            var {|#8:v_float|} = 1.0f;
            var {|#9:v_double|} = 1.0;
            var {|#10:v_decimal|} = 1.0m;
            var {|#11:v_nint|} = (nint)1;
            var {|#12:v_nuint|} = (nuint)1;
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
        public async Task SMA8001_Conform_ExplicitTypeWithPrimitiveNumbers_DoesNotReportDiagnostic()
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
        public async Task SMA8001_Conform_VarWithNonNumberTypes_DoesNotReportDiagnostic()
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
        public async Task SMA8001_Conform_MultipleVariablesInOneDeclaration_ReportsDiagnosticsForEach()
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
        public async Task SMA8001_Violate_VarWithNumberFromMembers_ReportsDiagnostics()
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
            var {|#0:foo|} = some.intField;
            var {|#1:bar|} = some.floatProperty;
            var {|#2:baz|} = some.MethodReturnsDecimal();
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
    }
}
