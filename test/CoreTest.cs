// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis;
using System;
using System.Collections.Generic;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class CoreTest
    {
        static CSharpCompilation CreateCompilation(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            return CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        static T FindFirst<T>(SyntaxNode root) where T : SyntaxNode
        {
            foreach (var node in root.DescendantNodes())
            {
                if (node is T match)
                    return match;
            }
            throw new InvalidOperationException($"No node of type {typeof(T).Name} found");
        }

        // ===== IsKnownImmutableType =====

        [TestMethod]
        public void IsKnownImmutableType_Null_ReturnsFalse()
        {
            Assert.IsFalse(Core.IsKnownImmutableType(null));
        }

        [TestMethod]
        public void IsKnownImmutableType_String_ReturnsTrue()
        {
            var comp = CreateCompilation("class C { string x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_Enum_ReturnsTrue()
        {
            var comp = CreateCompilation("enum E { A } class C { E x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_ReadOnlyStruct_ReturnsTrue()
        {
            var comp = CreateCompilation("readonly struct S {} class C { S x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_SystemInt_ReturnsTrue()
        {
            var comp = CreateCompilation("class C { int x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_IEnumerableGeneric_ReturnsTrue()
        {
            var comp = CreateCompilation("using System.Collections.Generic; class C { IEnumerable<int> x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_IReadOnlyList_ReturnsTrue()
        {
            var comp = CreateCompilation("using System.Collections.Generic; class C { IReadOnlyList<int> x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_IReadOnlyCollection_ReturnsTrue()
        {
            var comp = CreateCompilation("using System.Collections.Generic; class C { IReadOnlyCollection<int> x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_IEnumerableNonGeneric_ReturnsTrue()
        {
            var comp = CreateCompilation("using System.Collections; class C { IEnumerable x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_NonImmutableClass_ReturnsFalse()
        {
            var comp = CreateCompilation("class MyClass {} class C { MyClass x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsFalse(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_SystemUri_ReturnsTrue()
        {
            var source = "class C { System.Uri x; }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_SystemVersion_ReturnsTrue()
        {
            var source = "class C { System.Version x; }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Version).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_SystemType_ReturnsTrue()
        {
            var source = "class C { System.Type x; }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Type).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsTrue(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_SystemException_ReturnsFalse()
        {
            var source = "class C { System.Exception x; }";
            var comp = CreateCompilation(source);
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsFalse(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_GlobalClass_ReturnsFalse()
        {
            var source = "class MyGlobalClass {} class C { MyGlobalClass x; }";
            var comp = CreateCompilation(source);
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsFalse(Core.IsKnownImmutableType(type));
        }

        [TestMethod]
        public void IsKnownImmutableType_CustomUri_ReturnsFalse()
        {
            var source = "namespace NotSystem { class Uri {} } class C { NotSystem.Uri x; }";
            var comp = CreateCompilation(source);
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type;
            Assert.IsFalse(Core.IsKnownImmutableType(type));
        }

        // ===== GetMemberNamePrefix =====

        [TestMethod]
        public void GetMemberNamePrefix_Null_ReturnsEmpty()
        {
            var result = Core.GetMemberNamePrefix(null);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void GetMemberNamePrefix_InClass_ReturnsClassName()
        {
            var tree = CSharpSyntaxTree.ParseText("class MyClass { int x; }");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var result = Core.GetMemberNamePrefix(field);
            Assert.AreEqual("MyClass", result);
        }

        [TestMethod]
        public void GetMemberNamePrefix_InNamespaceAndClass_ReturnsBoth()
        {
            var tree = CSharpSyntaxTree.ParseText("namespace MyNs { class MyClass { int x; } }");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var result = Core.GetMemberNamePrefix(field);
            Assert.IsTrue(result.Contains("MyNs"));
            Assert.IsTrue(result.Contains("MyClass"));
        }

        [TestMethod]
        public void GetMemberNamePrefix_NestedClass_ReturnsBothClassNames()
        {
            var tree = CSharpSyntaxTree.ParseText("class Outer { class Inner { int x; } }");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var result = Core.GetMemberNamePrefix(field);
            Assert.IsTrue(result.Contains("Outer"));
            Assert.IsTrue(result.Contains("Inner"));
        }

        [TestMethod]
        public void GetMemberNamePrefix_DeepNamespace_ReturnsFullNamespace()
        {
            var tree = CSharpSyntaxTree.ParseText("namespace A.B.C { class MyClass { int x; } }");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var result = Core.GetMemberNamePrefix(field);
            Assert.IsTrue(result.Contains("A.B.C"));
            Assert.IsTrue(result.Contains("MyClass"));
        }

        // ===== SpanConcat =====

        [TestMethod]
        public void SpanConcat_TwoStrings_ReturnsConcatenated()
        {
            var result = Core.SpanConcat("Hello".AsSpan(), "World".AsSpan());
            Assert.AreEqual("HelloWorld", result);
        }

        [TestMethod]
        public void SpanConcat_EmptyLeft_ReturnsRight()
        {
            var result = Core.SpanConcat("".AsSpan(), "World".AsSpan());
            Assert.AreEqual("World", result);
        }

        [TestMethod]
        public void SpanConcat_EmptyRight_ReturnsLeft()
        {
            var result = Core.SpanConcat("Hello".AsSpan(), "".AsSpan());
            Assert.AreEqual("Hello", result);
        }

        [TestMethod]
        public void SpanConcat_BothEmpty_ReturnsEmpty()
        {
            var result = Core.SpanConcat("".AsSpan(), "".AsSpan());
            Assert.AreEqual("", result);
        }

        // ===== IsSuppressedByComment =====

        [TestMethod]
        public void IsSuppressedByComment_LocalDeclarationWithComment_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // suppress
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_LocalDeclarationWithoutComment_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_FieldDeclarationWithComment_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    // suppress
    int x = 1;
}");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(field, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_FieldDeclarationWithoutComment_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    int x = 1;
}");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(field, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_LambdaWithComment_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        System.Action a =
        // suppress
        () => {};
    }
}");
            var lambda = FindFirst<LambdaExpressionSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(lambda, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_AssignmentWithDiscardTrue_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // suppress
        _ = 1;
    }
}");
            var assignment = FindFirst<AssignmentExpressionSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(assignment, "// suppress", isDiscardOperation: true));
        }

        [TestMethod]
        public void IsSuppressedByComment_AssignmentWithDiscardFalse_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // suppress
        _ = 1;
    }
}");
            var assignment = FindFirst<AssignmentExpressionSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(assignment, "// suppress", isDiscardOperation: false));
        }

        [TestMethod]
        public void IsSuppressedByComment_Null_ReturnsFalse()
        {
            Assert.IsFalse(Core.IsSuppressedByComment((SyntaxNode?)null, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_WrongComment_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // other comment
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_UnsupportedNodeType_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    // suppress
    void M() {}
}");
            var method = FindFirst<MethodDeclarationSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(method, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_BlankLineBetweenCommentAndStatement_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // suppress

        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_SuppressionIsSecondComment_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // other comment
        // suppress
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_LineEndCommentOnPrecedingLine_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        int y = 0; // suppress
        var x = 1;
    }
}");
            var root = tree.GetRoot();
            var locals = new List<LocalDeclarationStatementSyntax>();
            foreach (var node in root.DescendantNodes())
            {
                if (node is LocalDeclarationStatementSyntax l)
                    locals.Add(l);
            }
            // second local declaration is "var x = 1;"
            var target = locals[1];
            Assert.IsFalse(Core.IsSuppressedByComment(target, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_LineEndCommentOnPrecedingLine_WithBlankLine_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        int y = 0; // suppress

        var x = 1;
    }
}");
            var root = tree.GetRoot();
            var locals = new List<LocalDeclarationStatementSyntax>();
            foreach (var node in root.DescendantNodes())
            {
                if (node is LocalDeclarationStatementSyntax l)
                    locals.Add(l);
            }
            // second local declaration is "var x = 1;"
            var target = locals[1];
            Assert.IsFalse(Core.IsSuppressedByComment(target, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_MultipleCommentsWithBlankLineBetween_FirstMatches_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // suppress

        // other comment
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_CaseInsensitive_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        // SUPPRESS this
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(local, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_FieldWithBlankLineAfterComment_ReturnsTrue()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    // suppress

    int x = 1;
}");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            Assert.IsTrue(Core.IsSuppressedByComment(field, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_FieldWithNonMatchingFirstComment_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    // other
    // suppress
    int x = 1;
}");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(field, "// suppress"));
        }

        // ===== UnwrapAllNullCoalesceOperation =====

        [TestMethod]
        public void UnwrapAllNullCoalesceOperation_NonConditionalAccess_ReturnsSame()
        {
            var source = @"
class C {
    void M() {
        int x = 1;
    }
}";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            var op = model.GetOperation(local.Declaration.Variables[0].Initializer.Value);
            if (op != null)
            {
                var result = Core.UnwrapAllNullCoalesceOperation(op);
                Assert.AreSame(op, result);
            }
        }

        [TestMethod]
        public void UnwrapAllNullCoalesceOperation_ConditionalAccess_ReturnsInnerOperation()
        {
            var source = @"
class C {
    void M(string s) {
        var x = s?.Length;
    }
}";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);
            var conditional = FindFirst<ConditionalAccessExpressionSyntax>(tree.GetRoot());
            var op = model.GetOperation(conditional);
            if (op is IConditionalAccessOperation condOp)
            {
                var result = Core.UnwrapAllNullCoalesceOperation(condOp);
                Assert.AreNotSame(condOp, result);
                Assert.AreSame(condOp.Operation, result);
            }
        }

        // ===== ToDiagnosticMessageName =====

        [TestMethod]
        public void ToDiagnosticMessageName_Local_ReturnsSimpleName()
        {
            var comp = CreateCompilation("class C { void M() { int foo; } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var localDecl = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            var local = model.GetDeclaredSymbol(localDecl.Declaration.Variables[0])!;
            Assert.AreEqual("foo", Core.ToDiagnosticMessageName(local));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_Parameter_ReturnsSimpleName()
        {
            var comp = CreateCompilation("class C { void M(params int[] values) { } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var method = FindFirst<MethodDeclarationSyntax>(tree.GetRoot());
            var methodSymbol = model.GetDeclaredSymbol(method)!;
            Assert.AreEqual("values", Core.ToDiagnosticMessageName(methodSymbol.Parameters[0]));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_NonGenericType_ReturnsSimpleName()
        {
            var comp = CreateCompilation("class MyClass {} class C { MyClass x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type!;
            Assert.AreEqual("MyClass", Core.ToDiagnosticMessageName(type));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_GenericType_ReturnsNameWithTypeParameters()
        {
            var comp = CreateCompilation("using System.Collections.Generic; class C { List<int> x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type!;
            Assert.AreEqual("List<int>", Core.ToDiagnosticMessageName(type));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_GenericStructType_ReturnsNameWithTypeParameters()
        {
            var comp = CreateCompilation("struct MyStruct<T> {} class C { MyStruct<int> x; }");
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var field = FindFirst<FieldDeclarationSyntax>(comp.SyntaxTrees[0].GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type!;
            Assert.AreEqual("MyStruct<int>", Core.ToDiagnosticMessageName(type));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_NestedGenericType_IncludesOuterTypeWithoutNamespace()
        {
            var comp = CreateCompilation(@"
namespace MyNamespace {
    class Outer<T> {
        class Inner<U> {
            Inner<U> _field;
        }
    }
}");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var type = model.GetTypeInfo(field.Declaration.Type).Type!;
            Assert.AreEqual("Outer<T>.Inner<U>", Core.ToDiagnosticMessageName(type));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_Field_ReturnsSimpleName()
        {
            var comp = CreateCompilation("class C { int _field; }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var fieldSymbol = model.GetDeclaredSymbol(field.Declaration.Variables[0])!;
            Assert.AreEqual("_field", Core.ToDiagnosticMessageName(fieldSymbol));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_Method_ReturnsNameWithTypeParameters()
        {
            var comp = CreateCompilation("class C { void Foo<T>(int value) { } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var method = FindFirst<MethodDeclarationSyntax>(tree.GetRoot());
            var methodSymbol = model.GetDeclaredSymbol(method)!;
            Assert.AreEqual("Foo<T>", Core.ToDiagnosticMessageName(methodSymbol));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_Property_ReturnsSimpleName()
        {
            var comp = CreateCompilation("class C { float Bar { get; set; } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var property = FindFirst<PropertyDeclarationSyntax>(tree.GetRoot());
            var propertySymbol = model.GetDeclaredSymbol(property)!;
            Assert.AreEqual("Bar", Core.ToDiagnosticMessageName(propertySymbol));
        }

        // ===== Report =====

        [TestMethod]
        public void Report_WithDebugMessageFlag_CallsInvoke()
        {
            var descriptor = new DiagnosticDescriptor("TEST", "TITLE", "MESSAGE {0}", "CAT", DiagnosticSeverity.Warning, true);
            var location = Location.None;
            var args = new object[] { "ARG" };
            var called = false;
            Core.Report(d => called = true, descriptor, location, args);
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Report_WithNullArgs_CallsInvoke()
        {
            var descriptor = new DiagnosticDescriptor("TEST", "TITLE", "MESSAGE", "CAT", DiagnosticSeverity.Warning, true);
            var called = false;
            Core.Report(d => called = true, descriptor, Location.None, null);
            Assert.IsTrue(called);
        }

        // ===== ReportDebugMessage =====

        [TestMethod]
        public void ReportDebugMessage_Symbol_CallsInvoke()
        {
            var comp = CreateCompilation("class C { int x; }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var symbol = model.GetDeclaredSymbol(field.Declaration.Variables[0])!;
            var called = false;
            Core.ReportDebugMessage(d => called = true, symbol, Location.None);
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_Operation_CallsInvoke()
        {
            var comp = CreateCompilation("class C { void M() { int x = 1; } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            var op = model.GetOperation(local.Declaration.Variables[0].Initializer.Value)!;
            var called = false;
            Core.ReportDebugMessage(d => called = true, op);
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_SyntaxNode_CallsInvoke()
        {
            var tree = CSharpSyntaxTree.ParseText("class C { void M() { int x = 1; } }");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            var called = false;
            Core.ReportDebugMessage(d => called = true, local);
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_SyntaxNodeWithMultipleChildren_CallsInvoke()
        {
            var tree = CSharpSyntaxTree.ParseText("class C { int x, y; }");
            var field = FindFirst<FieldDeclarationSyntax>(tree.GetRoot());
            var called = false;
            Core.ReportDebugMessage(d => called = true, field.Declaration);
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_Title_CallsInvoke()
        {
            var called = false;
#pragma warning disable CS0612
            Core.ReportDebugMessage(d => called = true, "TITLE", Location.None, "MSG");
#pragma warning restore
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_TitleAndLocations_CallsInvoke()
        {
            var called = false;
            Core.ReportDebugMessage(d => called = true, "TITLE", new[] { Location.None }, "MSG");
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_NullMessages_CallsInvoke()
        {
            var called = false;
            Core.ReportDebugMessage(d => called = true, "TITLE", new[] { Location.None }, null);
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_EmptyLocations_DoesNotCallInvoke()
        {
            var called = false;
            Core.ReportDebugMessage(d => called = true, "TITLE", Array.Empty<Location>(), "MSG");
            Assert.IsFalse(called);
        }

        [TestMethod]
        public void ReportDebugMessage_LocationsNull_DoesNotCallInvoke()
        {
            var called = false;
            Core.ReportDebugMessage(d => called = true, "TITLE", (IEnumerable<Location>)null!, "MSG");
            Assert.IsFalse(called);
        }

        // ===== IsSuppressedByComment(IOperation) =====

        [TestMethod]
        public void IsSuppressedByComment_Operation_DiscardAssignment_ReturnsTrue()
        {
            var source = @"
class C {
    void M() {
        // suppress
        _ = 1;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var assignment = FindFirst<AssignmentExpressionSyntax>(tree.GetRoot());
            var op = model.GetOperation(assignment)!;
            // op is ISimpleAssignmentOperation, but we need to pass the expression on the right to trigger the logic in IsSuppressedByComment(IOperation)
            var rightOp = ((ISimpleAssignmentOperation)op).Value;
            Assert.IsTrue(Core.IsSuppressedByComment(rightOp, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_Operation_VariableInitializer_ReturnsTrue()
        {
            var source = @"
class C {
    void M() {
        // suppress
        var x = 1;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            var op = model.GetOperation(local.Declaration.Variables[0].Initializer.Value)!;
            Assert.IsTrue(Core.IsSuppressedByComment(op, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_Operation_VariableInitializer_NotSuppressed_ReturnsFalse()
        {
            var source = @"
class C {
    void M() {
        var x = 1;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            var op = model.GetOperation(local.Declaration.Variables[0].Initializer.Value)!;
            Assert.IsFalse(Core.IsSuppressedByComment(op, "// suppress"));
        }

        [TestMethod]
        public void IsSuppressedByComment_MultiLineComment_ReturnsFalse()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
class C {
    void M() {
        /* suppress */
        var x = 1;
    }
}");
            var local = FindFirst<LocalDeclarationStatementSyntax>(tree.GetRoot());
            Assert.IsFalse(Core.IsSuppressedByComment(local, "// suppress"));
        }

        // ===== NormalizeTextWithEllipsis =====

        [TestMethod]
        public void NormalizeTextWithEllipsis_LongString_Truncates()
        {
            var longString = new string('a', 100);
            var method = typeof(Core).GetMethod("NormalizeTextWithEllipsis", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var result = (string)method!.Invoke(null, new object[] { longString })!;
            Assert.AreEqual(new string('a', 72) + "...", result);
        }

        [TestMethod]
        public void NormalizeTextWithEllipsis_Null_ReturnsDefault()
        {
            var method = typeof(Core).GetMethod("NormalizeTextWithEllipsis", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var result = (string)method!.Invoke(null, new object[] { null })!;
            Assert.AreEqual("<NULL TEXT>", result);
        }

        [TestMethod]
        public void NormalizeTextWithEllipsis_ShortString_ReturnsSame()
        {
            var shortString = "abc";
            var method = typeof(Core).GetMethod("NormalizeTextWithEllipsis", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var result = (string)method!.Invoke(null, new object[] { shortString })!;
            Assert.AreEqual("abc", result);
        }

        // ===== ReportDebugMessage (IOperation) additional =====

        [TestMethod]
        public void ReportDebugMessage_OperationWithGrandParent_CallsInvoke()
        {
            var source = @"
class C {
    void M() {
        int x = 1 + 2 + 3;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var literal = FindFirst<LiteralExpressionSyntax>(tree.GetRoot());
            var op = model.GetOperation(literal)!;
            var called = false;
            Core.ReportDebugMessage(d => called = true, op);
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_Obsolete_TitleMessageLocation_CallsInvoke()
        {
            var called = false;
#pragma warning disable CS0612
            Core.ReportDebugMessage(d => called = true, "TITLE", "MESSAGE", Location.None);
#pragma warning restore
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_Obsolete_TitleMessageLocations_CallsInvoke()
        {
            var called = false;
#pragma warning disable CS0612
            Core.ReportDebugMessage(d => called = true, "TITLE", "MESSAGE", new[] { Location.None });
#pragma warning restore
#if STMG_DEBUG_MESSAGE
            Assert.IsTrue(called);
#else
            Assert.IsFalse(called);
#endif
        }
    }
}
