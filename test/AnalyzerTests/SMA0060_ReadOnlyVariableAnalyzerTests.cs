// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0060_ReadOnlyVariableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0060_Violation_SimpleAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            {|#0:foo|} = 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            {|#0:foo|} += 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_IncrementAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            {|#0:foo|}++;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CoalesceAssignment_Local()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int? foo = null;
            {|#0:foo|} ??= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_DeconstructionAssignment_ExistingVariables()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int left = 0;
            int right = 0;
            ({|#0:left|}, {|#1:right|}) = (1, 2);
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("right");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_AutoPropertyAccess()
        {
            var test = @"
namespace Test
{
    class C { public int AutoProp { get; set; } }

    class Program
    {
        void M(C foo)
        {
            _ = foo.AutoProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_NullConditionalAutoPropertyAccess()
        {
            var test = @"
namespace Test
{
    class C { public int AutoProp { get; set; } }

    class Program
    {
        void M(C foo)
        {
            _ = foo?.AutoProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_DeconstructionAssignment_LeftExistingRightDeclared()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int left = 0;
            {|#2:({|#0:left|}, var {|#1:right|})|} = (1, 2);
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("right");
            // TODO: Remove this compiler-error expectation after upgrading Unity to a version that includes Roslyn 4+ (C# 10 support).
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError(identifier: "CS8184")
                .WithLocation(markupKey: 2);

            await VerifyWithRuleEnabledAsync(test, expectedCompiler, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0060_Violation_DeconstructionAssignment_LeftDeclaredRightExisting()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int right = 0;
            {|#2:(var {|#0:left|}, {|#1:right|})|} = (1, 2);
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("right");
            // TODO: Remove this compiler-error expectation after upgrading Unity to a version that includes Roslyn 4+ (C# 10 support).
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError(identifier: "CS8184")
                .WithLocation(markupKey: 2);

            await VerifyWithRuleEnabledAsync(test, expectedCompiler, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_DeconstructionDeclaration()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            var tuple = (1, 2);
            var (leftValue, rightValue) = tuple;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_StringPropertyAndMethodAccess()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            var s = ""test"";
            _ = s.Length;
            _ = s.ToUpper();
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ConstFieldArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        const int MyConst = 10;
        readonly int OtherConst = 20;
        static void Use(int value) { }

        void M()
        {
            Use(MyConst);

            Program foo = this;
            Use(foo.OtherConst);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ReadOnlyStructGetterOnlyPropertyArgument()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int IntField; }
    readonly struct S
    {
        public MutableStruct ReadOnlyProp => new MutableStruct();
    }

    class Program
    {
        static void Use(MutableStruct s) { }

        void M()
        {
            var s = new S();
            Use(s.ReadOnlyProp);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_SingleLetterLocal()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int i = 0;
            {|#0:i|} = 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_MutPrefixLocal()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int mut_count = 0;
            mut_count = 1;
            mut_count += 2;
            mut_count++;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_MemberAccessRootedAtLocal()
        {
            var test = @"
namespace Test
{
    class Box
    {
        public Box AutoPropNext { get; set; }
        public int AutoPropValue { get; set; }
    }

    class Program
    {
        void M()
        {
            var foo = new Box { AutoPropNext = new Box() };
            {|#0:foo.AutoPropNext.AutoPropValue|} = 310;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_MemberAccessRootedAtField()
        {
            var test = @"
namespace Test
{
    class Box
    {
        public Box AutoPropNext { get; set; }
        public int AutoPropValue { get; set; }
    }

    class Program
    {
        private Box _foo = new Box { AutoPropNext = new Box() };

        void M()
        {
            _foo.AutoPropNext.AutoPropValue = 310;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ForStatementHeaderAssignments()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M(int p)
        {
            int i = 0;
            for (i = 0, p += 1; (i += 1) < 10; i += 2, p--)
            {
            }
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_ForStatementBodyAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            for (int i = 0; i < 10; i++)
            {
                {|#0:i|} = 2;
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_OutVarCall_ButSubsequentAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void N()
        {
            int.TryParse(""1"", out var foo);
            {|#0:foo|} = 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_MethodCall_MutPrefixArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        static void Use(C value) { }

        void M()
        {
            var mut_foo = new C();
            Use(mut_foo);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_StructArgument_InParameter()
        {
            var test = @"
namespace Test
{
    struct S { public int X; }

    class Program
    {
        static void Use(in S value) { }

        void M()
        {
            var s = new S();
            Use(s);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_StructArgument_ReadOnlyByValue()
        {
            var test = @"
namespace Test
{
    readonly struct S { public int AutoProp { get; } }

    class Program
    {
        static void Use(S value) { }

        void M()
        {
            var s = new S();
            Use(s);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_IndexerArgument_ReferenceType()
        {
            var test = @"
namespace Test
{
    class MyIndexer
    {
        public int this[string key] => 0;
    }

    class Program
    {
        void M()
        {
            var idx = new MyIndexer();
            var key = ""A"";
            _ = idx[key];
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_MethodCallArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        static void Use(C value) { }
        static C Create() => new C();

        void M()
        {
            Use(Create());
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ObjectCreationArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        static void Use(C value) { }

        void M()
        {
            Use(new C());
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ReadOnlyFieldArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        readonly C _field = new C();
        static void Use(C value) { }

        void M()
        {
            var self = this;
            Use(self._field);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_BuiltinPrimitives()
        {
            var test = @"
using System;
namespace Test
{
    struct MutableStruct_ReadOnlyPrimitifeFields
    {
        readonly int integer;
        readonly float number;
        readonly DateTime temporal;
        readonly string text;

        void M()
        {
            Use(integer);
            Use(number);
            Use(temporal);
            Use(text);
        }
        void Use(int v) {}
        void Use(float v) {}
        void Use(DateTime v) {}
        void Use(string v) {}
    }

    class Class_ReadOnlyPrimitifeFields
    {
        readonly int integer;
        readonly float number;
        readonly DateTime temporal;
        readonly string text;

        void M()
        {
            Use(integer);
            Use(number);
            Use(temporal);
            Use(text);
        }
        void Use(int v) {}
        void Use(float v) {}
        void Use(DateTime v) {}
        void Use(string v) {}
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_PropertyAccess()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C ReadOnlyProp => new C();

        void M()
        {
            var self = this;
            _ = self.ReadOnlyProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_StructGetterOnlyPropertyAccess()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int IntField; }
    struct S
    {
        public MutableStruct ReadOnlyProp => new MutableStruct();
    }

    class Program
    {
        void M()
        {
            var s = new S();
            _ = s.ReadOnlyProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_MethodCallOnMutPrefixLocal()
        {
            var test = @"
namespace Test
{
    class C { public void Do() { } }

    class Program
    {
        void M()
        {
            var mut_foo = new C();
            mut_foo.Do();
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ReadOnlyMethodCallOnRootLocal()
        {
            var test = @"
namespace Test
{
    struct S { public readonly void Do() { } }

    class Program
    {
        void M()
        {
            var foo = new S();
            foo.Do();
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_StructReadOnlyGetterOnlyPropertyAccess()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int IntField; }
    struct S
    {
        public readonly MutableStruct ReadOnlyProp => new MutableStruct();
    }

    class Program
    {
        static void Use(MutableStruct s) { }

        void M()
        {
            var s = new S();
            Use(s.ReadOnlyProp);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_RefAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            int bar = 1;
            ref int r = ref foo;
            {|#0:r|} = ref bar;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("r");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_DecrementAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            {|#0:foo|}--;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_Subtract()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            {|#0:foo|} -= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_Multiply()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 1;
            {|#0:foo|} *= 2;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_Divide()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 2;
            {|#0:foo|} /= 2;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_Modulo()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 2;
            {|#0:foo|} %= 2;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_And()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 1;
            {|#0:foo|} &= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_Or()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 1;
            {|#0:foo|} |= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_Xor()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 1;
            {|#0:foo|} ^= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_LeftShift()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 1;
            {|#0:foo|} <<= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_CompoundAssignment_RightShift()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 1;
            {|#0:foo|} >>= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_MethodCall_MutPrefixParameterArgument()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        static void Use(C value) { }

        void M(C mut_value)
        {
            Use(mut_value);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_AnonymousObjectArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        static void Use(object value) { }

        void M()
        {
            Use(new { X = 1 });
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ArrayCreationArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        static void Use(int[] value) { }

        void M()
        {
            Use(new[] { 1, 2, 3 });
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_OutTypedDeclarationCall()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void N()
        {
            int.TryParse(""1"", out int foo);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_OutParameterAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M(out int result)
        {
            result = 0;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_LiteralArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        static void UseInt(int value) { }
        static void UseString(string value) { }

        void M()
        {
            UseInt(0);
            UseString(""text"");
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_DefaultArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        static void UseInt(int value) { }
        static void UseString(string value) { }

        void M()
        {
            UseInt(default);
            UseString(default);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_NullArgument()
        {
            var test = @"
namespace Test
{
    class Program
    {
        static void Use(object value) { }

        void M()
        {
            Use(null);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_PropertyAccessors_LocalAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        public int ReadWriteProp
        {
            get
            {
                int x = 0;
                {|#0:x|} = 1;
                return x;
            }
            set
            {
                int y = 0;
                {|#1:y|} = value;
            }
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("x");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("y");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0060_Violation_IndexerAccessors_LocalAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        public int this[int index]
        {
            get
            {
                int x = 0;
                {|#0:x|} = index;
                return x;
            }
            set
            {
                int y = 0;
                {|#1:y|} = value;
            }
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("x");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("y");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0060_Violation_Lambda_LocalAndParameterAssignment()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        void M()
        {
            Action<int> a = (p) => {
                int x = 0;
                {|#0:x|} = p;
                {|#1:p|} = 1;
            };
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("x");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 1)
                .WithArguments("p");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_Lambda_MutPrefix()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        void M()
        {
            Action<int> a = (mut_p) => {
                int mut_x = 0;
                mut_x = mut_p;
                mut_p = 1;
            };
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_PropertyAccessors_MutPrefix()
        {
            var test = @"
namespace Test
{
    class Program
    {
        public int ReadWriteProp
        {
            get
            {
                int mut_x = 0;
                mut_x = 1;
                return mut_x;
            }
            set
            {
                int mut_y = 0;
                mut_y = value;
            }
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_IndexerAccessors_MutPrefix()
        {
            var test = @"
namespace Test
{
    class Program
    {
        public int this[int index]
        {
            get
            {
                int mut_x = 0;
                mut_x = index;
                return mut_x;
            }
            set
            {
                int mut_y = 0;
                mut_y = value;
            }
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_WhileStatementCondition_SimpleAssignment()
        {
            var test = @"
using System.IO;
namespace Test
{
    class Program
    {
        void M(Stream mut_stream)
        {
            int read;
            while ((read = mut_stream.Read(new byte[0], 0, 0)) > 0)
            {
            }
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Violation_WhileStatementCondition_CompoundAssignment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int i = 0;
            while (({|#0:i|} += 1) < 10)
            {
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_WhileStatementCondition_Increment()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int i = 0;
            while ({|#0:i|}++ < 10)
            {
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Violation_WhileStatementBody_Assignment()
        {
            var test = @"
using System.IO;
namespace Test
{
    class Program
    {
        void M(Stream mut_stream)
        {
            int read;
            while ((read = mut_stream.Read(new byte[0], 0, 0)) > 0)
            {
                {|#0:read|} = 0;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 0)
                .WithArguments("read");

            await VerifyWithRuleEnabledAsync(test, expected);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_WithMiddleAutoProp()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int AutoProp { get; set; }
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public B AutoPropB { get; set; }
        public readonly B ReadOnlyPropB => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.AutoPropB.ReadOnlyProp;
            _ = foo.AutoPropB.ReadOnlyProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_AllReadOnly()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public readonly B ReadOnlyProp => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.ReadOnlyProp.ReadOnlyProp;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_WithEndAutoProp()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int AutoProp { get; set; }
    }

    struct C
    {
        public readonly B ReadOnlyPropB => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.ReadOnlyPropB.AutoProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_WithField_IgnoresFieldReadOnlyPropButChecksProp()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public int AutoProp { get; set; }
    }

    struct C
    {
        public B FieldB;
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.FieldB.AutoProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_ReadOnlyAutoProperty_WithMutableReturnType()
        {
            var test = @"
namespace Test
{
    class C { public int Value { get; set; } }
    struct B
    {
        public readonly C AutoProp { get; }
    }

    class Program
    {
        void M()
        {
            var foo = new B();
            _ = foo.AutoProp;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_WithReadOnlyMethodInChain()
        {
            var test = @"
namespace Test
{
    struct B
    {
        public readonly int ReadOnlyProp => 1;
    }

    struct C
    {
        public readonly B GetBReadOnly() => new B();
    }

    class Program
    {
        void M()
        {
            var foo = new C();
            _ = foo.GetBReadOnly().ReadOnlyProp;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_ThroughThis_IfAllReadOnly()
        {
            var test = @"
namespace Test
{
    struct B { public readonly int ReadOnlyProp => 1; }
    struct Program
    {
        public readonly B ReadOnlyPropB => new B();
        void M()
        {
            _ = this.ReadOnlyPropB.ReadOnlyProp;
            _ = ReadOnlyPropB.ReadOnlyProp;
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_ChainedAccess_StaticMember_AtStartOfChain()
        {
            var test = @"
namespace Test
{
    struct B { public int AutoProp { get; set; } }
    class S
    {
        public static B ReadOnlyPropStatic => new B();
    }
    class Program
    {
        void M()
        {
            _ = S.ReadOnlyPropStatic.AutoProp;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_IEnumerableArgument()
        {
            var test = @"
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Test
{
    class Program
    {
        static void Use(IEnumerable value) { }
        static void UseGeneric(IEnumerable<int> value) { }

        void M(IEnumerable eParam, IEnumerable<int> egParam)
        {
            IEnumerable e = null;
            IEnumerable<int> eg = null;
            Use(e);
            UseGeneric(eg);
            Use(eParam);
            UseGeneric(egParam);

            // LINQ methods
            eg.Any();
            egParam.Any();
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_EnumArgument()
        {
            var test = @"
namespace Test
{
    enum E { A }
    class Program
    {
        static void Use(E value) { }

        void M(E eParam)
        {
            E e = E.A;
            Use(e);
            Use(eParam);
        }
    }
}
";
            await VerifyWithRuleEnabledAsync(test);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_LambdaArgument_ButViolationInside()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        static void Use(Action action) { }

        void M()
        {
            Use(() => {
                int x = 0;
                {|#0:x|} = 1;
            });
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal).WithLocation(markupKey: 0).WithArguments("x");

            await VerifyWithRuleEnabledAsync(test, expected0);
        }


        [TestMethod]
        public async Task SMA0060_Compliant_AnonymousMethodArgument()
        {
            var test = @"
using System;
namespace Test
{
    class Program
    {
        static void Use(Action action) { }

        void M()
        {
            Use(delegate {
                int x = 0;
                {|#0:x|} = 1;
            });
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal).WithLocation(markupKey: 0).WithArguments("x");

            await VerifyWithRuleEnabledAsync(test, expected0);
        }


        private static async Task VerifyWithRuleEnabledAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            test.TestState.AnalyzerConfigFiles.Add(
                ("/.globalconfig", $"is_global = true\n{Core.Config_EnableImmutableVariable} = enable"));

            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var compilationOptions = project?.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                var specificOptions = compilationOptions.SpecificDiagnosticOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState,
                    ReportDiagnostic.Error);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall,
                    ReportDiagnostic.Error);

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
