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

namespace SatorImaging.StaticMemberAnalyzer.Tests
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

        [TestMethod]
        public void ToDiagnosticMessageName_GlobalNamespace_ReturnsGlobal()
        {
            var comp = CreateCompilation("class C { }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var classDecl = FindFirst<ClassDeclarationSyntax>(tree.GetRoot());
            var ns = model.GetDeclaredSymbol(classDecl)!.ContainingNamespace;
            Assert.AreEqual("global", Core.ToDiagnosticMessageName(ns));
        }

        [TestMethod]
        public void ToDiagnosticMessageName_Namespace_ReturnsFullPathNotLeafOnly()
        {
            var comp = CreateCompilation("namespace Foo.Bar { class C { } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var classDecl = FindFirst<ClassDeclarationSyntax>(tree.GetRoot());
            var ns = model.GetDeclaredSymbol(classDecl)!.ContainingNamespace;
            Assert.AreEqual("Foo.Bar", Core.ToDiagnosticMessageName(ns));
            Assert.AreEqual("Bar", ns.Name);
            Assert.AreNotEqual(ns.Name, Core.ToDiagnosticMessageName(ns));
        }

        // ===== Additional Coverage Tests =====

        [TestMethod]
        public void IsKnownImmutableType_Additional_Coverage()
        {
            var source = "class C { System.Exception e; System.Uri u; System.Collections.IEnumerable i; }";
            var comp = CreateCompilation(source);
            var model = comp.GetSemanticModel(comp.SyntaxTrees[0]);
            var fieldNodes = new System.Collections.Generic.List<FieldDeclarationSyntax>();
            foreach (var node in comp.SyntaxTrees[0].GetRoot().DescendantNodes())
            {
                if (node is FieldDeclarationSyntax f) fieldNodes.Add(f);
            }

            var typeE = model.GetTypeInfo(fieldNodes[0].Declaration.Type).Type;
            var typeU = model.GetTypeInfo(fieldNodes[1].Declaration.Type).Type;
            var typeI = model.GetTypeInfo(fieldNodes[2].Declaration.Type).Type;

            Assert.IsFalse(Core.IsKnownImmutableType(typeE));
            Assert.IsTrue(Core.IsKnownImmutableType(typeU));
            Assert.IsTrue(Core.IsKnownImmutableType(typeI));
            Assert.IsFalse(Core.IsKnownImmutableType(null));
        }

        [TestMethod]
        public void GetMemberNamePrefix_Additional_Coverage()
        {
            var tree = CSharpSyntaxTree.ParseText("namespace A.B { class C { int x; } }");
            FieldDeclarationSyntax field = null;
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node is FieldDeclarationSyntax f) { field = f; break; }
            }
            var result = Core.GetMemberNamePrefix(field);
            Assert.IsTrue(result.Contains("A.B"));
            Assert.IsTrue(result.Contains("C"));
        }

        [TestMethod]
        public void Report_Coverage()
        {
            var descriptor = new DiagnosticDescriptor("T", "T", "M {0}", "C", DiagnosticSeverity.Warning, true);
            var called = false;
            Core.Report(d => called = true, descriptor, Location.None, new object[] { "arg" });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void ReportDebugMessage_Overloads_Coverage()
        {
            var called = false;
            Action<Diagnostic> reporter = d => called = true;
            var methods = typeof(Core).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            foreach (var m in methods)
            {
                if (m.Name != "ReportDebugMessage") continue;
                var p = m.GetParameters();
                try {
                    if (p.Length == 4) {
                        if (!m.IsGenericMethod) {
                            if (p[2].ParameterType == typeof(Location))
                                m.Invoke(null, new object[] { reporter, "t", Location.None, new string[] { "m" } });
                            else if (p[2].ParameterType == typeof(string))
                                m.Invoke(null, new object[] { reporter, "t", "m", Location.None });
                        } else {
                            var gm = m.MakeGenericMethod(typeof(Location[]));
                            if (p[2].ParameterType.IsGenericParameter)
                                gm.Invoke(null, new object[] { reporter, "t", new Location[] { Location.None }, new string[] { "m" } });
                            else if (p[2].ParameterType == typeof(string))
                                gm.Invoke(null, new object[] { reporter, "t", "m", new Location[] { Location.None } });
                        }
                    }
                } catch {}
            }
#if DEBUG
            Assert.IsTrue(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_Symbol_Coverage()
        {
            var comp = CreateCompilation("class C { int x; }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            FieldDeclarationSyntax field = null;
            foreach (var node in tree.GetRoot().DescendantNodes()) if (node is FieldDeclarationSyntax f) { field = f; break; }
            var symbol = model.GetDeclaredSymbol(field.Declaration.Variables[0])!;
            var called = false;

            var methods = typeof(Core).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var m in methods)
            {
                if (m.Name == "ReportDebugMessage" && m.GetParameters().Length == 5 && m.GetParameters()[1].ParameterType == typeof(ISymbol))
                {
                    m.Invoke(null, new object[] { (Action<Diagnostic>)(d => called = true), symbol, Location.None, "caller", 1 });
                    break;
                }
            }
#if DEBUG
            Assert.IsTrue(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_Operation_Coverage()
        {
            var comp = CreateCompilation("class C { void M() { int x = 1; } }");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            LocalDeclarationStatementSyntax local = null;
            foreach (var node in tree.GetRoot().DescendantNodes()) if (node is LocalDeclarationStatementSyntax l) { local = l; break; }
            var op = model.GetOperation(local.Declaration.Variables[0].Initializer.Value)!;
            var called = false;

            var methods = typeof(Core).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var m in methods)
            {
                if (m.Name == "ReportDebugMessage" && m.GetParameters().Length == 4 && m.GetParameters()[1].ParameterType == typeof(IOperation))
                {
                    m.Invoke(null, new object[] { (Action<Diagnostic>)(d => called = true), op, "caller", 1 });
                }
                if (m.Name == "ReportDebugMessage" && m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(IOperation))
                {
                    m.Invoke(null, new object[] { (Action<Diagnostic>)(d => called = true), op, "caller", 1 });
                }
            }
#if DEBUG
            Assert.IsTrue(called);
#endif
        }

        [TestMethod]
        public void ReportDebugMessage_SyntaxNode_Coverage()
        {
            var tree = CSharpSyntaxTree.ParseText("class C { void M() { int x = 1; } }");
            LocalDeclarationStatementSyntax local = null;
            foreach (var node in tree.GetRoot().DescendantNodes()) if (node is LocalDeclarationStatementSyntax l) { local = l; break; }
            var called = false;

            var methods = typeof(Core).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var m in methods)
            {
                if (m.Name == "ReportDebugMessage" && m.GetParameters().Length == 4 && m.GetParameters()[1].ParameterType == typeof(SyntaxNode))
                {
                    m.Invoke(null, new object[] { (Action<Diagnostic>)(d => called = true), (SyntaxNode)local, "caller", 1 });
                }
                if (m.Name == "ReportDebugMessage" && m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(SyntaxNode))
                {
                    m.Invoke(null, new object[] { (Action<Diagnostic>)(d => called = true), (SyntaxNode)local, "caller", 1 });
                }
            }
#if DEBUG
            Assert.IsTrue(called);
#endif
        }

        [TestMethod]
        public void NormalizeTextWithEllipsis_Coverage()
        {
            var method = typeof(Core).GetMethod("NormalizeTextWithEllipsis", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual("abc", method.Invoke(null, new object[] { "abc" }));
            Assert.AreEqual("<NULL TEXT>", method.Invoke(null, new object[] { null }));
            Assert.AreEqual(new string('a', 72) + "...", method.Invoke(null, new object[] { new string('a', 100) }));
        }

        [TestMethod]
        public void IsSuppressedByComment_Additional_Coverage()
        {
            var source = @"
class C {
    // suppress
    int x = 1;

    void M() {
        // suppress
        _ = 1;

        // suppress
        var y = 2;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();

            FieldDeclarationSyntax field = null;
            AssignmentExpressionSyntax assign = null;
            LocalDeclarationStatementSyntax local = null;

            foreach (var node in root.DescendantNodes())
            {
                if (node is FieldDeclarationSyntax f && field == null) field = f;
                if (node is AssignmentExpressionSyntax a && assign == null) assign = a;
                if (node is LocalDeclarationStatementSyntax l && local == null) local = l;
            }

            Assert.IsTrue(Core.IsSuppressedByComment(field, "// suppress"));
            Assert.IsTrue(Core.IsSuppressedByComment(assign, "// suppress", true));
            Assert.IsFalse(Core.IsSuppressedByComment(assign, "// suppress", false));

            var rightOp = model.GetOperation(assign.Right);
            Assert.IsTrue(Core.IsSuppressedByComment(rightOp, "// suppress"));

            var localOp = model.GetOperation(local.Declaration.Variables[0].Initializer.Value);
            Assert.IsTrue(Core.IsSuppressedByComment(localOp, "// suppress"));
        }

    }
}
