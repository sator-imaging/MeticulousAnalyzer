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

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
            context.RegisterOperationAction(AnalyzeMethodReference, OperationKind.MethodReference);
            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzeForEachLoop, OperationKind.Loop);
            context.RegisterOperationAction(AnalyzeTypeOf, OperationKind.TypeOf);

            // Member declaration types (field, property, method, event) have no IOperation.
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

            ReportIfReflection(
                context,
                TryGetMemberNameLocation(invocation.Syntax, out var location, out var name) ? location : invocation.Syntax.GetLocation(),
                name ?? invocation.TargetMethod.Name,
                FindReflectionType(invocation.TargetMethod.ReturnType)
                    ?? GetReflectionReceiverType(invocation.Instance, invocation.TargetMethod));

            foreach (var argument in invocation.Arguments)
            {
                var value = argument.Value;
                if (value is IFieldReferenceOperation
                    or IPropertyReferenceOperation
                    or IMethodReferenceOperation
                    or IBinaryOperation)
                {
                    continue;
                }

                ReportIfReflection(
                    context,
                    argument.Syntax.GetLocation(),
                    argument.Syntax.ToString(),
                    FindReflectionType(value?.Type));
            }
        }

        private static void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IPropertyReferenceOperation propertyReference)
            {
                return;
            }

            ReportMemberReference(
                context,
                propertyReference.Syntax,
                propertyReference.Instance,
                propertyReference.Property,
                FindReflectionType(propertyReference.Type));
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

            ReportMemberReference(
                context,
                fieldReference.Syntax,
                fieldReference.Instance,
                fieldReference.Field,
                FindReflectionType(fieldReference.Type));
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

            ReportMemberReference(
                context,
                methodReference.Syntax,
                methodReference.Instance,
                methodReference.Method,
                FindReflectionType(methodReference.Method.ReturnType));
        }

        private static void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            if (context.Operation is not ITypeOfOperation typeOf
                || typeOf.Syntax is not TypeOfExpressionSyntax typeOfSyntax)
            {
                return;
            }

            var semanticModel = typeOf.SemanticModel ?? context.Compilation.GetSemanticModel(typeOfSyntax.SyntaxTree);
            ReportReflectionTypeSyntax(context, typeOfSyntax.Type, semanticModel);
        }

        private static void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declarator)
            {
                return;
            }

            var semanticModel = declarator.SemanticModel ?? context.Compilation.GetSemanticModel(declarator.Syntax.SyntaxTree);

            if (declarator.Syntax is not VariableDeclaratorSyntax variableSyntax)
            {
                return;
            }

            if (variableSyntax.Parent is VariableDeclarationSyntax declaration)
            {
                if (!declaration.Type.IsVar)
                {
                    if (declaration.Variables.FirstOrDefault() == variableSyntax)
                    {
                        ReportReflectionTypeSyntax(context, declaration.Type, semanticModel);
                    }

                    return;
                }
            }

            ReportIfReflection(
                context,
                variableSyntax.Identifier.GetLocation(),
                variableSyntax.Identifier.Text,
                FindReflectionType(declarator.Symbol.Type));
        }

        private static void AnalyzeForEachLoop(OperationAnalysisContext context)
        {
            if (context.Operation is not IForEachLoopOperation forEach
                || forEach.Syntax is not ForEachStatementSyntax forEachSyntax)
            {
                return;
            }

            var semanticModel = forEach.SemanticModel ?? context.Compilation.GetSemanticModel(forEachSyntax.SyntaxTree);

            if (!forEachSyntax.Type.IsVar)
            {
                ReportReflectionTypeSyntax(context, forEachSyntax.Type, semanticModel);
                return;
            }

            ITypeSymbol? iterationType = forEach.LoopControlVariable is IVariableDeclaratorOperation declarator
                ? declarator.Symbol.Type
                : semanticModel.GetForEachStatementInfo(forEachSyntax).ElementType;

            ReportIfReflection(
                context,
                forEachSyntax.Identifier.GetLocation(),
                forEachSyntax.Identifier.Text,
                FindReflectionType(iterationType));
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
                    ReportReflectionTypeSyntax(context, typeSyntax, semanticModel);
                }
            }
        }


        /*  helper  ================================================================ */

        private static void ReportMemberReference(
            OperationAnalysisContext context,
            SyntaxNode syntax,
            IOperation? instance,
            ISymbol member,
            INamedTypeSymbol? memberTypeReflection)
        {
            var reflectionType = memberTypeReflection ?? GetReflectionReceiverType(instance, member);
            if (reflectionType == null)
            {
                return;
            }

            if (instance == null && IsReflectionType(member.ContainingType))
            {
                var (qualifierLocation, qualifierName) = GetTypeQualifierLocation(syntax);
                Report(context, qualifierLocation, qualifierName, member.ContainingType);
                return;
            }

            if (!TryGetMemberNameLocation(syntax, out var location, out var name))
            {
                return;
            }

            Report(context, location, name, reflectionType);
        }

        private static void ReportIfReflection(
            OperationAnalysisContext context,
            Location location,
            string name,
            INamedTypeSymbol? reflectionType)
        {
            if (reflectionType == null)
            {
                return;
            }

            Report(context, location, name, reflectionType);
        }

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

        private static void ReportReflectionTypeSyntax(
            OperationAnalysisContext context,
            TypeSyntax? typeSyntax,
            SemanticModel semanticModel)
        {
            ReportReflectionTypeSyntax(
                typeSyntax,
                semanticModel,
                (location, name, type) => Report(context, location, name, type));
        }

        private static void ReportReflectionTypeSyntax(
            SymbolAnalysisContext context,
            TypeSyntax? typeSyntax,
            SemanticModel semanticModel)
        {
            ReportReflectionTypeSyntax(
                typeSyntax,
                semanticModel,
                (location, name, type) => Report(context, location, name, type));
        }

        private static void ReportReflectionTypeSyntax(
            TypeSyntax? typeSyntax,
            SemanticModel semanticModel,
            System.Action<Location, string, INamedTypeSymbol> report)
        {
            if (typeSyntax == null)
            {
                return;
            }

            switch (typeSyntax)
            {
                case IdentifierNameSyntax identifier when !identifier.IsVar:
                    if (semanticModel.GetTypeInfo(identifier).Type is INamedTypeSymbol type && IsReflectionType(type))
                    {
                        report(identifier.Identifier.GetLocation(), identifier.Identifier.Text, type);
                    }
                    break;

                case GenericNameSyntax generic:
                    foreach (var typeArgument in generic.TypeArgumentList.Arguments)
                    {
                        ReportReflectionTypeSyntax(typeArgument, semanticModel, report);
                    }
                    break;

                case ArrayTypeSyntax array:
                    ReportReflectionTypeSyntax(array.ElementType, semanticModel, report);
                    break;

                case QualifiedNameSyntax qualified:
                    ReportReflectionTypeSyntax(qualified.Right, semanticModel, report);
                    break;

                case AliasQualifiedNameSyntax alias:
                    ReportReflectionTypeSyntax(alias.Name, semanticModel, report);
                    break;

                case NullableTypeSyntax nullable:
                    ReportReflectionTypeSyntax(nullable.ElementType, semanticModel, report);
                    break;
            }
        }

        private static IEnumerable<TypeSyntax> ExtractDeclarationTypeSyntaxes(SyntaxNode root, ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol when root is VariableDeclaratorSyntax fieldVariable
                    && fieldVariable.Parent is VariableDeclarationSyntax fieldDeclaration:
                    if (fieldDeclaration.Variables.FirstOrDefault() == fieldVariable)
                    {
                        yield return fieldDeclaration.Type;
                    }
                    break;

                case IPropertySymbol when root is BasePropertyDeclarationSyntax property:
                    yield return property.Type;
                    if (property is IndexerDeclarationSyntax indexer)
                    {
                        foreach (var parameter in indexer.ParameterList.Parameters)
                        {
                            if (parameter.Type != null)
                            {
                                yield return parameter.Type;
                            }
                        }
                    }
                    break;

                case IMethodSymbol when root is BaseMethodDeclarationSyntax baseMethod:
                    if (baseMethod is MethodDeclarationSyntax method)
                    {
                        yield return method.ReturnType;
                    }
                    else if (baseMethod is OperatorDeclarationSyntax op)
                    {
                        yield return op.ReturnType;
                    }
                    else if (baseMethod is ConversionOperatorDeclarationSyntax conv)
                    {
                        yield return conv.Type;
                    }

                    foreach (var parameter in baseMethod.ParameterList.Parameters)
                    {
                        if (parameter.Type != null)
                        {
                            yield return parameter.Type;
                        }
                    }
                    break;

                case IMethodSymbol when root is LocalFunctionStatementSyntax localFunction:
                    yield return localFunction.ReturnType;
                    foreach (var parameter in localFunction.ParameterList.Parameters)
                    {
                        if (parameter.Type != null)
                        {
                            yield return parameter.Type;
                        }
                    }
                    break;

                case IEventSymbol when root is EventDeclarationSyntax eventDeclaration:
                    yield return eventDeclaration.Type;
                    break;

                case IEventSymbol when root is VariableDeclaratorSyntax eventVariable
                    && eventVariable.Parent is VariableDeclarationSyntax eventDeclaration
                    && eventDeclaration.Parent is EventFieldDeclarationSyntax eventField:
                    if (eventDeclaration.Variables.FirstOrDefault() == eventVariable)
                    {
                        yield return eventField.Declaration.Type;
                    }
                    break;
            }
        }

        private static bool TryGetMemberNameLocation(SyntaxNode syntax, out Location location, out string? name)
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

        private static (Location Location, string Name) GetTypeQualifierLocation(SyntaxNode syntax)
        {
            switch (syntax)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    return (memberAccess.Expression.GetLocation(), memberAccess.Expression.ToString());

                case IdentifierNameSyntax identifier:
                    return (identifier.Identifier.GetLocation(), identifier.Identifier.Text);

                default:
                    return (syntax.GetLocation(), syntax.ToString());
            }
        }

        // NOTE: member declared on reflection type can be inherited by non-reflection type
        //       (e.g. `System.Type` derives from `MemberInfo`) so check receiver type
        //       instead of member's containing type.
        private static INamedTypeSymbol? GetReflectionReceiverType(IOperation? instance, ISymbol member)
        {
            var receiverType = instance?.Type ?? member.ContainingType;

            return receiverType is INamedTypeSymbol named && IsReflectionType(named) ? named : null;
        }

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
