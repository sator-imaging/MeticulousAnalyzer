// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Linq;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.ReadOnlyVariableAnalyzer>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class ReadOnlyVariableAnalyzerUnitTests
    {
        [TestMethod]
        public async Task SimpleAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task ReadWritePropertyAccess_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C _field;
        public C Prop { get => _field; set => _field = value; }

        void M()
        {
            var self = this;
            _ = {|#0:self.Prop|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("self.Prop");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task IncrementAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CoalesceAssignment_Local_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CoalesceAssignment_Parameter_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M(int? foo)
        {
            {|#0:foo|} ??= 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task DeconstructionAssignment_ExistingVariables_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(1)
                .WithArguments("right");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task DeconstructionAssignment_LeftExistingRightDeclared_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int left = 0;
            ({|#0:left|}, var {|#1:right|}) = (1, 2);
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(1)
                .WithArguments("right");
            // TODO: Remove this compiler-error expectation after upgrading Unity to a version that includes Roslyn 4+ (C# 10 support).
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS8184")
                .WithSpan(9, 13, 9, 30);

            await VerifyWithRuleEnabledAsync(test, expectedCompiler, expected0, expected1);
        }

        [TestMethod]
        public async Task DeconstructionAssignment_LeftDeclaredRightExisting_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int right = 0;
            (var {|#0:left|}, {|#1:right|}) = (1, 2);
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(1)
                .WithArguments("right");
            // TODO: Remove this compiler-error expectation after upgrading Unity to a version that includes Roslyn 4+ (C# 10 support).
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS8184")
                .WithSpan(9, 13, 9, 30);

            await VerifyWithRuleEnabledAsync(test, expectedCompiler, expected0, expected1);
        }

        [TestMethod]
        public async Task DeconstructionDeclaration_IsAllowed()
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
        public async Task StringPropertyAndMethodAccess_IsStillReported_BecauseNotReadonly()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            var s = ""test"";
            _ = {|#0:s.Length|};
            _ = {|#1:s.ToUpper()|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("s.Length");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(1)
                .WithArguments("s.ToUpper()");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task ConstFieldArgument_IsAllowed()
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
        public async Task ReadOnlyStructGetterOnlyPropertyArgument_IsAllowed()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int X; }
    readonly struct S
    {
        public MutableStruct Prop => new MutableStruct();
    }

    class Program
    {
        static void Use(MutableStruct s) { }

        void M()
        {
            var s = new S();
            Use(s.Prop);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task SingleLetterLocal_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MutPrefixLocal_IsAllowed()
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
        public async Task MethodParameterAssignment_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M(int valueParam)
        {
            {|#0:valueParam|} = 1;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(0)
                .WithArguments("valueParam");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task IndexerAndSetterParameterAssignments_ReportDiagnostic()
        {
            var test = @"
namespace Test
{
    class MyType
    {
        int _x;

        public int MyProp
        {
            set
            {
                {|#0:value|} = 123;
                _x = value;
            }
        }

        public int this[int index]
        {
            get => _x + index;
            set
            {
                {|#1:index|} += 1;
                {|#2:value|} = index;
                _x = value;
            }
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(0)
                .WithArguments("value");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(2)
                .WithArguments("value");
            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(1)
                .WithArguments("index");

            await VerifyWithRuleEnabledAsync(test, expected0, expected2, expected1);
        }

        [TestMethod]
        public async Task MemberAccessRootedAtLocal_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Box
    {
        public Box Next { get; set; }
        public int Value { get; set; }
    }

    class Program
    {
        void M()
        {
            var foo = new Box { Next = new Box() };
            {|#0:foo.Next.Value|} = 310;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MemberAccessRootedAtParameter_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Box
    {
        public Box Next { get; set; }
        public int Value { get; set; }
    }

    class Program
    {
        void M(Box foo)
        {
            {|#0:foo.Next.Value|} = 310;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MemberAccessRootedAtField_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    class Box
    {
        public Box Next { get; set; }
        public int Value { get; set; }
    }

    class Program
    {
        private Box _foo = new Box { Next = new Box() };

        void M()
        {
            _foo.Next.Value = 310;
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task ForStatementHeaderAssignments_AreAllowed()
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
        public async Task ForStatementBodyAssignment_IsStillReported()
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
                .WithLocation(0)
                .WithArguments("i");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task OutVarCall_NotReported_ButSubsequentAssignment_Reported()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MethodCall_ReferenceTypeArgument_ReportsDiagnostic()
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
            var foo = new C();
            Use({|#0:foo|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MethodCall_MutPrefixArgument_IsAllowed()
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
        public async Task StructArgument_InParameter_IsAllowed()
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
        public async Task StructArgument_ReadOnlyByValue_IsAllowed()
        {
            var test = @"
namespace Test
{
    readonly struct S { public int X { get; } }

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
        public async Task StructArgument_MutableByValue_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct S { public int X; }

    class Program
    {
        static void Use(S value) { }

        void M()
        {
            var s = new S();
            Use({|#0:s|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(0)
                .WithArguments("s");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task IndexerArgument_ReferenceType_ReportsDiagnostic()
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
            _ = {|#0:idx[key]|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("idx[key]");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MethodCallArgument_IsAllowed()
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
        public async Task ObjectCreationArgument_IsAllowed()
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
        public async Task FieldArgument_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C _field = new C();
        static void Use(C value) { }

        void M()
        {
            var self = this;
            Use({|#0:self._field|});
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(0)
                .WithArguments("self");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task ReadOnlyFieldArgument_IsAllowed()
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
        public async Task PropertyAccess_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C Prop => new C();

        void M()
        {
            var self = this;
            _ = {|#0:self.Prop|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("self.Prop");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task StructGetterOnlyPropertyAccess_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int X; }
    struct S
    {
        public MutableStruct Prop => new MutableStruct();
    }

    class Program
    {
        void M()
        {
            var s = new S();
            _ = {|#0:s.Prop|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(0)
                .WithArguments("s.Prop");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MethodCallOnImmutableLocal_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class C { public void Do() { } }

    class Program
    {
        void M()
        {
            var foo = new C();
            {|#0:foo.Do()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(0)
                .WithArguments("foo.Do()");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MethodCallOnMutPrefixLocal_IsAllowed()
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
        public async Task ReadOnlyMethodCallOnImmutableLocal_IsAllowed()
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
        public async Task MethodCallOnImmutableParameter_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class C { public void Do() { } }

    class Program
    {
        void M(C foo)
        {
            {|#0:foo.Do()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(0)
                .WithArguments("foo.Do()");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task StructReadOnlyGetterOnlyPropertyAccess_IsAllowed()
        {
            var test = @"
namespace Test
{
    struct MutableStruct { public int X; }
    struct S
    {
        public readonly MutableStruct Prop => new MutableStruct();
    }

    class Program
    {
        static void Use(MutableStruct s) { }

        void M()
        {
            var s = new S();
            Use(s.Prop);
        }
    }
}
";

            await VerifyWithRuleEnabledAsync(test);
        }

        [TestMethod]
        public async Task RefAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("r");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task DecrementAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_Subtract_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_Multiply_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_Divide_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_Modulo_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_And_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_Or_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_Xor_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_LeftShift_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task CompoundAssignment_RightShift_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task MethodCall_MutPrefixParameterArgument_IsAllowed()
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
        public async Task AnonymousObjectArgument_IsAllowed()
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
        public async Task ArrayCreationArgument_IsAllowed()
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
        public async Task OutTypedDeclarationCall_NotReported()
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
        public async Task OutParameterAssignment_IsAllowed()
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
        public async Task LiteralArgument_IsAllowed()
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
        public async Task DefaultArgument_IsAllowed()
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
        public async Task NullArgument_IsAllowed()
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
        public async Task PropertyAccessors_LocalAssignment_ReportsDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        public int MyProp
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
                .WithLocation(0)
                .WithArguments("x");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(1)
                .WithArguments("y");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task IndexerAccessors_LocalAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("x");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(1)
                .WithArguments("y");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task Lambda_LocalAndParameterAssignment_ReportsDiagnostic()
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
                .WithLocation(0)
                .WithArguments("x");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(1)
                .WithArguments("p");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task Lambda_MutPrefix_IsAllowed()
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
        public async Task PropertyAccessors_MutPrefix_IsAllowed()
        {
            var test = @"
namespace Test
{
    class Program
    {
        public int MyProp
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
        public async Task IndexerAccessors_MutPrefix_IsAllowed()
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
        public async Task RuleSuppressed_NoDiagnostic()
        {
            var test = @"
namespace Test
{
    class Program
    {
        void M()
        {
            int foo = 0;
            foo = 1;
        }
    }
}
";

            var verifier = new VerifyCS.Test
            {
                TestCode = test,
            };

            verifier.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                var compilationOptions = project?.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                var specificOptions = compilationOptions.SpecificDiagnosticOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                    ReportDiagnostic.Suppress);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter,
                    ReportDiagnostic.Suppress);
                specificOptions = specificOptions.SetItem(
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument,
                    ReportDiagnostic.Suppress);

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificOptions);
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });

            await verifier.RunAsync();
        }

        [TestMethod]
        public void RulesAreDisabledByDefault()
        {
            var analyzer = new ReadOnlyVariableAnalyzer();
            var ids = new[]
            {
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal,
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter,
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument,
                ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument,
            };

            foreach (var id in ids)
            {
                var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == id);
                Assert.IsFalse(descriptor.IsEnabledByDefault, $"{id} should be disabled by default");
            }
        }

        [TestMethod]
        public void MethodCallRuleIsDisabledByDefault()
        {
            var analyzer = new ReadOnlyVariableAnalyzer();
            var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall);
            Assert.IsFalse(descriptor.IsEnabledByDefault, $"{ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall} should be disabled by default");
        }


        private static async Task VerifyWithRuleEnabledAsync(string source, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
            };

            test.TestState.AnalyzerConfigFiles.Add(
                ("/.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.category-ImmutableVariable.severity = error"));

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
                    ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument,
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
