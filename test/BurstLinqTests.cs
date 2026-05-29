// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class BurstLinqTests
    {
        #region Helpers

        private static SyntaxList<MemberDeclarationSyntax> GetClassMembers(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var classDecl = (ClassDeclarationSyntax)root.Members[0];
            return classDecl.Members;
        }

        private static SeparatedSyntaxList<ParameterSyntax> GetMethodParameters(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var classDecl = (ClassDeclarationSyntax)root.Members[0];
            var method = (MethodDeclarationSyntax)classDecl.Members[0];
            return method.ParameterList.Parameters;
        }

        private static ImmutableArray<ISymbol> GetSymbolsFromCompilation(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var builder = ImmutableArray.CreateBuilder<ISymbol>();
            foreach (var member in root.DescendantNodes())
            {
                var symbol = model.GetDeclaredSymbol(member);
                if (symbol != null)
                {
                    builder.Add(symbol);
                }
            }
            return builder.ToImmutableArray();
        }

        private static IEnumerable<T> AsEnumerableOnly<T>(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }

        private class DisposalTrackingEnumerable : IEnumerable<object>
        {
            private readonly object[] items;
            public bool WasDisposed { get; private set; }

            public DisposalTrackingEnumerable(params object[] items)
            {
                this.items = items;
            }

            public IEnumerator<object> GetEnumerator() => new TrackingEnumerator(this);
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            private class TrackingEnumerator : IEnumerator<object>
            {
                private readonly DisposalTrackingEnumerable parent;
                private int index = -1;
                public TrackingEnumerator(DisposalTrackingEnumerable parent) { this.parent = parent; }
                public object Current => parent.items[index];
                public bool MoveNext() { index++; return index < parent.items.Length; }
                public void Reset() { index = -1; }
                public void Dispose() { parent.WasDisposed = true; }
            }
        }

        #endregion

        #region ElementAtOrDefault

        [TestMethod]
        public void ElementAtOrDefault_ReturnsItem_ValidIndexOnList()
        {
            List<int> list = new() { 10, 20, 30 };
            Assert.AreEqual(20, list.ElementAtOrDefault(1));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsItem_ValidIndexOnArray()
        {
            int[] arr = new[] { 5, 15, 25 };
            Assert.AreEqual(25, arr.ElementAtOrDefault(2));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsItem_ValidIndexOnImmutableArray()
        {
            var imm = ImmutableArray.Create(100, 200, 300);
            Assert.AreEqual(100, imm.ElementAtOrDefault(0));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsDefault_NegativeIndex()
        {
            List<int> list = new() { 1, 2, 3 };
            Assert.AreEqual(0, list.ElementAtOrDefault(-1));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsDefault_IndexExceedsCount()
        {
            List<string> list = new() { "a", "b" };
            Assert.IsNull(list.ElementAtOrDefault(5));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsDefault_EmptyCollection()
        {
            List<int> list = new();
            Assert.AreEqual(0, list.ElementAtOrDefault(0));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsItem_PureEnumerableSlowPath()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 7, 8, 9 });
            Assert.AreEqual(9, source.ElementAtOrDefault(2));
        }

        [TestMethod]
        public void ElementAtOrDefault_ReturnsDefault_PureEnumerableIndexOutOfRange()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 7, 8 });
            Assert.AreEqual(0, source.ElementAtOrDefault(5));
        }

        #endregion

        #region Where (ImmutableArray)

        [TestMethod]
        public void Where_ImmutableArray_ReturnsMatches_PredicateTrue()
        {
            var source = ImmutableArray.Create(1, 2, 3, 4, 5);
            var results = new List<int>();
            foreach (var item in source.Where(x => x > 3))
            {
                results.Add(item);
            }
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(4, results[0]);
            Assert.AreEqual(5, results[1]);
        }

        [TestMethod]
        public void Where_ImmutableArray_ReturnsEmpty_NoMatch()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            var results = new List<int>();
            foreach (var item in source.Where(x => x > 100))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Where (IReadOnlyList)

        [TestMethod]
        public void Where_IReadOnlyList_ReturnsMatches_PredicateTrue()
        {
            IReadOnlyList<int> source = new List<int> { 10, 20, 30, 40 };
            var results = new List<int>();
            foreach (var item in source.Where(x => x >= 30))
            {
                results.Add(item);
            }
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(30, results[0]);
            Assert.AreEqual(40, results[1]);
        }

        [TestMethod]
        public void Where_IReadOnlyList_ReturnsEmpty_NoMatch()
        {
            IReadOnlyList<int> source = new List<int> { 1, 2, 3 };
            var results = new List<int>();
            foreach (var item in source.Where(x => x > 10))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Where (SyntaxList)

        [TestMethod]
        public void Where_SyntaxList_ReturnsMatches_FilteredByKind()
        {
            string code = "class C { void M() {} int X; }";
            var members = GetClassMembers(code);
            var results = new List<MemberDeclarationSyntax>();
            foreach (var item in members.Where(m => m is MethodDeclarationSyntax))
            {
                results.Add(item);
            }
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0] is MethodDeclarationSyntax);
        }

        [TestMethod]
        public void Where_SyntaxList_ReturnsEmpty_NoMatchingKind()
        {
            string code = "class C { int X; int Y; }";
            var members = GetClassMembers(code);
            var results = new List<MemberDeclarationSyntax>();
            foreach (var item in members.Where(m => m is MethodDeclarationSyntax))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Where (SeparatedSyntaxList)

        [TestMethod]
        public void Where_SeparatedSyntaxList_ReturnsMatches_FilteredByName()
        {
            string code = "class C { void M(int a, string b, int c) {} }";
            var parameters = GetMethodParameters(code);
            var results = new List<ParameterSyntax>();
            foreach (var item in parameters.Where(p => p.Type.ToString() == "int"))
            {
                results.Add(item);
            }
            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public void Where_SeparatedSyntaxList_ReturnsEmpty_NoMatch()
        {
            string code = "class C { void M(int a, int b) {} }";
            var parameters = GetMethodParameters(code);
            var results = new List<ParameterSyntax>();
            foreach (var item in parameters.Where(p => p.Type.ToString() == "double"))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Where (IEnumerable fallback)

        [TestMethod]
        public void Where_IEnumerable_ReturnsMatches_FallbackToLinq()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3, 4 });
            var result = source.Where(x => x % 2 == 0);
            var results = new List<int>();
            foreach (var item in result)
            {
                results.Add(item);
            }
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(2, results[0]);
            Assert.AreEqual(4, results[1]);
        }

        [TestMethod]
        public void Where_IEnumerable_ReturnsEmpty_NoMatch()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3 });
            var result = source.Where(x => x > 100);
            var results = new List<int>();
            foreach (var item in result)
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Linq_Where ToImmutableArray

        [TestMethod]
        public void LinqWhere_ToImmutableArray_ReturnsFiltered_MatchingItems()
        {
            var source = ImmutableArray.Create(1, 2, 3, 4, 5);
            var filtered = source.Where(x => x % 2 == 0).ToImmutableArray();
            Assert.AreEqual(2, filtered.Length);
            Assert.AreEqual(2, filtered[0]);
            Assert.AreEqual(4, filtered[1]);
        }

        [TestMethod]
        public void LinqWhere_ToImmutableArray_ReturnsEmpty_NoMatch()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            var filtered = source.Where(x => x > 10).ToImmutableArray();
            Assert.AreEqual(0, filtered.Length);
        }

        #endregion

        #region Linq_Where iterator comprehensive

        [TestMethod]
        public void LinqWhere_Iterator_IteratesCorrectly_MultipleEnumerations()
        {
            var source = ImmutableArray.Create(10, 20, 30);
            var query = source.Where(x => x >= 20);

            var first = new List<int>();
            foreach (var item in query)
            {
                first.Add(item);
            }

            var second = new List<int>();
            foreach (var item in query)
            {
                second.Add(item);
            }

            Assert.AreEqual(2, first.Count);
            Assert.AreEqual(2, second.Count);
            Assert.AreEqual(20, first[0]);
            Assert.AreEqual(30, first[1]);
        }

        [TestMethod]
        public void LinqWhere_Iterator_ReturnsEmpty_EmptySource()
        {
            var source = ImmutableArray<int>.Empty;
            var results = new List<int>();
            foreach (var item in source.Where(x => true))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Where_Any (ImmutableArray)

        [TestMethod]
        public void WhereAny_ImmutableArray_ReturnsTrue_MatchExists()
        {
            var source = ImmutableArray.Create(1, 2, 3, 4);
            Assert.IsTrue(source.Where_Any(x => x == 3));
        }

        [TestMethod]
        public void WhereAny_ImmutableArray_ReturnsFalse_NoMatch()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            Assert.IsFalse(source.Where_Any(x => x > 100));
        }

        [TestMethod]
        public void WhereAny_ImmutableArray_ReturnsFalse_EmptyArray()
        {
            var source = ImmutableArray<int>.Empty;
            Assert.IsFalse(source.Where_Any(x => true));
        }

        [TestMethod]
        public void WhereAny_ImmutableArray_ReturnsFalse_DefaultArray()
        {
            var source = default(ImmutableArray<int>);
            Assert.IsFalse(source.Where_Any(x => true));
        }

        #endregion

        #region Where_Any (Linq_OfType_Where)

        [TestMethod]
        public void WhereAny_LinqOfTypeWhere_ReturnsFalse_EmptySource()
        {
            var symbols = ImmutableArray.Create<ISymbol>();
            var query = symbols.OfType_Where<ISymbol>(s => true);
            Assert.IsFalse(query.Where_Any(s => true));
        }

        [TestMethod]
        public void WhereAny_LinqOfTypeWhere_ReturnsTrue_MatchExists()
        {
            var symbols = GetSymbolsFromCompilation("class Foo { int field1; void Method1() {} } class Bar { }");
            var query = symbols.OfType_Where<INamedTypeSymbol>(s => true);
            Assert.IsTrue(query.Where_Any(s => s.Name == "Foo"));
        }

        [TestMethod]
        public void WhereAny_LinqOfTypeWhere_ReturnsFalse_PredicateNeverTrue()
        {
            var symbols = GetSymbolsFromCompilation("class Foo { int field1; void Method1() {} } class Bar { }");
            var query = symbols.OfType_Where<INamedTypeSymbol>(s => true);
            Assert.IsFalse(query.Where_Any(s => s.Name == "NonExistent"));
        }

        #endregion

        #region OfType_FirstOrDefault

        [TestMethod]
        public void OfTypeFirstOrDefault_ReturnsMatch_TypeExists()
        {
            List<object> source = new() { "hello", 42, "world" };
            var result = source.OfType_FirstOrDefault<int>();
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void OfTypeFirstOrDefault_ReturnsDefault_TypeAbsent()
        {
            List<object> source = new() { "hello", "world" };
            var result = source.OfType_FirstOrDefault<int>();
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void OfTypeFirstOrDefault_ReturnsNull_ReferenceTypeAbsent()
        {
            List<object> source = new() { 1, 2, 3 };
            string result = source.OfType_FirstOrDefault<string>();
            Assert.IsNull(result);
        }

        #endregion

        #region OfType_Any

        [TestMethod]
        public void OfTypeAny_ReturnsTrue_TypeExists()
        {
            List<object> source = new() { "text", 42, 3.14 };
            Assert.IsTrue(source.OfType_Any<int>());
        }

        [TestMethod]
        public void OfTypeAny_ReturnsFalse_TypeAbsent()
        {
            List<object> source = new() { "text", 3.14 };
            Assert.IsFalse(source.OfType_Any<int>());
        }

        [TestMethod]
        public void OfTypeAny_ReturnsFalse_EmptyCollection()
        {
            List<object> source = new();
            Assert.IsFalse(source.OfType_Any<int>());
        }

        #endregion

        #region OfType (Linq_OfType<T> struct iterator)

        [TestMethod]
        public void OfType_ReturnsMatchingItems_MixedTypes()
        {
            List<object> source = new() { "a", 1, "b", 2, "c" };
            var results = new List<string>();
            foreach (var item in source.OfType<string>())
            {
                results.Add(item);
            }
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("a", results[0]);
            Assert.AreEqual("b", results[1]);
            Assert.AreEqual("c", results[2]);
        }

        [TestMethod]
        public void OfType_ReturnsEmpty_NoMatchingType()
        {
            List<object> source = new() { "a", "b" };
            var results = new List<int>();
            foreach (var item in source.OfType<int>())
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void OfType_ReturnsEmpty_EmptyCollectionOptimization()
        {
            List<object> source = new();
            var results = new List<int>();
            foreach (var item in source.OfType<int>())
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Linq_OfType<T> iterator comprehensive

        [TestMethod]
        public void LinqOfType_Iterator_IteratesCorrectly_MultipleForeach()
        {
            List<object> source = new() { 1, "two", 3, "four" };
            var query = source.OfType<int>();

            var first = new List<int>();
            foreach (var item in query)
            {
                first.Add(item);
            }

            var second = new List<int>();
            foreach (var item in query)
            {
                second.Add(item);
            }

            Assert.AreEqual(2, first.Count);
            Assert.AreEqual(2, second.Count);
            Assert.AreEqual(1, first[0]);
            Assert.AreEqual(3, first[1]);
        }

        [TestMethod]
        public void LinqOfType_Iterator_ReturnsAll_SingleType()
        {
            List<object> source = new() { 10, 20, 30 };
            var results = new List<int>();
            foreach (var item in source.OfType<int>())
            {
                results.Add(item);
            }
            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public void LinqOfType_Iterator_DisposesEnumerator_AfterIteration()
        {
            var source = new DisposalTrackingEnumerable("a", 1, "b", 2);
            Assert.IsFalse(source.WasDisposed);
            var results = new List<string>();
            foreach (var item in source.OfType<string>())
            {
                results.Add(item);
            }
            Assert.IsTrue(source.WasDisposed);
            Assert.AreEqual(2, results.Count);
        }

        #endregion

        #region OfType_Where

        [TestMethod]
        public void OfTypeWhere_ReturnsMatches_TypeAndPredicateMatch()
        {
            var symbols = GetSymbolsFromCompilation("class Foo { int field1; void Method1() {} } class Bar { }");
            var results = new List<INamedTypeSymbol>();
            foreach (var item in symbols.OfType_Where<INamedTypeSymbol>(s => s.Name == "Foo"))
            {
                results.Add(item);
            }
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Foo", results[0].Name);
        }

        [TestMethod]
        public void OfTypeWhere_ReturnsEmpty_PredicateNeverTrue()
        {
            var symbols = GetSymbolsFromCompilation("class Foo { int field1; void Method1() {} } class Bar { }");
            var results = new List<INamedTypeSymbol>();
            foreach (var item in symbols.OfType_Where<INamedTypeSymbol>(s => s.Name == "NonExistent"))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void OfTypeWhere_ReturnsEmpty_EmptySource()
        {
            var source = ImmutableArray<ISymbol>.Empty;
            var results = new List<ISymbol>();
            foreach (var item in source.OfType_Where<ISymbol>(s => true))
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region Linq_OfType_Where struct iterator

        [TestMethod]
        public void LinqOfTypeWhere_Iterator_ReturnsEmpty_NoTypeMatch()
        {
            var source = ImmutableArray<ISymbol>.Empty;
            var query = source.OfType_Where<ISymbol>(s => true);
            var results = new List<ISymbol>();
            foreach (var item in query)
            {
                results.Add(item);
            }
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void LinqOfTypeWhere_Iterator_IteratesCorrectly_MultipleForeach()
        {
            var symbols = GetSymbolsFromCompilation("class Foo { } class Bar { } class Baz { }");
            var query = symbols.OfType_Where<INamedTypeSymbol>(s => s.Name != "Baz");

            var first = new List<string>();
            foreach (var item in query)
            {
                first.Add(item.Name);
            }

            var second = new List<string>();
            foreach (var item in query)
            {
                second.Add(item.Name);
            }

            Assert.AreEqual(first.Count, second.Count);
            Assert.IsTrue(first.Count > 0);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i], second[i]);
            }
        }

        #endregion

        #region ToArray

        [TestMethod]
        public void ToArray_ReturnsArray_FromListAsEnumerable()
        {
            IEnumerable<int> source = new List<int> { 1, 2, 3 };
            int[] result = source.ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }

        [TestMethod]
        public void ToArray_ReturnsArray_FromArrayAsEnumerable()
        {
            IEnumerable<int> source = new int[] { 10, 20 };
            int[] result = source.ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(10, result[0]);
            Assert.AreEqual(20, result[1]);
        }

        [TestMethod]
        public void ToArray_ReturnsEmptyArray_EmptyReadOnlyCollection()
        {
            IEnumerable<int> source = new List<int>();
            int[] result = source.ToArray();
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ToArray_ReturnsArray_PureEnumerableSlowPath()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 5, 6, 7 });
            int[] result = source.ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(5, result[0]);
            Assert.AreEqual(6, result[1]);
            Assert.AreEqual(7, result[2]);
        }

        [TestMethod]
        public void ToArray_ReturnsEmptyArray_PureEnumerableEmpty()
        {
            IEnumerable<int> source = AsEnumerableOnly(Array.Empty<int>());
            int[] result = source.ToArray();
            Assert.AreEqual(0, result.Length);
        }

        #endregion

        #region FirstOrDefault (Linq_OfType<T>)

        [TestMethod]
        public void FirstOrDefault_LinqOfType_ReturnsFirst_TypeExists()
        {
            List<object> source = new() { "a", 42, "b" };
            var query = source.OfType<int>();
            var result = query.FirstOrDefault();
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void FirstOrDefault_LinqOfType_ReturnsDefault_TypeAbsent()
        {
            List<object> source = new() { "a", "b" };
            var query = source.OfType<int>();
            var result = query.FirstOrDefault();
            Assert.AreEqual(0, result);
        }

        #endregion

        #region FirstOrDefault (ImmutableArray)

        [TestMethod]
        public void FirstOrDefault_ImmutableArray_ReturnsFirst_NonEmpty()
        {
            var source = ImmutableArray.Create(10, 20, 30);
            Assert.AreEqual(10, source.FirstOrDefault());
        }

        [TestMethod]
        public void FirstOrDefault_ImmutableArray_ReturnsDefault_Empty()
        {
            var source = ImmutableArray<int>.Empty;
            Assert.AreEqual(0, source.FirstOrDefault());
        }

        [TestMethod]
        public void FirstOrDefault_ImmutableArray_ReturnsDefault_DefaultArray()
        {
            var source = default(ImmutableArray<int>);
            Assert.AreEqual(0, source.FirstOrDefault());
        }

        #endregion

        #region FirstOrDefault (IEnumerable)

        [TestMethod]
        public void FirstOrDefault_IEnumerable_ReturnsFirst_NonEmpty()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 5, 6, 7 });
            Assert.AreEqual(5, source.FirstOrDefault());
        }

        [TestMethod]
        public void FirstOrDefault_IEnumerable_ReturnsDefault_Empty()
        {
            IEnumerable<int> source = AsEnumerableOnly(Array.Empty<int>());
            Assert.AreEqual(0, source.FirstOrDefault());
        }

        #endregion

        #region FirstOrDefault (ImmutableArray + predicate)

        [TestMethod]
        public void FirstOrDefault_ImmutableArrayPredicate_ReturnsMatch_PredicateTrue()
        {
            var source = ImmutableArray.Create(1, 2, 3, 4);
            Assert.AreEqual(3, source.FirstOrDefault(x => x > 2));
        }

        [TestMethod]
        public void FirstOrDefault_ImmutableArrayPredicate_ReturnsDefault_NoMatch()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            Assert.AreEqual(0, source.FirstOrDefault(x => x > 100));
        }

        [TestMethod]
        public void FirstOrDefault_ImmutableArrayPredicate_ReturnsDefault_Empty()
        {
            var source = ImmutableArray<int>.Empty;
            Assert.AreEqual(0, source.FirstOrDefault(x => true));
        }

        [TestMethod]
        public void FirstOrDefault_ImmutableArrayPredicate_ReturnsDefault_DefaultArray()
        {
            var source = default(ImmutableArray<int>);
            Assert.AreEqual(0, source.FirstOrDefault(x => true));
        }

        #endregion

        #region FirstOrDefault (IEnumerable + predicate)

        [TestMethod]
        public void FirstOrDefault_IEnumerablePredicate_ReturnsMatch_PredicateTrue()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3, 4 });
            Assert.AreEqual(3, source.FirstOrDefault(x => x > 2));
        }

        [TestMethod]
        public void FirstOrDefault_IEnumerablePredicate_ReturnsDefault_NoMatch()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3 });
            Assert.AreEqual(0, source.FirstOrDefault(x => x > 100));
        }

        #endregion

        #region Any (SyntaxList)

        [TestMethod]
        public void Any_SyntaxList_ReturnsTrue_MatchExists()
        {
            string code = "class C { void M() {} int X; }";
            var members = GetClassMembers(code);
            Assert.IsTrue(members.Any(m => m is MethodDeclarationSyntax));
        }

        [TestMethod]
        public void Any_SyntaxList_ReturnsFalse_NoMatch()
        {
            string code = "class C { int X; int Y; }";
            var members = GetClassMembers(code);
            Assert.IsFalse(members.Any(m => m is MethodDeclarationSyntax));
        }

        #endregion

        #region Any (ImmutableArray)

        [TestMethod]
        public void Any_ImmutableArray_ReturnsTrue_MatchExists()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            Assert.IsTrue(source.Any(x => x == 2));
        }

        [TestMethod]
        public void Any_ImmutableArray_ReturnsFalse_NoMatch()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            Assert.IsFalse(source.Any(x => x > 100));
        }

        [TestMethod]
        public void Any_ImmutableArray_ReturnsFalse_Empty()
        {
            var source = ImmutableArray<int>.Empty;
            Assert.IsFalse(source.Any(x => true));
        }

        [TestMethod]
        public void Any_ImmutableArray_ReturnsFalse_DefaultArray()
        {
            var source = default(ImmutableArray<int>);
            Assert.IsFalse(source.Any(x => true));
        }

        #endregion

        #region Any (IEnumerable)

        [TestMethod]
        public void Any_IEnumerable_ReturnsTrue_MatchExists()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3 });
            Assert.IsTrue(source.Any(x => x == 2));
        }

        [TestMethod]
        public void Any_IEnumerable_ReturnsFalse_NoMatch()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3 });
            Assert.IsFalse(source.Any(x => x > 100));
        }

        #endregion

        #region Contains

        [TestMethod]
        public void Contains_ReturnsTrue_ValueFound()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3 });
            Assert.IsTrue(source.Contains(2));
        }

        [TestMethod]
        public void Contains_ReturnsFalse_ValueAbsent()
        {
            IEnumerable<int> source = AsEnumerableOnly(new[] { 1, 2, 3 });
            Assert.IsFalse(source.Contains(99));
        }

        [TestMethod]
        public void Contains_ReturnsFalse_EmptyCollection()
        {
            IEnumerable<int> source = AsEnumerableOnly(Array.Empty<int>());
            Assert.IsFalse(source.Contains(1));
        }

        [TestMethod]
        public void Contains_ReturnsTrue_StringValue()
        {
            IEnumerable<string> source = AsEnumerableOnly(new[] { "hello", "world" });
            Assert.IsTrue(source.Contains("world"));
        }

        #endregion

        #region First (ImmutableArray)

        [TestMethod]
        public void First_ImmutableArray_ReturnsFirstElement_NonEmpty()
        {
            var source = ImmutableArray.Create(42, 99, 7);
            Assert.AreEqual(42, source.First());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void First_ImmutableArray_Throws_EmptyArray()
        {
            var source = ImmutableArray<int>.Empty;
            source.First();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void First_ImmutableArray_Throws_DefaultArray()
        {
            var source = default(ImmutableArray<int>);
            source.First();
        }

        #endregion

        #region SelectMany_FirstOrDefault

        [TestMethod]
        public void SelectManyFirstOrDefault_ReturnsMatch_NestedItemFound()
        {
            string code = "class C { void M(int target, string other) {} void N(double x) {} }";
            var members = GetClassMembers(code);
            var result = members.SelectMany_FirstOrDefault(
                m => m is MethodDeclarationSyntax method
                    ? method.ParameterList.Parameters
                    : default,
                p => p.Identifier.Text == "target"
            );
            Assert.IsNotNull(result);
            Assert.AreEqual("target", result.Identifier.Text);
        }

        [TestMethod]
        public void SelectManyFirstOrDefault_ReturnsNull_NoNestedMatch()
        {
            string code = "class C { void M(int a, string b) {} }";
            var members = GetClassMembers(code);
            var result = members.SelectMany_FirstOrDefault(
                m => m is MethodDeclarationSyntax method
                    ? method.ParameterList.Parameters
                    : default,
                p => p.Identifier.Text == "nonexistent"
            );
            Assert.IsNull(result);
        }

        #endregion
    }
}
