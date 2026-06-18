using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string code = @"
using System.Collections.Generic;
class C {
    void M(Dictionary<string, int> dict) {
        if (dict.TryGetValue(""key"", out var _)) {}
        var (_, b) = (1, 2);
        foreach (var _ in new int[0]) {}
    }
}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        var root = tree.GetRoot();
        foreach (var node in root.DescendantNodes())
        {
            if (node is IdentifierNameSyntax id && id.IsVar) {
                var type = model.GetTypeInfo(id);
                Console.WriteLine($"IdentifierName var, Parent: {id.Parent?.Kind()}, Type: {type.Type}, ConvertedType: {type.ConvertedType}");
            }
            if (node is DiscardDesignationSyntax d) {
                var symbol = model.GetDeclaredSymbol(d);
                var symbolInfo = model.GetSymbolInfo(d).Symbol;
                var typeInfo = model.GetTypeInfo(d);
                var operation = model.GetOperation(d);
                Console.WriteLine($"DiscardDesignation: {d}, Parent: {d.Parent?.Kind()}, DeclaredSymbol: {symbol?.GetType().Name}, SymbolInfo: {symbolInfo?.GetType().Name}, Type: {typeInfo.Type}, ConvertedType: {typeInfo.ConvertedType}, Operation: {operation?.GetType().Name}, OpType: {(operation as IDiscardOperation)?.Type}");
            }
        }
    }
}
