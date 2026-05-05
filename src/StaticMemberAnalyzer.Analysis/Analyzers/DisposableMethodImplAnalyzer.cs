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

            var disposableMembers = GetDisposableMembers(typeSymbol).ToImmutableArray();
            if (disposableMembers.IsEmpty)
                return;

            var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>().ToImmutableArray();
            var disposeMethods = allMethods.Where(IsDisposeImplementation).ToImmutableArray();

            if (disposeMethods.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_MissingDisposeImplementation, typeSymbol.Locations[0], typeSymbol.Name));
                return;
            }

            var targetMethods = new List<IMethodSymbol>();
            var fullDisposePattern = false;

            foreach (var method in disposeMethods)
            {
                if (method.Parameters.Length == 0 && method.DeclaredAccessibility == Accessibility.Public)
                {
                    // Check for Full Dispose Pattern
                    if (IsFullDisposePattern(context.Compilation, method, out var disposeBoolMethod))
                    {
                        fullDisposePattern = true;
                        targetMethods.Add(disposeBoolMethod!);
                    }
                    else
                    {
                        targetMethods.Add(method);
                    }
                }
            }

            // In case of Full Dispose Pattern, we only care about Dispose(bool)
            // If not, we might have multiple Dispose methods (e.g. explicit interface implementations)
            if (fullDisposePattern)
            {
                var disposeBool = targetMethods.First(m => m.Parameters.Length == 1);
                var undisposed = GetUndisposedMembers(context.Compilation, disposeBool, disposableMembers);
                ReportUndisposed(context, disposeBool, undisposed);
            }
            else
            {
                // Report on all Dispose methods if they don't dispose everything?
                // Or just pick the main one? The instructions say "if members remain undisposed... report on each Dispose method declared in the type".
                foreach (var method in disposeMethods)
                {
                    var undisposed = GetUndisposedMembers(context.Compilation, method, disposableMembers);
                    ReportUndisposed(context, method, undisposed);
                }
            }
        }

        private static bool IsFullDisposePattern(Compilation compilation, IMethodSymbol disposeMethod, out IMethodSymbol? disposeBoolMethod)
        {
            disposeBoolMethod = null;
            var typeSymbol = disposeMethod.ContainingType;

            disposeBoolMethod = typeSymbol.GetMembers(DisposeMethodName).OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 1 &&
                                     m.Parameters[0].Type.SpecialType == SpecialType.System_Boolean &&
                                     m.DeclaredAccessibility == Accessibility.Protected);

            if (disposeBoolMethod == null) return false;

            var gcType = compilation.GetTypeByMetadataName("System.GC");
            if (gcType == null) return false;

            foreach (var syntaxRef in disposeMethod.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                var model = compilation.GetSemanticModel(syntax.SyntaxTree);
                var operation = model.GetOperation(syntax);
                if (operation == null) continue;

                foreach (var op in operation.Descendants().OfType<IInvocationOperation>())
                {
                    if (op.TargetMethod.Name == "SuppressFinalize" &&
                        SymbolEqualityComparer.Default.Equals(op.TargetMethod.ContainingType, gcType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ReportUndisposed(SymbolAnalysisContext context, IMethodSymbol method, IEnumerable<ISymbol> undisposed)
        {
            var location = method.Locations[0];
            if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDecl)
            {
                location = methodDecl.Identifier.GetLocation();
            }

            foreach (var member in undisposed)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_UndisposedMember, location, member.Name));
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
                    // Only analyze auto-properties (those without a body)
                    if (IsAutoProperty(propertySymbol) && IsDisposable(propertySymbol.Type))
                    {
                        builder.Add(propertySymbol);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static bool IsAutoProperty(IPropertySymbol propertySymbol)
        {
            foreach (var syntaxRef in propertySymbol.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax propertyDeclaration)
                {
                    if (propertyDeclaration.ExpressionBody != null)
                        return false;

                    if (propertyDeclaration.AccessorList == null)
                        return false;

                    foreach (var accessor in propertyDeclaration.AccessorList.Accessors)
                    {
                        if (accessor.Body != null || accessor.ExpressionBody != null)
                            return false;
                    }
                    return true;
                }
            }
            return false;
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

            // Duck typing: public void Dispose()
            return named.GetMembers(DisposeMethodName).OfType<IMethodSymbol>()
                .Any(m => m.Parameters.Length == 0 &&
                          m.ReturnType.SpecialType == SpecialType.System_Void &&
                          m.DeclaredAccessibility == Accessibility.Public);
        }

        private static bool IsAsyncDisposable(INamedTypeSymbol symbol)
        {
            return symbol.Name == "IAsyncDisposable" &&
                   symbol.ContainingNamespace.Name == "System" &&
                   symbol.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;
        }
    }
}
