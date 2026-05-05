// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
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
            if (disposableMembers.IsEmpty)
                return;

            var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>().ToImmutableArray();
            var disposeMethods = allMethods.Where(IsDisposeImplementation).ToImmutableArray();

            var location = typeSymbol.Locations[0];
            if (typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax typeDecl)
            {
                location = typeDecl.Identifier.GetLocation();
            }

            if (disposeMethods.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_MissingDisposeImplementation, location, typeSymbol.Name));
                return;
            }

            // Find target method in order: Dispose(bool), Dispose(), then explicit IDisposable.Dispose
            IMethodSymbol? targetMethod = disposeMethods.FirstOrDefault(m => m.Parameters.Length == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Boolean);
            if (targetMethod == null)
            {
                targetMethod = disposeMethods.FirstOrDefault(m => m.Parameters.Length == 0 && m.DeclaredAccessibility == Accessibility.Public && !m.ExplicitInterfaceImplementations.Any());
            }
            if (targetMethod == null)
            {
                targetMethod = disposeMethods.FirstOrDefault(m => m.ExplicitInterfaceImplementations.Any(e => e.Name == DisposeMethodName));
            }

            if (targetMethod != null)
            {
                var undisposed = GetUndisposedMembers(context.Compilation, targetMethod, disposableMembers);
                if (undisposed.Any())
                {
                    var joinedNames = string.Join(", ", undisposed.Select(m => m.Name));
                    context.ReportDiagnostic(Diagnostic.Create(Rule_UndisposedMember, location, joinedNames));
                }
            }
        }

        private static HashSet<ISymbol> GetUndisposedMembers(Compilation compilation, IMethodSymbol method, ImmutableArray<ISymbol> disposableMembers)
        {
            var undisposed = new HashSet<ISymbol>(disposableMembers, SymbolEqualityComparer.Default);

            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                var model = compilation.GetSemanticModel(syntax.SyntaxTree);
                var operation = model.GetOperation(syntax);

                if (operation == null) continue;

                foreach (var op in operation.Descendants())
                {
                    if (op is IInvocationOperation invocation)
                    {
                        if (IsDisposeCall(invocation.TargetMethod))
                        {
                            var receiver = invocation.Instance;
                            if (receiver is IConversionOperation conversion)
                            {
                                receiver = conversion.Operand;
                            }

                            if (receiver is IMemberReferenceOperation memberRef)
                            {
                                undisposed.Remove(memberRef.Member);
                            }
                        }
                    }
                    else if (op is IConditionalAccessOperation conditionalAccess)
                    {
                        if (conditionalAccess.WhenNotNull is IInvocationOperation invocationOnNotNull &&
                            IsDisposeCall(invocationOnNotNull.TargetMethod))
                        {
                            var receiver = conditionalAccess.Operation;
                            if (receiver is IConversionOperation conversion)
                            {
                                receiver = conversion.Operand;
                            }

                            if (receiver is IMemberReferenceOperation memberRef)
                            {
                                undisposed.Remove(memberRef.Member);
                            }
                        }
                    }
                }
            }

            return undisposed;
        }

        private static bool IsDisposeCall(IMethodSymbol method)
        {
            if (method.Name == DisposeMethodName) return true;

            foreach (var explicitImpl in method.ExplicitInterfaceImplementations)
            {
                if (explicitImpl.Name == DisposeMethodName) return true;
            }

            return false;
        }

        private static bool IsDisposeImplementation(IMethodSymbol method)
        {
            if (method.Name == DisposeMethodName) return true;

            if (method.ExplicitInterfaceImplementations.Any(e => e.Name == DisposeMethodName))
                return true;

            return false;
        }

        private static ImmutableArray<ISymbol> GetDisposableMembers(INamedTypeSymbol typeSymbol)
        {
            var builder = ImmutableArray.CreateBuilder<ISymbol>();

            foreach (var member in typeSymbol.GetMembers())
            {
                if (member.IsStatic || member.IsImplicitlyDeclared) continue;

                if (member is IFieldSymbol fieldSymbol)
                {
                    if (IsDisposable(fieldSymbol.Type))
                    {
                        builder.Add(fieldSymbol);
                    }
                }
                else if (member is IPropertySymbol propertySymbol)
                {
                    if (IsDisposable(propertySymbol.Type))
                    {
                        builder.Add(propertySymbol);
                    }
                }
            }

            return builder.ToImmutable();
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
            return symbol.Name == "IAsyncDisposable" &&
                   symbol.ContainingNamespace.Name == "System" &&
                   symbol.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;
        }
    }
}
