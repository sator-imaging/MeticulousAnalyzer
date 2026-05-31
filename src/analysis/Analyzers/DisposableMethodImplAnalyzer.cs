// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        public const string RuleId_MissingIDisposableInterface = "SMA0045";
        private static readonly DiagnosticDescriptor Rule_MissingIDisposableInterface = new(
            RuleId_MissingIDisposableInterface,
            new LocalizableResourceString(nameof(Resources.SMA0045_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0045_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0045_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_UndisposedMember,
            Rule_MissingDisposeImplementation,
            Rule_MissingIDisposableInterface
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
            {
                return;
            }

            if (typeSymbol.TypeKind is not (TypeKind.Class or TypeKind.Struct))
            {
                return;
            }

            var disposableMemberSet = GetDisposableMembers(typeSymbol);
            if (disposableMemberSet == null)
            {
                return;
            }

            IMethodSymbol? fullDisposeMethod = null;
            IMethodSymbol? publicDisposeMethod = null;
            IMethodSymbol? explicitImplMethod = null;

            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is not IMethodSymbol method)
                {
                    continue;
                }

                if (method.Name == DisposeMethodName)
                {
                    if (method.Parameters.Length == 1 &&
                        method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean)
                    {
                        fullDisposeMethod = method;
                        break;
                    }

                    if (publicDisposeMethod == null &&
                        method.Parameters.Length == 0 &&
                        method.DeclaredAccessibility == Accessibility.Public &&
                        method.ReturnType.SpecialType == SpecialType.System_Void)
                    {
                        publicDisposeMethod = method;
                    }
                }

                if (explicitImplMethod == null &&
                    method.ExplicitInterfaceImplementations.Any(static e =>
                    {
                        return e.ContainingType.SpecialType == SpecialType.System_IDisposable
                            && e.Name == DisposeMethodName;
                    }))
                {
                    explicitImplMethod = method;
                }
            }

            if (!typeSymbol.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_IDisposable))
            {
                ReportDiagnostic(context, Rule_MissingIDisposableInterface, typeSymbol, typeSymbol.ToDiagnosticMessageName());
            }

            var targetMethod = fullDisposeMethod ?? publicDisposeMethod ?? explicitImplMethod;
            if (targetMethod == null)
            {
                ReportDiagnostic(context, Rule_MissingDisposeImplementation, typeSymbol, typeSymbol.ToDiagnosticMessageName());
                return;
            }

            AnalyzeAndUpdateDisposableMemberSet(context.Compilation, targetMethod, disposableMemberSet);
            if (disposableMemberSet.Count != 0)
            {
                ReportUndisposedMembers(context, typeSymbol, disposableMemberSet);
            }
        }

        private static void ReportUndisposedMembers(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol, HashSet<ISymbol> undisposedMembers)
        {
            foreach (var member in undisposedMembers)
            {
                var reported = false;
                foreach (var syntaxRef in member.DeclaringSyntaxReferences)
                {
                    if (syntaxRef.GetSyntax() is not VariableDeclaratorSyntax varDecl)
                    {
                        continue;
                    }

                    var location = varDecl.Identifier.GetLocation();

                    // Declarator -> Declaration -> FieldDeclaration
                    if (!Core.IsSuppressedByComment(varDecl.Parent?.Parent, DisposableAnalyzer.SuppressionComment))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule_UndisposedMember, location, member.ToDiagnosticMessageName()));
                    }

                    reported = true;  // Set true even if suppressed
                }

                if (!reported)
                {
                    ReportDiagnostic(context, Rule_UndisposedMember, typeSymbol, member.ToDiagnosticMessageName());
                }
            }
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object[]? messageArgs)
        {
            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                var location = syntaxRef.GetSyntax() switch
                {
                    TypeDeclarationSyntax typeDecl => typeDecl.Identifier.GetLocation(),
                    var syntax => syntax.GetLocation()
                };

                context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
            }
        }

        private static void AnalyzeAndUpdateDisposableMemberSet(Compilation compilation, IMethodSymbol method, HashSet<ISymbol> undisposed)
        {
            foreach (var syntaxRef in method.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();
                var model = compilation.GetSemanticModel(syntax.SyntaxTree);
                var operation = model.GetOperation(syntax);

                if (operation == null)
                {
                    continue;
                }

                foreach (var op in operation.DescendantsAndSelf())
                {
                    IOperation? instance = null;

                    var candidate = op;
                    if (candidate is IConditionalAccessOperation conditional)
                    {
                        candidate = conditional.WhenNotNull;
                        instance = conditional.Operation;
                    }

                    if (candidate is not IInvocationOperation invocation)
                    {
                        continue;
                    }

                    if (IsDisposeCall(invocation.TargetMethod))
                    {
                        instance ??= invocation.Instance;
                        if (instance is IConversionOperation conversion)
                        {
                            instance = conversion.Operand;
                        }

                        if (instance is IMemberReferenceOperation memberRef)
                        {
                            undisposed.Remove(memberRef.Member);
                        }
                    }

                    if (undisposed.Count == 0)
                    {
                        return;
                    }
                }
            }
        }

        private static bool IsDisposeCall(IMethodSymbol method)
        {
            return method.Name == DisposeMethodName
                && method.Parameters.Length == 0
                && method.ReturnType.SpecialType == SpecialType.System_Void;
        }


        private static HashSet<ISymbol>? GetDisposableMembers(INamedTypeSymbol typeSymbol)
        {
            HashSet<ISymbol>? result = null;

            foreach (var member in typeSymbol.GetMembers())
            {
                if (member.IsStatic || member.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (member is IFieldSymbol fieldSymbol)
                {
                    if (IsDisposable(fieldSymbol.Type))
                    {
                        result ??= new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                        result.Add(fieldSymbol);
                    }
                }
            }

            return result;
        }

        private static bool IsDisposable(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol named)
            {
                return false;
            }

            if (named.SpecialType == SpecialType.System_IDisposable ||
                named.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_IDisposable))
            {
                return true;
            }

            return false;
        }
    }
}
