// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System;
using System.Reflection;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8002_NullSuppressionAnalyzerTests_UnexpectedSyntaxNode
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
        public void SMA8002_Compliant_SuppressNullableWarning_UnexpectedSyntaxNode()
        {
            var source = "public class C {}";
            var tree = CSharpSyntaxTree.ParseText(source);
            var comp = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = comp.GetSemanticModel(tree);

            var classNode = FindFirst<ClassDeclarationSyntax>(tree.GetRoot());
            var classSymbol = model.GetDeclaredSymbol(classNode);

            var analyzerType = typeof(NullSuppressionAnalyzer);
            var method = analyzerType.GetMethod("AnalyzeSuppressNullableWarning", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "AnalyzeSuppressNullableWarning method not found.");

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
    }
}
