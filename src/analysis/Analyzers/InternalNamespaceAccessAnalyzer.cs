// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InternalNamespaceAccessAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId_InternalNamespaceAccess = "SMA0080";

        private static readonly DiagnosticDescriptor Rule_InternalNamespaceAccess = new(
            RuleId_InternalNamespaceAccess,
            new LocalizableResourceString(nameof(Resources.SMA0080_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0080_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0080_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule_InternalNamespaceAccess);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(
                AnalyzeTypeOperand,
                OperationKind.DefaultValue,
                OperationKind.TypeOf,
                OperationKind.IsType,
                OperationKind.Conversion,
                OperationKind.TypeParameterObjectCreation,
                OperationKind.ArrayCreation,
                OperationKind.SizeOf);

            context.RegisterOperationAction(
                AnalyzeSymbolMember,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.EventReference,
                OperationKind.MethodReference,
                OperationKind.Invocation,
                OperationKind.ObjectCreation);

            context.RegisterOperationAction(
                AnalyzePattern,
                OperationKind.BinaryPattern,
                OperationKind.DeclarationPattern,
                OperationKind.NegatedPattern,
                OperationKind.RecursivePattern,
                OperationKind.TypePattern);

            context.RegisterOperationAction(AnalyzeNameOf, OperationKind.NameOf);

            context.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Field,
                SymbolKind.Property);
        }

        private static void AnalyzeTypeOperand(OperationAnalysisContext context)
        {
            var operation = context.Operation;
            if (operation is IConversionOperation conversion
                && conversion.Operand is IDefaultValueOperation)
            {
                return;
            }

            var type = TryGetTypeFromOperation(operation);
            if (type != null)
            {
                ReportCrossNamespaceAccess(context, operation, type);
            }
        }

        private static void AnalyzeSymbolMember(OperationAnalysisContext context)
        {
            var operation = context.Operation;
            var symbol = TryGetSymbolFromMemberOperation(operation);
            if (symbol != null)
            {
                ReportCrossNamespaceAccess(context, operation, symbol);
            }
        }

        private static void AnalyzePattern(OperationAnalysisContext context)
        {
            if (context.Operation is not IPatternOperation pattern)
            {
                return;
            }

            if (pattern.Parent is IPatternOperation)
            {
                return;
            }

            var type = GetPatternTypeSymbol(pattern);
            if (type != null)
            {
                ReportCrossNamespaceAccess(context, pattern, type);
            }
        }

        private static void AnalyzeNameOf(OperationAnalysisContext context)
        {
            if (context.Operation is not INameOfOperation nameOf)
            {
                return;
            }

            var symbol = TryGetNameOfSymbolFromOperation(nameOf);
            if (symbol == null)
            {
                symbol = TryGetNameOfSymbolFromSemanticModel(nameOf);
            }

            if (symbol != null)
            {
                ReportCrossNamespaceAccess(context, nameOf, symbol);
            }
        }

        private static ITypeSymbol? TryGetTypeFromOperation(IOperation operation) =>
            operation switch
            {
                IDefaultValueOperation defaultValue => defaultValue.Type,
                ITypeOfOperation typeOf => typeOf.TypeOperand,
                IIsTypeOperation isType => isType.TypeOperand,
                IConversionOperation conversion => conversion.Type,
                ITypeParameterObjectCreationOperation typeParamCreation => typeParamCreation.Type,
                IArrayCreationOperation arrayCreation => arrayCreation.Type,
                ISizeOfOperation sizeOf => sizeOf.TypeOperand,
                _ => null
            };

        private static ISymbol? TryGetSymbolFromMemberOperation(IOperation operation) =>
            operation switch
            {
                IFieldReferenceOperation fieldRef => fieldRef.Field,
                IPropertyReferenceOperation propertyRef => propertyRef.Property,
                IEventReferenceOperation eventRef => eventRef.Event,
                IMethodReferenceOperation methodRef => methodRef.Method,
                IInvocationOperation invocation => invocation.TargetMethod,
                IObjectCreationOperation objectCreation => (ISymbol?)objectCreation.Constructor ?? objectCreation.Type,
                _ => null
            };

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            switch (context.Symbol)
            {
                case IFieldSymbol field:
                    ReportCrossNamespaceAccess(
                        context,
                        GetFieldTypeLocation(field),
                        field.Type);
                    break;

                case IPropertySymbol property:
                    ReportCrossNamespaceAccess(
                        context,
                        GetPropertyTypeLocation(property),
                        property.Type);
                    break;

                case IMethodSymbol method:
                    ReportCrossNamespaceAccess(
                        context,
                        GetReturnTypeLocation(method),
                        method.ReturnType);
                    foreach (var parameter in method.Parameters)
                    {
                        ReportCrossNamespaceAccess(
                            context,
                            GetParameterTypeLocation(method, parameter),
                            parameter.Type);
                    }

                    break;

                case INamedTypeSymbol namedType:
                    if (namedType.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
                    {
                        ReportCrossNamespaceAccess(
                            context,
                            GetBaseOrInterfaceTypeLocation(namedType, baseType, context.Compilation),
                            baseType);
                    }

                    foreach (var @interface in namedType.Interfaces)
                    {
                        ReportCrossNamespaceAccess(
                            context,
                            GetBaseOrInterfaceTypeLocation(namedType, @interface, context.Compilation),
                            @interface);
                    }

                    break;
            }
        }

        private static Location GetReturnTypeLocation(IMethodSymbol method)
        {
            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is MethodDeclarationSyntax methodDecl && methodDecl.ReturnType != null)
                {
                    return methodDecl.ReturnType.GetLocation();
                }
            }

            return method.Locations[0];
        }

        private static Location GetParameterTypeLocation(IMethodSymbol method, IParameterSymbol parameter)
        {
            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is not MethodDeclarationSyntax methodDecl)
                {
                    continue;
                }

                var index = GetParameterIndex(method, parameter);
                if (index < 0 || index >= methodDecl.ParameterList.Parameters.Count)
                {
                    continue;
                }

                var parameterSyntax = methodDecl.ParameterList.Parameters[index];
                if (parameterSyntax.Type != null)
                {
                    return parameterSyntax.Type.GetLocation();
                }
            }

            return parameter.Locations[0];
        }

        private static int GetParameterIndex(IMethodSymbol method, IParameterSymbol parameter)
        {
            for (var i = 0; i < method.Parameters.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(method.Parameters[i], parameter))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Location GetFieldTypeLocation(IFieldSymbol field)
        {
            foreach (var syntaxRef in field.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is VariableDeclaratorSyntax declarator
                    && declarator.Parent is VariableDeclarationSyntax variableDeclaration)
                {
                    return variableDeclaration.Type.GetLocation();
                }
            }

            return field.Locations[0];
        }

        private static Location GetPropertyTypeLocation(IPropertySymbol property)
        {
            foreach (var syntaxRef in property.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is PropertyDeclarationSyntax propertyDeclaration)
                {
                    return propertyDeclaration.Type.GetLocation();
                }
            }

            return property.Locations[0];
        }

        private static Location GetBaseOrInterfaceTypeLocation(
            INamedTypeSymbol namedType,
            ITypeSymbol typeSymbol,
            Compilation compilation)
        {
            foreach (var syntaxRef in namedType.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is not TypeDeclarationSyntax typeDecl || typeDecl.BaseList == null)
                {
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(typeDecl.SyntaxTree);
                foreach (var baseTypeSyntax in typeDecl.BaseList.Types)
                {
                    var typeInfo = semanticModel.GetTypeInfo(baseTypeSyntax.Type);
                    if (SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeSymbol)
                        || SymbolEqualityComparer.Default.Equals(typeInfo.ConvertedType, typeSymbol))
                    {
                        return baseTypeSyntax.Type.GetLocation();
                    }
                }
            }

            return namedType.Locations[0];
        }

        private static void ReportCrossNamespaceAccess(
            OperationAnalysisContext context,
            IOperation operation,
            ISymbol? symbol)
        {
            var location = operation.Syntax.GetLocation();
            if (location == null)
            {
                return;
            }

            ReportCrossNamespaceAccess(
                context.Compilation,
                context.ContainingSymbol?.ContainingNamespace,
                location,
                symbol,
                context.ReportDiagnostic);
        }

        private static void ReportCrossNamespaceAccess(
            SymbolAnalysisContext context,
            Location location,
            ITypeSymbol? type)
        {
            ReportCrossNamespaceAccess(
                context.Compilation,
                context.Symbol.ContainingNamespace,
                location,
                type,
                context.ReportDiagnostic);
        }

        private static void ReportCrossNamespaceAccess(
            Compilation compilation,
            INamespaceSymbol? useNamespace,
            Location location,
            ISymbol? symbol,
            System.Action<Diagnostic> reportDiagnostic)
        {
            var restrictedSymbol = FindRestrictedSymbol(symbol);
            if (restrictedSymbol == null)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(restrictedSymbol.ContainingAssembly, compilation.Assembly))
            {
                return;
            }

            if (useNamespace == null)
            {
                return;
            }

            var declarationNamespace = restrictedSymbol.ContainingNamespace;
            if (declarationNamespace == null || IsSameNamespace(useNamespace, declarationNamespace))
            {
                return;
            }

            reportDiagnostic(Diagnostic.Create(
                Rule_InternalNamespaceAccess,
                location,
                restrictedSymbol.ToDiagnosticMessageName(),
                useNamespace.ToDiagnosticMessageName(),
                declarationNamespace.ToDiagnosticMessageName()));
        }

        private static ISymbol? TryGetNameOfSymbolFromOperation(INameOfOperation nameOf)
        {
            if (nameOf.Argument is IMemberReferenceOperation memberRef)
            {
                return memberRef.Member;
            }

            if (nameOf.Argument is ITypeOfOperation typeOf)
            {
                return typeOf.TypeOperand as INamedTypeSymbol;
            }

            return null;
        }

        private static ISymbol? TryGetNameOfSymbolFromSemanticModel(INameOfOperation nameOf)
        {
            var semanticModel = nameOf.SemanticModel;
            if (semanticModel == null || nameOf.Argument == null)
            {
                return null;
            }

            return semanticModel.GetSymbolInfo(nameOf.Argument.Syntax).Symbol;
        }

        private static ITypeSymbol? GetPatternTypeSymbol(IPatternOperation pattern) =>
            pattern switch
            {
                ITypePatternOperation typePattern => typePattern.MatchedType,
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType,
                IRecursivePatternOperation recursivePattern => recursivePattern.MatchedType,
                INegatedPatternOperation negated => GetPatternTypeSymbol(negated.Pattern),
                IBinaryPatternOperation binary =>
                    GetPatternTypeSymbol(binary.LeftPattern) ?? GetPatternTypeSymbol(binary.RightPattern),
                _ => null
            };

        private static ISymbol? FindRestrictedSymbol(ISymbol? symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            while (true)
            {
                if (symbol is IArrayTypeSymbol arrayType)
                {
                    symbol = arrayType.ElementType;
                }
                else if (symbol is IPointerTypeSymbol pointerType)
                {
                    symbol = pointerType.PointedAtType;
                }
                else
                {
                    break;
                }
            }

            for (var current = symbol; current != null; current = current.ContainingType)
            {
                if (IsInternalOrProtectedInternal(current.DeclaredAccessibility))
                {
                    return current == symbol ? current : symbol;
                }

                if (current is INamedTypeSymbol namedType)
                {
                    foreach (var typeArg in namedType.TypeArguments)
                    {
                        var restricted = FindRestrictedSymbol(typeArg);
                        if (restricted != null)
                        {
                            return restricted;
                        }
                    }
                }
            }

            if (symbol is IMethodSymbol method)
            {
                foreach (var typeArg in method.TypeArguments)
                {
                    var restricted = FindRestrictedSymbol(typeArg);
                    if (restricted != null)
                    {
                        return restricted;
                    }
                }
            }

            return null;
        }

        private static bool IsInternalOrProtectedInternal(Accessibility accessibility) =>
            accessibility == Accessibility.Internal
            || accessibility == Accessibility.ProtectedOrInternal;

        private static bool IsSameNamespace(INamespaceSymbol left, INamespaceSymbol right) =>
            SymbolEqualityComparer.Default.Equals(left, right);
    }
}
