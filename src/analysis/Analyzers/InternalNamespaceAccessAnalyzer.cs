using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        private static string[] VisibleNamespaces = System.Array.Empty<string>();
        private static string[] VisibleTypes = System.Array.Empty<string>();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(ctx =>
            {
                VisibleNamespaces = Core.GetConfigurationArray(ctx, Core.Config_VisibleInternalNamespaces);
                VisibleTypes = Core.GetConfigurationArray(ctx, Core.Config_VisibleInternalTypes);
            });

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
                OperationKind.DeclarationPattern,
                OperationKind.RecursivePattern,
                OperationKind.TypePattern);

            context.RegisterOperationAction(AnalyzeNameOf, OperationKind.NameOf);

            context.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Field,
                SymbolKind.Property,
                SymbolKind.Event);

            context.RegisterSyntaxNodeAction(
                AnalyzeLocalFunctionDeclaration,
                SyntaxKind.LocalFunctionStatement);
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
            var useNamespace = context.Symbol.ContainingNamespace;
            switch (context.Symbol)
            {
                case IFieldSymbol field:
                    ReportCrossNamespaceAccess(
                        context.Compilation,
                        useNamespace,
                        GetFieldTypeLocations(field),
                        field.Type,
                        context.ReportDiagnostic);
                    break;

                case IPropertySymbol property:
                    ReportCrossNamespaceAccess(
                        context.Compilation,
                        useNamespace,
                        GetPropertyTypeLocations(property),
                        property.Type,
                        context.ReportDiagnostic);
                    foreach (var parameter in property.Parameters)
                    {
                        ReportCrossNamespaceAccess(
                            context.Compilation,
                            useNamespace,
                            GetIndexerParameterTypeLocations(property, parameter),
                            parameter.Type,
                            context.ReportDiagnostic);
                    }

                    break;

                case IEventSymbol @event:
                    ReportCrossNamespaceAccess(
                        context.Compilation,
                        useNamespace,
                        GetEventTypeLocations(@event),
                        @event.Type,
                        context.ReportDiagnostic);
                    break;

                case IMethodSymbol method:
                    if (method.AssociatedSymbol is IPropertySymbol or IEventSymbol
                        || method.MethodKind == MethodKind.LocalFunction
                        || method.IsImplicitlyDeclared)
                    {
                        break;
                    }

                    AnalyzeMethodSignature(
                        context.Compilation,
                        useNamespace,
                        method,
                        context.ReportDiagnostic);
                    break;

                case INamedTypeSymbol namedType:
                    if (namedType.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
                    {
                        ReportCrossNamespaceAccess(
                            context.Compilation,
                            useNamespace,
                            GetBaseOrInterfaceTypeLocations(namedType, baseType, context.Compilation),
                            baseType,
                            context.ReportDiagnostic);
                    }

                    foreach (var @interface in namedType.Interfaces)
                    {
                        ReportCrossNamespaceAccess(
                            context.Compilation,
                            useNamespace,
                            GetBaseOrInterfaceTypeLocations(namedType, @interface, context.Compilation),
                            @interface,
                            context.ReportDiagnostic);
                    }

                    foreach (var typeParam in namedType.TypeParameters)
                    {
                        foreach (var constraint in typeParam.ConstraintTypes)
                        {
                            ReportCrossNamespaceAccess(
                                context.Compilation,
                                useNamespace,
                                GetTypeParameterConstraintLocations(namedType, typeParam, constraint, context.Compilation),
                                constraint,
                                context.ReportDiagnostic);
                        }
                    }

                    if (namedType.TypeKind == TypeKind.Delegate && namedType.DelegateInvokeMethod is { } invokeMethod)
                    {
                        ReportCrossNamespaceAccess(
                            context.Compilation,
                            useNamespace,
                            GetDelegateReturnTypeLocations(namedType),
                            invokeMethod.ReturnType,
                            context.ReportDiagnostic);

                        foreach (var parameter in invokeMethod.Parameters)
                        {
                            ReportCrossNamespaceAccess(
                                context.Compilation,
                                useNamespace,
                                GetDelegateParameterTypeLocations(namedType, parameter),
                                parameter.Type,
                                context.ReportDiagnostic);
                        }
                    }

                    break;
            }
        }

        private static void AnalyzeLocalFunctionDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IMethodSymbol method
                || method.MethodKind != MethodKind.LocalFunction)
            {
                return;
            }

            AnalyzeMethodSignature(
                context.Compilation,
                method.ContainingNamespace,
                method,
                context.ReportDiagnostic);
        }

        private static void AnalyzeMethodSignature(
            Compilation compilation,
            INamespaceSymbol? useNamespace,
            IMethodSymbol method,
            System.Action<Diagnostic> reportDiagnostic)
        {
            ReportCrossNamespaceAccess(
                compilation,
                useNamespace,
                GetReturnTypeLocations(method),
                method.ReturnType,
                reportDiagnostic);
            foreach (var parameter in method.Parameters)
            {
                ReportCrossNamespaceAccess(
                    compilation,
                    useNamespace,
                    GetParameterTypeLocations(method, parameter),
                    parameter.Type,
                    reportDiagnostic);
            }

            foreach (var typeParam in method.TypeParameters)
            {
                foreach (var constraint in typeParam.ConstraintTypes)
                {
                    ReportCrossNamespaceAccess(
                        compilation,
                        useNamespace,
                        GetTypeParameterConstraintLocations(method, typeParam, constraint, compilation),
                        constraint,
                        reportDiagnostic);
                }
            }
        }

        private static IEnumerable<Location> GetReturnTypeLocations(IMethodSymbol method)
        {
            var any = false;
            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                if (syntax is MethodDeclarationSyntax methodDecl && methodDecl.ReturnType != null)
                {
                    yield return methodDecl.ReturnType.GetLocation();
                    any = true;
                }
                else if (syntax is LocalFunctionStatementSyntax localFunc && localFunc.ReturnType != null)
                {
                    yield return localFunc.ReturnType.GetLocation();
                    any = true;
                }
                else if (syntax is OperatorDeclarationSyntax operatorDecl && operatorDecl.ReturnType != null)
                {
                    yield return operatorDecl.ReturnType.GetLocation();
                    any = true;
                }
                else if (syntax is ConversionOperatorDeclarationSyntax conversionDecl && conversionDecl.Type != null)
                {
                    yield return conversionDecl.Type.GetLocation();
                    any = true;
                }
            }

            if (!any) foreach (var loc in method.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetParameterTypeLocations(IMethodSymbol method, IParameterSymbol parameter)
        {
            var any = false;
            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                if (syntax is BaseMethodDeclarationSyntax methodDecl)
                {
                    var index = parameter.Ordinal;
                    if (index >= 0 && index < methodDecl.ParameterList.Parameters.Count)
                    {
                        var parameterSyntax = methodDecl.ParameterList.Parameters[index];
                        if (parameterSyntax.Type != null)
                        {
                            yield return parameterSyntax.Type.GetLocation();
                            any = true;
                        }
                    }
                }
                else if (syntax is LocalFunctionStatementSyntax localFunc)
                {
                    var index = parameter.Ordinal;
                    if (index >= 0 && index < localFunc.ParameterList.Parameters.Count)
                    {
                        var parameterSyntax = localFunc.ParameterList.Parameters[index];
                        if (parameterSyntax.Type != null)
                        {
                            yield return parameterSyntax.Type.GetLocation();
                            any = true;
                        }
                    }
                }
            }

            if (!any) foreach (var loc in parameter.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetFieldTypeLocations(IFieldSymbol field)
        {
            var any = false;
            foreach (var syntaxRef in field.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is VariableDeclaratorSyntax declarator
                    && declarator.Parent is VariableDeclarationSyntax variableDeclaration)
                {
                    yield return variableDeclaration.Type.GetLocation();
                    any = true;
                }
            }

            if (!any) foreach (var loc in field.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetPropertyTypeLocations(IPropertySymbol property)
        {
            var any = false;
            foreach (var syntaxRef in property.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                if (syntax is PropertyDeclarationSyntax propertyDeclaration)
                {
                    yield return propertyDeclaration.Type.GetLocation();
                    any = true;
                }
                else if (syntax is IndexerDeclarationSyntax indexerDeclaration)
                {
                    yield return indexerDeclaration.Type.GetLocation();
                    any = true;
                }
            }

            if (!any) foreach (var loc in property.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetIndexerParameterTypeLocations(IPropertySymbol property, IParameterSymbol parameter)
        {
            var any = false;
            foreach (var syntaxRef in property.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is IndexerDeclarationSyntax indexerDecl)
                {
                    var index = parameter.Ordinal;
                    if (index >= 0 && index < indexerDecl.ParameterList.Parameters.Count)
                    {
                        var parameterSyntax = indexerDecl.ParameterList.Parameters[index];
                        if (parameterSyntax.Type != null)
                        {
                            yield return parameterSyntax.Type.GetLocation();
                            any = true;
                        }
                    }
                }
            }

            if (!any) foreach (var loc in parameter.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetBaseOrInterfaceTypeLocations(
            INamedTypeSymbol namedType,
            ITypeSymbol typeSymbol,
            Compilation compilation)
        {
            var any = false;
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
                        yield return baseTypeSyntax.Type.GetLocation();
                        any = true;
                    }
                }
            }

            if (!any) foreach (var loc in namedType.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetEventTypeLocations(IEventSymbol @event)
        {
            var any = false;
            foreach (var syntaxRef in @event.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                if (syntax is VariableDeclaratorSyntax declarator
                    && declarator.Parent is VariableDeclarationSyntax variableDeclaration)
                {
                    yield return variableDeclaration.Type.GetLocation();
                    any = true;
                }
                else if (syntax is EventDeclarationSyntax eventDeclaration)
                {
                    yield return eventDeclaration.Type.GetLocation();
                    any = true;
                }
            }

            if (!any) foreach (var loc in @event.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetTypeParameterConstraintLocations(
            ISymbol symbol,
            ITypeParameterSymbol typeParam,
            ITypeSymbol constraintType,
            Compilation compilation)
        {
            var any = false;
            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                TypeParameterConstraintClauseSyntax? constraintClause = null;
                if (syntax is TypeDeclarationSyntax typeDecl)
                {
                    foreach (var clause in typeDecl.ConstraintClauses)
                    {
                        if (clause.Name.Identifier.Text == typeParam.Name)
                        {
                            constraintClause = clause;
                            break;
                        }
                    }
                }
                else if (syntax is MethodDeclarationSyntax methodDecl)
                {
                    foreach (var clause in methodDecl.ConstraintClauses)
                    {
                        if (clause.Name.Identifier.Text == typeParam.Name)
                        {
                            constraintClause = clause;
                            break;
                        }
                    }
                }
                else if (syntax is DelegateDeclarationSyntax delegateDecl)
                {
                    foreach (var clause in delegateDecl.ConstraintClauses)
                    {
                        if (clause.Name.Identifier.Text == typeParam.Name)
                        {
                            constraintClause = clause;
                            break;
                        }
                    }
                }
                else if (syntax is LocalFunctionStatementSyntax localFunc)
                {
                    foreach (var clause in localFunc.ConstraintClauses)
                    {
                        if (clause.Name.Identifier.Text == typeParam.Name)
                        {
                            constraintClause = clause;
                            break;
                        }
                    }
                }

                if (constraintClause == null)
                {
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                foreach (var constraint in constraintClause.Constraints)
                {
                    if (constraint is TypeConstraintSyntax typeConstraint)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(typeConstraint.Type);
                        if (SymbolEqualityComparer.Default.Equals(typeInfo.Type, constraintType)
                            || SymbolEqualityComparer.Default.Equals(typeInfo.ConvertedType, constraintType))
                        {
                            yield return typeConstraint.Type.GetLocation();
                            any = true;
                        }
                    }
                }
            }

            if (!any) foreach (var loc in symbol.Locations) yield return loc;
        }

        private static void ReportCrossNamespaceAccess(
            OperationAnalysisContext context,
            IOperation operation,
            ISymbol? symbol)
        {
            var location = operation.Syntax.GetLocation();
            ReportCrossNamespaceAccess(
                context.Compilation,
                context.ContainingSymbol?.ContainingNamespace,
                location != null ? new[] { location } : Enumerable.Empty<Location>(),
                symbol,
                context.ReportDiagnostic);
        }

        private static void ReportCrossNamespaceAccess(
            Compilation compilation,
            INamespaceSymbol? useNamespace,
            IEnumerable<Location> locations,
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

            if (restrictedSymbol.ContainingType?.Name == "SR")
            {
                return;
            }

            if (VisibleTypes.Contains(restrictedSymbol.ContainingType?.Name ?? string.Empty))
            {
                return;
            }

            if (useNamespace == null)
            {
                return;
            }

            var declarationNamespace = restrictedSymbol.ContainingNamespace;
            if (declarationNamespace == null
                || declarationNamespace.Name == "Core"
                || VisibleNamespaces.Contains(declarationNamespace.Name)
                || IsSameNamespace(useNamespace, declarationNamespace))
            {
                return;
            }

            var anyReported = false;
            foreach (var location in locations)
            {
                if (location == null || location == Location.None) continue;
                reportDiagnostic(Diagnostic.Create(
                    Rule_InternalNamespaceAccess,
                    location,
                    restrictedSymbol.ToDiagnosticMessageName(),
                    useNamespace.ToDiagnosticMessageName(),
                    declarationNamespace.ToDiagnosticMessageName()));
                anyReported = true;
            }

            if (!anyReported)
            {
                foreach (var location in restrictedSymbol.Locations)
                {
                    if (location == null || location == Location.None) continue;
                    reportDiagnostic(Diagnostic.Create(
                        Rule_InternalNamespaceAccess,
                        location,
                        restrictedSymbol.ToDiagnosticMessageName(),
                        useNamespace.ToDiagnosticMessageName(),
                        declarationNamespace.ToDiagnosticMessageName()));
                }
            }
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
                if (current is INamedTypeSymbol { IsAnonymousType: true })
                {
                    continue;
                }

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

        private static IEnumerable<Location> GetDelegateReturnTypeLocations(INamedTypeSymbol delegateType)
        {
            var any = false;
            foreach (var syntaxRef in delegateType.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is DelegateDeclarationSyntax delegateDecl && delegateDecl.ReturnType != null)
                {
                    yield return delegateDecl.ReturnType.GetLocation();
                    any = true;
                }
            }

            if (!any) foreach (var loc in delegateType.Locations) yield return loc;
        }

        private static IEnumerable<Location> GetDelegateParameterTypeLocations(INamedTypeSymbol delegateType, IParameterSymbol parameter)
        {
            var any = false;
            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod != null)
            {
                foreach (var syntaxRef in delegateType.DeclaringSyntaxReferences)
                {
                    if (syntaxRef.GetSyntax() is DelegateDeclarationSyntax delegateDecl)
                    {
                        var index = parameter.Ordinal;
                        if (index >= 0 && index < delegateDecl.ParameterList.Parameters.Count)
                        {
                            var parameterSyntax = delegateDecl.ParameterList.Parameters[index];
                            if (parameterSyntax.Type != null)
                            {
                                yield return parameterSyntax.Type.GetLocation();
                                any = true;
                            }
                        }
                    }
                }
            }

            if (!any) foreach (var loc in parameter.Locations) yield return loc;
        }
    }
}
