// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
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
        public async Task SMA0060_Violate_SimpleAssignment()
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
        public async Task SMA0063_Violate_ReadWritePropertyAccess()
        {
            var test = @"
namespace Test
{
    class C { }

    class Program
    {
        C _field;
        public C ReadWriteProp { get => _field; set => _field = value; }

        void M()
        {
            var self = this;
            _ = {|#0:self.ReadWriteProp|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(markupKey: 0)
                .WithArguments("self.ReadWriteProp", "self");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Violate_CompoundAssignment()
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
        public async Task SMA0060_Violate_IncrementAssignment()
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
        public async Task SMA0060_Violate_CoalesceAssignment_Local()
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
        public async Task SMA0061_Violate_CoalesceAssignment_Parameter()
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
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Violate_DeconstructionAssignment_ExistingVariables()
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
        public async Task SMA0060_Conform_AutoPropertyAccess()
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
        public async Task SMA0060_Conform_NullConditionalAutoPropertyAccess()
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
        public async Task SMA0060_Violate_DeconstructionAssignment_LeftExistingRightDeclared()
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
                .WithLocation(markupKey: 0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("right");
            // TODO: Remove this compiler-error expectation after upgrading Unity to a version that includes Roslyn 4+ (C# 10 support).
            // Diagnostic spans overlap and cannot use markers.
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError(identifier: "CS8184")
                .WithSpan(startLine: 9, startColumn: 13, endLine: 9, endColumn: 30);

            await VerifyWithRuleEnabledAsync(test, expectedCompiler, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0060_Violate_DeconstructionAssignment_LeftDeclaredRightExisting()
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
                .WithLocation(markupKey: 0)
                .WithArguments("left");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal)
                .WithLocation(markupKey: 1)
                .WithArguments("right");
            // TODO: Remove this compiler-error expectation after upgrading Unity to a version that includes Roslyn 4+ (C# 10 support).
            // Diagnostic spans overlap and cannot use markers.
            var expectedCompiler = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError(identifier: "CS8184")
                .WithSpan(startLine: 9, startColumn: 13, endLine: 9, endColumn: 30);

            await VerifyWithRuleEnabledAsync(test, expectedCompiler, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0060_Conform_DeconstructionDeclaration_IsAllowed()
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
        public async Task SMA0060_Conform_StringPropertyAndMethodAccess()
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
        public async Task SMA0060_Conform_ConstFieldArgument_IsAllowed()
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
        public async Task SMA0060_Conform_ReadOnlyStructGetterOnlyPropertyArgument_IsAllowed()
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
        public async Task SMA0060_Violate_SingleLetterLocal()
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
        public async Task SMA0060_Conform_MutPrefixLocal_IsAllowed()
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
        public async Task SMA0061_Violate_MethodParameterAssignment()
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
                .WithLocation(markupKey: 0)
                .WithArguments("valueParam");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0061_Violate_IndexerAndSetterParameterAssignments_ReportDiagnostic()
        {
            var test = @"
namespace Test
{
    class MyType
    {
        int _x;

        public int SetterProp
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
                .WithLocation(markupKey: 0)
                .WithArguments("value");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 2)
                .WithArguments("value");
            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 1)
                .WithArguments("index");

            await VerifyWithRuleEnabledAsync(test, expected0, expected2, expected1);
        }

        [TestMethod]
        public async Task SMA0060_Violate_MemberAccessRootedAtLocal()
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
        public async Task SMA0061_Violate_MemberAccessRootedAtParameter()
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
        void M(Box foo)
        {
            {|#0:foo.AutoPropNext.AutoPropValue|} = 310;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0062_Violate_MutableMembers_NonStringType()
        {
            var test = @"
namespace Test
{
    class C { public int Value; }
    class B
    {
        public C Field;
        public C Prop { get => Field; set => Field = value; }
        public void Do() { }
    }

    class Program
    {
        static void Use(C c) { }
        void M()
        {
            var foo = new B();
            Use({|#0:foo.Field|});
            _ = {|#1:foo.Prop|};
            {|#2:foo.Do()|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument)
                .WithLocation(markupKey: 0)
                .WithArguments("foo");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(markupKey: 1)
                .WithArguments("foo.Prop", "foo");
            var expected2 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 2)
                .WithArguments("foo.Do()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0064_Violate_Combinations_MutableReturn_ReadOnly_AutoProp()
        {
            var test = @"
namespace Test
{
    class C { public int Value; }
    struct S
    {
        public readonly C ReadOnlyAutoProp { get; }
        public readonly C ReadOnlyMethod() => null;
        public readonly C ReadOnlyBlockMethod() { return null; }
        public readonly C ReadOnlyBlockProp { get { return null; } }
        public C MutableMethod() { return null; }
        public C MutableProp { get => null; set {} }
    }

    class Program
    {
        void M()
        {
            var s = new S();
            _ = s.ReadOnlyAutoProp;
            _ = s.ReadOnlyMethod();
            _ = s.ReadOnlyBlockMethod();
            _ = s.ReadOnlyBlockProp;
            _ = {|#0:s.MutableMethod()|};
            _ = {|#1:s.MutableProp|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall)
                .WithLocation(markupKey: 0)
                .WithArguments("s.MutableMethod()", "s");
            var expected1 = VerifyCS.Diagnostic(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyPropertyArgument)
                .WithLocation(markupKey: 1)
                .WithArguments("s.MutableProp", "s");

            await VerifyWithRuleEnabledAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task SMA0060_Conform_MemberAccessRootedAtField()
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
        public async Task SMA0060_Conform_ForStatementHeaderAssignments_AreAllowed()
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
        public async Task SMA0060_Violate_ForStatementBodyAssignment_IsStillReported()
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
        public async Task SMA0060_Violate_OutVarCall_NotReported_ButSubsequentAssignment_Reported()
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
        public async Task SMA0062_Violate_MethodCall_ReferenceTypeArgument()
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
                .WithLocation(markupKey: 0)
                .WithArguments("foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Conform_MethodCall_MutPrefixArgument_IsAllowed()
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
        public async Task SMA0060_Conform_StructArgument_InParameter_IsAllowed()
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
        public async Task SMA0060_Conform_StructArgument_ReadOnlyByValue_IsAllowed()
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
        public async Task SMA0062_Violate_StructArgument_MutableByValue()
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
                .WithLocation(markupKey: 0)
                .WithArguments("s");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Conform_IndexerArgument_ReferenceType()
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
        public async Task SMA0060_Conform_MethodCallArgument_IsAllowed()
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
        public async Task SMA0060_Conform_ObjectCreationArgument_IsAllowed()
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
        public async Task SMA0062_Violate_FieldArgument()
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
                .WithLocation(markupKey: 0)
                .WithArguments("self");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Conform_ReadOnlyFieldArgument_IsAllowed()
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
        public async Task SMA0060_Conform_BuiltinPrimitives()
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
        public async Task SMA0060_Conform_PropertyAccess()
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
        public async Task SMA0060_Conform_StructGetterOnlyPropertyAccess()
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
        public async Task SMA0064_Violate_MethodCallOnRootLocal()
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
                .WithLocation(markupKey: 0)
                .WithArguments("foo.Do()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Conform_MethodCallOnMutPrefixLocal_IsAllowed()
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
        public async Task SMA0060_Conform_ReadOnlyMethodCallOnRootLocal_IsAllowed()
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
        public async Task SMA0064_Violate_MethodCallOnRootParameter()
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
                .WithLocation(markupKey: 0)
                .WithArguments("foo.Do()", "foo");

            await VerifyWithRuleEnabledAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0060_Conform_StructReadOnlyGetterOnlyPropertyAccess_IsAllowed()
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
        public async Task SMA0060_Violate_RefAssignment()
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
        public async Task SMA0060_Violate_DecrementAssignment()
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
        public async Task SMA0060_Violate_CompoundAssignment_Subtract()
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
        public async Task SMA0060_Violate_CompoundAssignment_Multiply()
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
        public async Task SMA0060_Violate_CompoundAssignment_Divide()
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
        public async Task SMA0060_Violate_CompoundAssignment_Modulo()
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
        public async Task SMA0060_Violate_CompoundAssignment_And()
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
        public async Task SMA0060_Violate_CompoundAssignment_Or()
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
        public async Task SMA0060_Violate_CompoundAssignment_Xor()
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
        public async Task SMA0060_Violate_CompoundAssignment_LeftShift()
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
        public async Task SMA0060_Violate_CompoundAssignment_RightShift()
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
        public async Task SMA0060_Conform_MethodCall_MutPrefixParameterArgument_IsAllowed()
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
        public async Task SMA0060_Conform_AnonymousObjectArgument_IsAllowed()
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
        public async Task SMA0060_Conform_ArrayCreationArgument_IsAllowed()
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
        public async Task SMA0060_Conform_OutTypedDeclarationCall_NotReported()
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
        public async Task SMA0060_Conform_OutParameterAssignment_IsAllowed()
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
        public async Task SMA0060_Conform_LiteralArgument_IsAllowed()
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
        public async Task SMA0060_Conform_DefaultArgument_IsAllowed()
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
        public async Task SMA0060_Conform_NullArgument_IsAllowed()
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
        public async Task SMA0060_Violate_PropertyAccessors_LocalAssignment()
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
        public async Task SMA0060_Violate_IndexerAccessors_LocalAssignment()
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
        public async Task SMA0060_Violate_Lambda_LocalAndParameterAssignment()
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
        public async Task SMA0060_Conform_Lambda_MutPrefix_IsAllowed()
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
        public async Task SMA0060_Conform_PropertyAccessors_MutPrefix_IsAllowed()
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
        public async Task SMA0060_Conform_IndexerAccessors_MutPrefix_IsAllowed()
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
        public async Task SMA0060_Conform_RuleSuppressed()
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
        public void SMA0060_Conform_RulesAreDisabledByDefault()
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
        public void SMA0064_Conform_MethodCallRuleIsDisabledByDefault()
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
                ("/.globalconfig", $"is_global = true\n{Core.Config_EnableImmutableVariable} = true"));

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
