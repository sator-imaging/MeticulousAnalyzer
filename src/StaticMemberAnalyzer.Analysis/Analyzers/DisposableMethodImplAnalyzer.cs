// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DisposableMethodImplAnalyzer : DiagnosticAnalyzer
    {
        public const string DisposeMethodName = "Dispose";
        private const string AsyncDisposableTypeName = "IAsyncDisposable";

        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_UndisposedMember = "SMA0043";
        private static readonly DiagnosticDescriptor Rule_UndisposedMember = new(
            RuleId_UndisposedMember,
            new LocalizableResourceString(nameof(Resources.SMA0043_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0043_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0043_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_MissingDisposeImplementation = "SMA0044";
        private static readonly DiagnosticDescriptor Rule_MissingDisposeImplementation = new(
            RuleId_MissingDisposeImplementation,
            new LocalizableResourceString(nameof(Resources.SMA0044_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0044_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0044_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_UndisposedMember,
            Rule_MissingDisposeImplementation
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            if (context.Symbol is not INamedTypeSymbol typeSymbol)
                return;

            if (typeSymbol.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                return;

            var disposableMembers = GetDisposableMembers(typeSymbol);
            if (!disposableMembers.Any())
                return;

            IMethodSymbol? targetMethod = null;
            IMethodSymbol? publicDispose = null;
            IMethodSymbol? explicitDispose = null;

            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is not IMethodSymbol method) continue;

                if (method.Name == DisposeMethodName)
                {
                    if (method.Parameters.Length == 1 && method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean)
                    {
                        targetMethod = method;
                        break;
                    }

                    if (method.Parameters.Length == 0 && method.DeclaredAccessibility == Accessibility.Public)
                    {
                        publicDispose = method;
                        break;
                    }

                    if (method.ExplicitInterfaceImplementations.Any(e => e.Name == DisposeMethodName))
                    {
                        explicitDispose = method;
                        break;
                    }
                }
            }

            targetMethod ??= publicDispose ?? explicitDispose;

            if (targetMethod == null)
            {
                Report(context, Rule_MissingDisposeImplementation, typeSymbol, typeSymbol.Name);
                return;
            }

            var undisposedSet = new HashSet<ISymbol>(disposableMembers, SymbolEqualityComparer.Default);
            CollectUndisposedMembers(context.Compilation, targetMethod, undisposedSet);
            if (undisposedSet.Count != 0)
            {
                var joinedNames = string.Join(", ", undisposedSet.Select(m => m.Name));
                Report(context, Rule_UndisposedMember, typeSymbol, joinedNames);
            }
        }

        private static void Report(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, INamedTypeSymbol typeSymbol, params object[]? messageArgs)
        {
            foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                var location = syntaxRef.GetSyntax() switch
                {
                    TypeDeclarationSyntax typeDecl => typeDecl.Identifier.GetLocation(),
                    var syntax => syntax.GetLocation()
                };

                context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
            }
        }

        private static void CollectUndisposedMembers(Compilation compilation, IMethodSymbol method, HashSet<ISymbol> undisposed)
        {
            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                var model = compilation.GetSemanticModel(syntax.SyntaxTree);
                var operation = model.GetOperation(syntax);

                if (operation == null) continue;

                foreach (var op in operation.Descendants())
                {
                    var candidate = op;
                    if (candidate is IConditionalAccessOperation conditional)
                    {
                        candidate = conditional.WhenNotNull;
                    }

                    if (candidate is not IInvocationOperation invocation)
                    {
                        continue;
                    }

                    if (IsDisposeCall(invocation.TargetMethod))
                    {
                        var instance = invocation.Instance;
                        if (instance is IConversionOperation conversion)
                        {
                            instance = conversion.Operand;
                        }

                        if (instance is IMemberReferenceOperation memberRef)
                        {
                            undisposed.Remove(memberRef.Member);
                        }
                    }
                }
            }
        }

        private static bool IsDisposeCall(IMethodSymbol method)
        {
            return method.Name == DisposeMethodName && method.Parameters.Length == 0 && method.ReturnType.SpecialType == SpecialType.System_Void;
        }


        private static IEnumerable<ISymbol> GetDisposableMembers(INamedTypeSymbol typeSymbol)
        {
            foreach (var member in typeSymbol.GetMembers())
            {
                if (member.IsStatic || member.IsImplicitlyDeclared) continue;

                if (member is IFieldSymbol fieldSymbol)
                {
                    if (IsDisposable(fieldSymbol.Type))
                    {
                        yield return fieldSymbol;
                    }
                }
                else if (member is IPropertySymbol propertySymbol)
                {
                    if (IsDisposable(propertySymbol.Type))
                    {
                        yield return propertySymbol;
                    }
                }
            }
        }

        private static bool IsDisposable(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol named)
                return false;

            // Check IDisposable or IAsyncDisposable
            if (named.SpecialType == SpecialType.System_IDisposable ||
                named.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable || IsAsyncDisposable(i)))
            {
                return true;
            }

            return false;
        }

        private static bool IsAsyncDisposable(INamedTypeSymbol symbol)
        {
            return symbol.Name == AsyncDisposableTypeName &&
                   symbol.ContainingNamespace.Name == "System" &&
                   symbol.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;
        }
    }
}
