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
    public class SMA8003_DebugAssertAnalyzerTests_UnexpectedOperation
    {
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
        public void SMA8003_Compliant_Invocation_UnexpectedOperation()
        {
            var source = "public class C { void M() { int x = 1; } }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);

            var methodNode = FindFirst<MethodDeclarationSyntax>(tree.GetRoot());
            var methodSymbol = model.GetDeclaredSymbol(methodNode);

            var literalNode = FindFirst<LiteralExpressionSyntax>(tree.GetRoot());
            var literalOperation = model.GetOperation(literalNode);

            var analyzerType = typeof(DebugAssertAnalyzer);
            var method = analyzerType.GetMethod("AnalyzeInvocation", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "AnalyzeInvocation method not found.");

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
    }
}
