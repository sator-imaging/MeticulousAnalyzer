// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule_SystemReflectionUsage);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                AnalyzeTypeSyntaxDeclaration,
                SyntaxKind.FieldDeclaration,
                SyntaxKind.EventFieldDeclaration,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.LocalFunctionStatement,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.Parameter);

            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);

            context.RegisterOperationAction(
                AnalyzeTypeOperand,
                OperationKind.DefaultValue,
                OperationKind.TypeOf,
                OperationKind.IsType,
                OperationKind.Conversion,
                OperationKind.TypeParameterObjectCreation,
                OperationKind.ArrayCreation);

            context.RegisterOperationAction(
                AnalyzeSymbolMember,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.EventReference,
                OperationKind.MethodReference,
                OperationKind.Invocation);

            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzePattern, OperationKind.IsPattern);
        }

        private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not UsingDirectiveSyntax usingDirective || usingDirective.Name == null)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(usingDirective.Name).Symbol is not INamespaceSymbol namespaceSymbol)
            {
                return;
            }

            if (!IsSystemReflectionNamespace(namespaceSymbol))
            {
                return;
            }

            ReportDiagnostic(context.ReportDiagnostic, GetRightmostIdentifierLocation(usingDirective.Name), namespaceSymbol);
        }

        private static void AnalyzeTypeSyntaxDeclaration(SyntaxNodeAnalysisContext context)
        {
            TypeSyntax? typeSyntax = context.Node switch
            {
                BaseFieldDeclarationSyntax field => field.Declaration.Type,
                PropertyDeclarationSyntax property => property.Type,
                MethodDeclarationSyntax method => method.ReturnType,
                LocalFunctionStatementSyntax localFunction => localFunction.ReturnType,
                DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.ReturnType,
                ParameterSyntax parameter => parameter.Type,
                _ => null,
            };

            if (typeSyntax == null)
            {
                return;
            }

            var typeSymbol = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
            if (!InvolvesSystemReflection(typeSymbol))
            {
                return;
            }

            ReportDiagnostic(context.ReportDiagnostic, GetTypeSyntaxIdentifierLocation(typeSyntax), typeSymbol!);
        }

        private static void AnalyzeTypeOperand(OperationAnalysisContext context)
        {
            var type = TryGetNamedTypeFromOperation(context.Operation);
            if (type != null && InvolvesSystemReflection(type))
            {
                ReportDiagnostic(context.ReportDiagnostic, GetTypeSyntaxIdentifierLocation(context.Operation.Syntax), type);
            }
        }

        private static void AnalyzeSymbolMember(OperationAnalysisContext context)
        {
            var operation = context.Operation;

            switch (operation)
            {
                case IInvocationOperation invocation:
                    if (InvolvesSystemReflection(invocation.TargetMethod.ReturnType))
                    {
                        ReportDiagnostic(context.ReportDiagnostic, GetMemberNameLocation(invocation.Syntax), invocation.TargetMethod.ReturnType!);
                    }

                    return;

                case IMethodReferenceOperation methodReference:
                    if (InvolvesSystemReflection(methodReference.Method.ReturnType))
                    {
                        ReportDiagnostic(context.ReportDiagnostic, GetMemberNameLocation(methodReference.Syntax), methodReference.Method.ReturnType!);
                    }

                    return;

                case IFieldReferenceOperation fieldReference:
                    if (IsDeclaredInSystemReflection(fieldReference.Field) || InvolvesSystemReflection(fieldReference.Field.Type))
                    {
                        ReportDiagnostic(context.ReportDiagnostic, GetMemberNameLocation(fieldReference.Syntax), fieldReference.Field);
                    }

                    return;

                case IPropertyReferenceOperation propertyReference:
                    if (IsDeclaredInSystemReflection(propertyReference.Property) || InvolvesSystemReflection(propertyReference.Property.Type))
                    {
                        ReportDiagnostic(context.ReportDiagnostic, GetMemberNameLocation(propertyReference.Syntax), propertyReference.Property);
                    }

                    return;

                case IEventReferenceOperation eventReference:
                    if (IsDeclaredInSystemReflection(eventReference.Event) || InvolvesSystemReflection(eventReference.Event.Type))
                    {
                        ReportDiagnostic(context.ReportDiagnostic, GetMemberNameLocation(eventReference.Syntax), eventReference.Event);
                    }

                    return;
            }
        }

        private static void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            if (context.Operation is not IObjectCreationOperation objectCreation)
            {
                return;
            }

            if (!InvolvesSystemReflection(objectCreation.Type))
            {
                return;
            }

            ReportDiagnostic(context.ReportDiagnostic, GetTypeSyntaxIdentifierLocation(objectCreation.Syntax), objectCreation.Type!);
        }

        private static void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declarator)
            {
                return;
            }

            if (!InvolvesSystemReflection(declarator.Symbol.Type))
            {
                return;
            }

            if (declarator.Syntax is not VariableDeclaratorSyntax syntax)
            {
                return;
            }

            if (syntax.Parent is VariableDeclarationSyntax declaration && !declaration.Type.IsVar)
            {
                ReportDiagnostic(context.ReportDiagnostic, GetTypeSyntaxIdentifierLocation(declaration.Type), declarator.Symbol.Type);
                return;
            }

            ReportDiagnostic(context.ReportDiagnostic, syntax.Identifier.GetLocation(), declarator.Symbol.Type);
        }

        private static void AnalyzePattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IIsPatternOperation isPattern)
            {
                return;
            }

            var type = GetPatternTypeSymbol(isPattern.Pattern);
            if (type != null && InvolvesSystemReflection(type))
            {
                ReportDiagnostic(context.ReportDiagnostic, GetTypeSyntaxIdentifierLocation(isPattern.Pattern.Syntax), type);
            }
        }

        private static INamedTypeSymbol? TryGetNamedTypeFromOperation(IOperation operation) =>
            operation switch
            {
                IDefaultValueOperation defaultValue => defaultValue.Type as INamedTypeSymbol,
                ITypeOfOperation typeOf => typeOf.TypeOperand as INamedTypeSymbol,
                IIsTypeOperation isType => isType.TypeOperand as INamedTypeSymbol,
                IConversionOperation conversion when InvolvesSystemReflection(conversion.Type) =>
                    conversion.Type as INamedTypeSymbol,
                ITypeParameterObjectCreationOperation typeParamCreation =>
                    typeParamCreation.Type is INamedTypeSymbol namedType && namedType.TypeKind != TypeKind.TypeParameter
                        ? namedType
                        : null,
                IArrayCreationOperation arrayCreation => TryGetArrayElementNamedType(arrayCreation),
                _ => null,
            };

        private static INamedTypeSymbol? TryGetArrayElementNamedType(IArrayCreationOperation arrayCreation)
        {
            var type = arrayCreation.Type;
            while (type is IArrayTypeSymbol arrayType)
            {
                type = arrayType.ElementType;
            }

            return type as INamedTypeSymbol;
        }

        private static INamedTypeSymbol? GetPatternTypeSymbol(IPatternOperation pattern) =>
            pattern switch
            {
                ITypePatternOperation typePattern => typePattern.MatchedType as INamedTypeSymbol,
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType as INamedTypeSymbol,
                IRecursivePatternOperation recursivePattern => recursivePattern.MatchedType as INamedTypeSymbol,
                INegatedPatternOperation negated => GetPatternTypeSymbol(negated.Pattern),
                IBinaryPatternOperation binary =>
                    GetPatternTypeSymbol(binary.LeftPattern) ?? GetPatternTypeSymbol(binary.RightPattern),
                _ => null,
            };

        private static bool InvolvesSystemReflection(ITypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            type = type.WithNullableAnnotation(NullableAnnotation.None);

            if (type is IArrayTypeSymbol arrayType)
            {
                return InvolvesSystemReflection(arrayType.ElementType);
            }

            if (type is IPointerTypeSymbol pointerType)
            {
                return InvolvesSystemReflection(pointerType.PointedAtType);
            }

            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
            {
                return InvolvesSystemReflection(nullableType.TypeArguments[0]);
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (IsDeclaredInSystemReflection(namedType))
                {
                    return true;
                }

                foreach (var typeArgument in namedType.TypeArguments)
                {
                    if (InvolvesSystemReflection(typeArgument))
                    {
                        return true;
                    }
                }

                return false;
            }

            return IsDeclaredInSystemReflection(type);
        }

        private static bool IsDeclaredInSystemReflection(ISymbol symbol) =>
            symbol.ContainingNamespace != null && IsSystemReflectionNamespace(symbol.ContainingNamespace);

        private static bool IsSystemReflectionNamespace(INamespaceSymbol namespaceSymbol) =>
            namespaceSymbol.Name == "Reflection"
            && namespaceSymbol.ContainingNamespace is INamespaceSymbol
            {
                Name: "System",
                ContainingNamespace.IsGlobalNamespace: true,
            };

        private static void ReportDiagnostic(System.Action<Diagnostic> reportDiagnostic, Location location, ISymbol symbol) =>
            ReportDiagnostic(reportDiagnostic, location, symbol.ToDiagnosticMessageName());

        private static void ReportDiagnostic(System.Action<Diagnostic> reportDiagnostic, Location location, string displayName)
        {
            if (location == null)
            {
                return;
            }

            reportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionUsage,
                location,
                displayName));
        }

        private static Location GetMemberNameLocation(SyntaxNode syntax) =>
            syntax switch
            {
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } =>
                    memberAccess.Name.Identifier.GetLocation(),
                MemberAccessExpressionSyntax memberAccess =>
                    memberAccess.Name.Identifier.GetLocation(),
                IdentifierNameSyntax identifier =>
                    identifier.Identifier.GetLocation(),
                _ => syntax.GetLocation(),
            };

        private static Location GetTypeSyntaxIdentifierLocation(SyntaxNode syntax) =>
            syntax switch
            {
                TypeOfExpressionSyntax typeOf => GetTypeSyntaxIdentifierLocation(typeOf.Type),
                DefaultExpressionSyntax defaultExpression => GetTypeSyntaxIdentifierLocation(defaultExpression.Type),
                CastExpressionSyntax cast => GetTypeSyntaxIdentifierLocation(cast.Type),
                DeclarationExpressionSyntax declaration => GetTypeSyntaxIdentifierLocation(declaration.Type),
                VariableDeclarationSyntax variable => GetTypeSyntaxIdentifierLocation(variable.Type),
                ObjectCreationExpressionSyntax objectCreation => GetTypeSyntaxIdentifierLocation(objectCreation.Type),
                ArrayCreationExpressionSyntax { Type: not null } arrayCreation =>
                    GetTypeSyntaxIdentifierLocation(arrayCreation.Type),
                NullableTypeSyntax nullable => GetTypeSyntaxIdentifierLocation(nullable.ElementType),
                ArrayTypeSyntax arrayType => GetTypeSyntaxIdentifierLocation(arrayType.ElementType),
                TypeSyntax typeSyntax => GetRightmostIdentifierLocation(typeSyntax),
                _ => syntax.GetLocation(),
            };

        private static Location GetRightmostIdentifierLocation(NameSyntax name) =>
            name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.GetLocation(),
                QualifiedNameSyntax qualified => GetRightmostIdentifierLocation(qualified.Right),
                AliasQualifiedNameSyntax alias => GetRightmostIdentifierLocation(alias.Name),
                GenericNameSyntax generic => generic.Identifier.GetLocation(),
                _ => name.GetLocation(),
            };
    }
}
