// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace SatorImaging.MeticulousAnalyzer.CodeFixes.Providers
{
    internal static class LinqCodeFixHelper
    {
        // Microsoft.CodeAnalysis.CSharp 3.8.0 has no named SyntaxKind for file-scoped namespaces.
        private const int FileScopedNamespaceDeclarationRawKind = 8845;

        internal static string GetTopLevelNamespaceName(SyntaxNode node)
        {
            NameSyntax? outermostName = null;
            for (var current = node.Parent; current != null; current = current.Parent)
            {
                if (current is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    outermostName = namespaceDeclaration.Name;
                }
                else if (current.RawKind == FileScopedNamespaceDeclarationRawKind)
                {
                    foreach (var child in current.ChildNodes())
                    {
                        if (child is NameSyntax name)
                        {
                            outermostName = name;
                            break;
                        }
                    }
                }
            }

            return outermostName == null ? string.Empty : GetTopLevelName(outermostName);
        }

        internal static string GetTopLevelNamespaceName(string namespaceName)
        {
            var separator = namespaceName.IndexOf('.');
            return separator < 0 ? namespaceName : namespaceName.Substring(0, separator);
        }

        internal static string GetTopLevelName(NameSyntax name)
        {
            while (name is QualifiedNameSyntax qualifiedName)
            {
                name = qualifiedName.Left;
            }

            return name is IdentifierNameSyntax identifierName
                ? identifierName.Identifier.ValueText
                : string.Empty;
        }

        internal static Document? FindDocument(Project project, string fileName)
        {
            foreach (var document in project.Documents)
            {
                if (document.Name == fileName)
                {
                    return document;
                }
            }
            return null;
        }

        internal static ClassDeclarationSyntax? FindClass(SyntaxNode root, string className)
        {
            foreach (var node in root.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax declaration
                    && declaration.Identifier.ValueText == className)
                {
                    return declaration;
                }
            }
            return null;
        }

        internal static string GenerateEmptyFile(string className, string namespaceName)
        {
            var source = new StringBuilder(capacity: 8192);
            source.Append(LinqOperationCodeFixProvider.GeneratedFileHeader);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                source.AppendLine($"namespace {namespaceName}");
                source.AppendLine("{");
            }

            var indent = string.IsNullOrEmpty(namespaceName) ? string.Empty : "    ";
            source.AppendLine($"{indent}internal static partial class {className}");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}}}");
            if (!string.IsNullOrEmpty(namespaceName)) source.AppendLine("}");
            return source.ToString().Replace("\r\n", "\n");
        }

        internal static bool AreMethodsEquivalent(IMethodSymbol left, IMethodSymbol right)
        {
            if (left.Name != right.Name
                || left.TypeParameters.Length != right.TypeParameters.Length
                || left.Parameters.Length != right.Parameters.Length)
            {
                return false;
            }

            for (int i = 0, count = left.Parameters.Length; i < count; i++)
            {
                if (left.Parameters[i].RefKind != right.Parameters[i].RefKind
                    || !AreParameterTypesEquivalent(left.Parameters[i].Type, right.Parameters[i].Type))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AreParameterTypesEquivalent(ITypeSymbol left, ITypeSymbol right)
        {
            if (left is ITypeParameterSymbol leftTypeParameter
                && right is ITypeParameterSymbol rightTypeParameter)
            {
                if (leftTypeParameter.TypeParameterKind != rightTypeParameter.TypeParameterKind
                    || leftTypeParameter.Ordinal != rightTypeParameter.Ordinal)
                {
                    return false;
                }

                return leftTypeParameter.TypeParameterKind == TypeParameterKind.Method
                    || SymbolEqualityComparer.Default.Equals(
                        leftTypeParameter.ContainingSymbol,
                        rightTypeParameter.ContainingSymbol);
            }

            if (left is INamedTypeSymbol leftNamedType
                && right is INamedTypeSymbol rightNamedType)
            {
                if (!SymbolEqualityComparer.Default.Equals(
                        leftNamedType.OriginalDefinition,
                        rightNamedType.OriginalDefinition)
                    || leftNamedType.TypeArguments.Length != rightNamedType.TypeArguments.Length)
                {
                    return false;
                }

                for (int i = 0, count = leftNamedType.TypeArguments.Length; i < count; i++)
                {
                    if (!AreParameterTypesEquivalent(
                            leftNamedType.TypeArguments[i],
                            rightNamedType.TypeArguments[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (left is IArrayTypeSymbol leftArrayType
                && right is IArrayTypeSymbol rightArrayType)
            {
                return leftArrayType.Rank == rightArrayType.Rank
                    && AreParameterTypesEquivalent(leftArrayType.ElementType, rightArrayType.ElementType);
            }

            if (left is IPointerTypeSymbol leftPointerType
                && right is IPointerTypeSymbol rightPointerType)
            {
                return AreParameterTypesEquivalent(leftPointerType.PointedAtType, rightPointerType.PointedAtType);
            }

            return SymbolEqualityComparer.Default.Equals(left, right);
        }
    }
}
