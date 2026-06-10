// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReflectionAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_SystemReflectionUsage = "SMA7003";

        private static readonly DiagnosticDescriptor Rule_SystemReflectionUsage = new(
            RuleId_SystemReflectionUsage,
            new LocalizableResourceString(nameof(Resources.SMA7003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA7003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA7003_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule_SystemReflectionUsage);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Runtime usage via IOperation.
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
            context.RegisterOperationAction(AnalyzeMethodReference, OperationKind.MethodReference);
            context.RegisterOperationAction(AnalyzeTypeOf, OperationKind.TypeOf);
            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);

            // Declaration-site type references have no IOperation.
            // NOTE: parameter types are reported via the containing method symbol to avoid duplicate diagnostics.
            context.RegisterSymbolAction(
                AnalyzeDeclarationSymbol,
                SymbolKind.Field,
                SymbolKind.Property,
                SymbolKind.Method,
                SymbolKind.Event);
        }


        /*  operation  ================================================================ */

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
            {
                return;
            }

            var found = FindReflectionType(invocation.TargetMethod.ReturnType)
                ?? GetReflectionReceiverType(invocation.Instance, invocation.TargetMethod);
            if (found == null)
            {
                return;
            }

            if (!TryGetMemberNameLocation(invocation.Syntax, out var location, out var name))
            {
                return;
            }

            Report(context, location, name, found);
        }

        private static void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IPropertyReferenceOperation propertyReference)
            {
                return;
            }

            if (IsStaticTypeQualifiedAccess(propertyReference.Instance, propertyReference.Property.ContainingType))
            {
                if (TryGetTypeQualifierLocation(propertyReference.Syntax, out var qualifierLocation, out var qualifierName))
                {
                    Report(context, qualifierLocation, qualifierName, propertyReference.Property.ContainingType);
                }

                return;
            }

            var found = FindReflectionType(propertyReference.Type)
                ?? GetReflectionReceiverType(propertyReference.Instance, propertyReference.Property);
            if (found == null)
            {
                return;
            }

            if (!TryGetMemberNameLocation(propertyReference.Syntax, out var location, out var name))
            {
                return;
            }

            Report(context, location, name, found);
        }

        private static void AnalyzeFieldReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IFieldReferenceOperation fieldReference)
            {
                return;
            }

            if (fieldReference.Parent is IInvocationOperation)
            {
                return;
            }

            if (IsStaticTypeQualifiedAccess(fieldReference.Instance, fieldReference.Field.ContainingType))
            {
                if (TryGetTypeQualifierLocation(fieldReference.Syntax, out var qualifierLocation, out var qualifierName))
                {
                    Report(context, qualifierLocation, qualifierName, fieldReference.Field.ContainingType);
                }

                return;
            }

            var found = FindReflectionType(fieldReference.Type)
                ?? GetReflectionReceiverType(fieldReference.Instance, fieldReference.Field);
            if (found == null)
            {
                return;
            }

            if (!TryGetMemberNameLocation(fieldReference.Syntax, out var location, out var name))
            {
                return;
            }

            Report(context, location, name, found);
        }

        private static void AnalyzeMethodReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IMethodReferenceOperation methodReference)
            {
                return;
            }

            if (methodReference.Parent is IInvocationOperation)
            {
                return;
            }

            if (IsStaticTypeQualifiedAccess(methodReference.Instance, methodReference.Method.ContainingType))
            {
                if (TryGetTypeQualifierLocation(methodReference.Syntax, out var qualifierLocation, out var qualifierName))
                {
                    Report(context, qualifierLocation, qualifierName, methodReference.Method.ContainingType);
                }

                return;
            }

            var found = FindReflectionType(methodReference.Method.ReturnType)
                ?? GetReflectionReceiverType(methodReference.Instance, methodReference.Method);
            if (found == null)
            {
                return;
            }

            if (!TryGetMemberNameLocation(methodReference.Syntax, out var location, out var name))
            {
                return;
            }

            Report(context, location, name, found);
        }

        private static void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            if (context.Operation is not ITypeOfOperation typeOf)
            {
                return;
            }

            if (typeOf.TypeOperand is not INamedTypeSymbol type || !IsReflectionType(type))
            {
                return;
            }

            if (typeOf.Syntax is not TypeOfExpressionSyntax typeOfSyntax)
            {
                return;
            }

            ReportReflectionTypeNamesInTypeSyntax(context, typeOfSyntax.Type, typeOf.SemanticModel);
        }

        private static void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declarator)
            {
                return;
            }

            var semanticModel = declarator.SemanticModel ?? context.Compilation.GetSemanticModel(declarator.Syntax.SyntaxTree);

            if (declarator.Syntax is ForEachStatementSyntax forEach)
            {
                if (!forEach.Type.IsVar)
                {
                    ReportReflectionTypeNamesInTypeSyntax(context, forEach.Type, semanticModel);
                    return;
                }

                var foreachFound = FindReflectionType(declarator.Symbol.Type);
                if (foreachFound != null)
                {
                    Report(context, forEach.Identifier.GetLocation(), forEach.Identifier.Text, foreachFound);
                }

                return;
            }

            if (declarator.Syntax is not VariableDeclaratorSyntax variableSyntax)
            {
                return;
            }

            if (variableSyntax.Parent is VariableDeclarationSyntax declaration)
            {
                if (!declaration.Type.IsVar)
                {
                    ReportReflectionTypeNamesInTypeSyntax(context, declaration.Type, semanticModel);
                    return;
                }
            }

            var found = FindReflectionType(declarator.Symbol.Type);
            if (found == null)
            {
                return;
            }

            Report(context, variableSyntax.Identifier.GetLocation(), variableSyntax.Identifier.Text, found);
        }


        /*  symbol  ================================================================ */

        private static void AnalyzeDeclarationSymbol(SymbolAnalysisContext context)
        {
            foreach (var syntaxRef in context.Symbol.DeclaringSyntaxReferences)
            {
                var root = syntaxRef.GetSyntax();
                var semanticModel = context.Compilation.GetSemanticModel(root.SyntaxTree);

                foreach (var typeSyntax in ExtractDeclarationTypeSyntaxes(root, context.Symbol))
                {
                    ReportReflectionTypeNamesInTypeSyntax(context, typeSyntax, semanticModel);
                }
            }
        }


        /*  helper  ================================================================ */

        private static void Report(OperationAnalysisContext context, Location location, string identifier, INamedTypeSymbol reflectionType)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionUsage,
                location,
                identifier,
                reflectionType.ToDisplayString()));
        }

        private static void Report(SymbolAnalysisContext context, Location location, string identifier, INamedTypeSymbol reflectionType)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionUsage,
                location,
                identifier,
                reflectionType.ToDisplayString()));
        }

        private static void ReportReflectionTypeNamesInTypeSyntax(
            OperationAnalysisContext context,
            TypeSyntax typeSyntax,
            SemanticModel? semanticModel)
        {
            if (semanticModel == null)
            {
                return;
            }

            foreach (var name in typeSyntax.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
            {
                if (name.IsVar)
                {
                    continue;
                }

                if (semanticModel.GetSymbolInfo(name).Symbol is not INamedTypeSymbol type || !IsReflectionType(type))
                {
                    continue;
                }

                Report(context, name.Identifier.GetLocation(), name.Identifier.Text, type);
            }
        }

        private static void ReportReflectionTypeNamesInTypeSyntax(
            SymbolAnalysisContext context,
            TypeSyntax typeSyntax,
            SemanticModel semanticModel)
        {
            foreach (var name in typeSyntax.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
            {
                if (name.IsVar)
                {
                    continue;
                }

                if (semanticModel.GetSymbolInfo(name).Symbol is not INamedTypeSymbol type || !IsReflectionType(type))
                {
                    continue;
                }

                Report(context, name.Identifier.GetLocation(), name.Identifier.Text, type);
            }
        }

        private static IEnumerable<TypeSyntax> ExtractDeclarationTypeSyntaxes(SyntaxNode root, ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol when root is VariableDeclaratorSyntax fieldVariable
                    && fieldVariable.Parent is VariableDeclarationSyntax fieldDeclaration:
                    yield return fieldDeclaration.Type;
                    break;

                case IPropertySymbol when root is PropertyDeclarationSyntax property:
                    yield return property.Type;
                    break;

                case IMethodSymbol when root is MethodDeclarationSyntax method:
                    yield return method.ReturnType;
                    foreach (var parameter in method.ParameterList.Parameters)
                    {
                        yield return parameter.Type;
                    }
                    break;

                case IMethodSymbol when root is LocalFunctionStatementSyntax localFunction:
                    yield return localFunction.ReturnType;
                    foreach (var parameter in localFunction.ParameterList.Parameters)
                    {
                        yield return parameter.Type;
                    }
                    break;

                case IEventSymbol when root is EventDeclarationSyntax eventDeclaration:
                    yield return eventDeclaration.Type;
                    break;

                case IEventSymbol when root is EventFieldDeclarationSyntax eventField:
                    yield return eventField.Declaration.Type;
                    break;
            }
        }

        private static bool TryGetMemberNameLocation(SyntaxNode syntax, out Location location, out string name)
        {
            switch (syntax)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    location = memberAccess.Name.Identifier.GetLocation();
                    name = memberAccess.Name.Identifier.Text;
                    return true;

                case MemberBindingExpressionSyntax memberBinding:
                    location = memberBinding.Name.Identifier.GetLocation();
                    name = memberBinding.Name.Identifier.Text;
                    return true;

                case InvocationExpressionSyntax invocation:
                    return TryGetMemberNameLocation(invocation.Expression, out location, out name);

                case IdentifierNameSyntax identifier:
                    location = identifier.Identifier.GetLocation();
                    name = identifier.Identifier.Text;
                    return true;

                default:
                    location = syntax.GetLocation();
                    name = syntax.ToString();
                    return true;
            }
        }

        private static bool TryGetTypeQualifierLocation(SyntaxNode syntax, out Location location, out string name)
        {
            switch (syntax)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    if (memberAccess.Expression is SimpleNameSyntax qualifier)
                    {
                        location = qualifier.Identifier.GetLocation();
                        name = qualifier.Identifier.Text;
                        return true;
                    }
                    break;

                case IdentifierNameSyntax identifier:
                    location = identifier.Identifier.GetLocation();
                    name = identifier.Identifier.Text;
                    return true;
            }

            location = syntax.GetLocation();
            name = syntax.ToString();
            return false;
        }

        // NOTE: member declared on reflection type can be inherited by non-reflection type
        //       (e.g. `System.Type` derives from `MemberInfo`) so check receiver type
        //       instead of member's containing type.
        private static INamedTypeSymbol? GetReflectionReceiverType(IOperation? instance, ISymbol member)
        {
            var receiverType = instance?.Type ?? member.ContainingType;

            return receiverType is INamedTypeSymbol named && IsReflectionType(named) ? named : null;
        }

        private static bool IsStaticTypeQualifiedAccess(IOperation? instance, INamedTypeSymbol containingType)
        {
            return instance == null && IsReflectionType(containingType);
        }

        // NOTE: depth limit for pathological recursive generics.
        private const int MaxTypeSearchDepth = 8;

        private static INamedTypeSymbol? FindReflectionType(ITypeSymbol? type, int depth = 0)
        {
            if (type == null || depth > MaxTypeSearchDepth)
            {
                return null;
            }

            switch (type)
            {
                case IArrayTypeSymbol array:
                    return FindReflectionType(array.ElementType, depth + 1);

                case INamedTypeSymbol named:
                    if (IsReflectionType(named))
                    {
                        return named;
                    }

                    foreach (var typeArg in named.TypeArguments)
                    {
                        var found = FindReflectionType(typeArg, depth + 1);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                    return null;

                default:
                    return null;
            }
        }

        private static bool IsReflectionType(INamedTypeSymbol type)
        {
            // attribute types (e.g. ObfuscationAttribute, AssemblyVersionAttribute) are exempt.
            return IsSystemReflectionNamespace(type.ContainingNamespace)
                && !IsAttributeType(type);
        }

        private static bool IsSystemReflectionNamespace(INamespaceSymbol? ns)
        {
            // also match sub-namespaces (e.g. System.Reflection.Emit)
            while (ns is { IsGlobalNamespace: false })
            {
                if (ns is
                    {
                        Name: nameof(System.Reflection), ContainingNamespace:
                        {
                            Name: nameof(System), ContainingNamespace:
                            {
                                IsGlobalNamespace: true,
                            }
                        }
                    })
                {
                    return true;
                }

                ns = ns.ContainingNamespace;
            }

            return false;
        }

        private static bool IsAttributeType(INamedTypeSymbol type)
        {
            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (baseType is
                    {
                        Name: nameof(System.Attribute), ContainingNamespace:
                        {
                            Name: nameof(System), ContainingNamespace:
                            {
                                IsGlobalNamespace: true,
                            }
                        }
                    })
                {
                    return true;
                }
            }

            return false;
        }
    }
}
