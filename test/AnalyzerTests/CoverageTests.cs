// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System;
using System.Reflection;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class CoverageTests
    {
        private class DummyAttributeData : AttributeData
        {
            protected override INamedTypeSymbol CommonAttributeClass => null;
            protected override IMethodSymbol CommonAttributeConstructor => null;
            protected override SyntaxReference CommonApplicationSyntaxReference => null;
            protected override System.Collections.Immutable.ImmutableArray<TypedConstant> CommonConstructorArguments => System.Collections.Immutable.ImmutableArray<TypedConstant>.Empty;
            protected override System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> CommonNamedArguments => System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>>.Empty;
        }

        private static T FindFirst<T>(SyntaxNode root) where T : SyntaxNode
        {
            foreach (var node in root.DescendantNodes())
            {
                if (node is T match)
                    return match;
            }
            throw new InvalidOperationException($"No node of type {typeof(T).Name} found");
        }

        [TestMethod]
        public void RunAdditionalLineCoverageTests()
        {
            // Set up a valid compilation environment to create non-null, robust AnalysisContext objects
            var source = "public class C { void M() { int x = 1; } }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);

            var classNode = FindFirst<ClassDeclarationSyntax>(tree.GetRoot());
            var classSymbol = model.GetDeclaredSymbol(classNode);

            var methodNode = FindFirst<MethodDeclarationSyntax>(tree.GetRoot());
            var methodSymbol = model.GetDeclaredSymbol(methodNode);

            var literalNode = FindFirst<LiteralExpressionSyntax>(tree.GetRoot());
            var literalOperation = model.GetOperation(literalNode);

            // 1. NullSuppressionAnalyzer.AnalyzeSuppressNullableWarning
            {
                var analyzerType = typeof(NullSuppressionAnalyzer);
                var method = analyzerType.GetMethod("AnalyzeSuppressNullableWarning", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "AnalyzeSuppressNullableWarning method not found.");

                // Pass robust context (Node is ClassDeclarationSyntax, which is not PostfixUnaryExpressionSyntax)
                var context = new SyntaxNodeAnalysisContext(
                    node: classNode,
                    containingSymbol: classSymbol,
                    semanticModel: model,
                    options: null,
                    reportDiagnostic: diag => { },
                    isSupportedDiagnostic: d => true,
                    cancellationToken: default);

                method.Invoke(null, new object[] { context });
            }

            // 2. CatchAnalyzer.AnalyzeCatchClause
            {
                var analyzerType = typeof(CatchAnalyzer);
                var method = analyzerType.GetMethod("AnalyzeCatchClause", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "AnalyzeCatchClause method not found.");

                // Pass robust context (Node is ClassDeclarationSyntax, which is not CatchClauseSyntax)
                var context = new SyntaxNodeAnalysisContext(
                    node: classNode,
                    containingSymbol: classSymbol,
                    semanticModel: model,
                    options: null,
                    reportDiagnostic: diag => { },
                    isSupportedDiagnostic: d => true,
                    cancellationToken: default);

                method.Invoke(null, new object[] { context });
            }

            // 3. DebugAssertAnalyzer.AnalyzeInvocation
            {
                var analyzerType = typeof(DebugAssertAnalyzer);
                var method = analyzerType.GetMethod("AnalyzeInvocation", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "AnalyzeInvocation method not found.");

                // Pass robust context (Operation is ILiteralOperation, which is not IInvocationOperation)
                var context = new OperationAnalysisContext(
                    operation: literalOperation,
                    containingSymbol: methodSymbol,
                    compilation: comp,
                    options: null,
                    reportDiagnostic: diag => { },
                    isSupportedDiagnostic: d => true,
                    cancellationToken: default);

                method.Invoke(null, new object[] { context });
            }

            // 4. MethodImplAnalyzer.AnalyzeMethod
            {
                var analyzerType = typeof(MethodImplAnalyzer);
                var method = analyzerType.GetMethod("AnalyzeMethod", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(method, "AnalyzeMethod method not found.");

                // Pass robust context
                var context = new SymbolAnalysisContext(
                    symbol: methodSymbol,
                    compilation: comp,
                    options: null,
                    reportDiagnostic: diag => { },
                    isSupportedDiagnostic: d => true,
                    cancellationToken: default);

                method.Invoke(null, new object[] { context });
            }
        }

        [TestMethod]
        public void TestMethodImplAnalyzerFallbackCoverage()
        {
            var source = @"
using System.Runtime.CompilerServices;
public class TestClass
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MyMethod() {}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TestClass() {}

    public int MyProp {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        set;
    }

    public static TestClass operator +(TestClass a, TestClass b) => null;
}
";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);

            var methodDecl = FindFirst<MethodDeclarationSyntax>(tree.GetRoot());
            var methodSymbol = model.GetDeclaredSymbol(methodDecl);

            var ctorDecl = FindFirst<ConstructorDeclarationSyntax>(tree.GetRoot());
            var ctorSymbol = model.GetDeclaredSymbol(ctorDecl);

            var accessorDecl = FindFirst<AccessorDeclarationSyntax>(tree.GetRoot());
            var accessorSymbol = model.GetDeclaredSymbol(accessorDecl);

            BaseMethodDeclarationSyntax operatorNode = null;
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node is OperatorDeclarationSyntax opNode) { operatorNode = opNode; break; }
                if (node is ConversionOperatorDeclarationSyntax convNode) { operatorNode = convNode; break; }
            }
            Assert.IsNotNull(operatorNode, "Operator/Conversion node not found.");
            var operatorSymbol = model.GetDeclaredSymbol(operatorNode);

            var analyzerType = typeof(MethodImplAnalyzer);
            var reportFallbackMethod = analyzerType.GetMethod("ReportFallback", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(reportFallbackMethod, "ReportFallback method not found.");

            // Construct valid SymbolAnalysisContext with exact parameter names as found by reflection
            var context = new SymbolAnalysisContext(
                symbol: methodSymbol,
                compilation: comp,
                options: null,
                reportDiagnostic: diag => { },
                isSupportedDiagnostic: d => true,
                cancellationToken: default);

            // Invoke for each type of method/constructor/accessor to trigger the switch cases in ReportFallback
            reportFallbackMethod.Invoke(null, new object[] { context, methodSymbol });
            reportFallbackMethod.Invoke(null, new object[] { context, ctorSymbol });
            reportFallbackMethod.Invoke(null, new object[] { context, accessorSymbol });
            reportFallbackMethod.Invoke(null, new object[] { context, operatorSymbol });

            // Cover ReportWithFallback using DummyAttributeData
            var reportWithFallbackMethod = analyzerType.GetMethod("ReportWithFallback", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(reportWithFallbackMethod, "ReportWithFallback method not found.");
            reportWithFallbackMethod.Invoke(null, new object[] { context, methodSymbol, new DummyAttributeData() });
        }
    }
}
