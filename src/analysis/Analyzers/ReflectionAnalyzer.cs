// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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

            // Each action covers its own scan target to avoid duplicate reports:
            // - Type references (typeof, locals, fields, parameters, generics, casts, etc.)
            context.RegisterSyntaxNodeAction(AnalyzeTypeReference, SyntaxKind.IdentifierName, SyntaxKind.GenericName);
            // - Method invocations (return type or declaring type)
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            // - Member references (properties, fields, events and method groups)
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.MemberBindingExpression);
            // - Locals declarations (`var` only; explicit type is reported as type reference)
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }


        /*  type reference  ================================================================ */

        private static void AnalyzeTypeReference(SyntaxNodeAnalysisContext context)
        {
            // NOTE: `var` keyword resolves to the inferred type. covered by locals declaration action.
            if (context.Node is not SimpleNameSyntax name || name.IsVar)
            {
                return;
            }

            // NOTE: namespace parts of qualified name don't resolve to type symbol.
            //       method/property/field references are covered by other actions.
            if (context.SemanticModel.GetSymbolInfo(name).Symbol is not INamedTypeSymbol type)
            {
                return;
            }

            if (IsReflectionType(type))
            {
                Report(context, name.Identifier, type);
            }
        }


        /*  invocation  ================================================================ */

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            {
                return;
            }

            // NOTE: arguments are ignored. identifiers found in argument expressions
            //       are detected by the other scan targets.
            var found = FindReflectionType(method.ReturnType)
                ?? GetReflectionReceiverType(context, GetReceiverSyntax(invocation.Expression), method);
            if (found == null)
            {
                return;
            }

            var nameSyntax = GetInvokedNameSyntax(invocation.Expression);
            if (nameSyntax == null)
            {
                return;
            }

            Report(context, nameSyntax.Identifier, found);
        }

        private static SimpleNameSyntax? GetInvokedNameSyntax(ExpressionSyntax expression)
        {
            return expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name,
                SimpleNameSyntax simpleName => simpleName,
                _ => null,
            };
        }

        private static ExpressionSyntax? GetReceiverSyntax(ExpressionSyntax expression)
        {
            return expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                // `foo?.Bar` receiver is found on enclosing conditional access expression
                MemberBindingExpressionSyntax memberBinding => memberBinding.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression,
                _ => null,
            };
        }

        // NOTE: member declared on reflection type can be inherited by non-reflection type
        //       (e.g. `System.Type` derives from `MemberInfo`) so that check receiver type
        //       instead of member's containing type.
        private static INamedTypeSymbol? GetReflectionReceiverType(SyntaxNodeAnalysisContext context, ExpressionSyntax? receiver, ISymbol member)
        {
            var receiverType = receiver != null
                ? context.SemanticModel.GetTypeInfo(receiver).Type
                : member.ContainingType;  // implicit receiver (omitted `this.` or `using static` import)

            return receiverType is INamedTypeSymbol named && IsReflectionType(named) ? named : null;
        }


        /*  member reference  ================================================================ */

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            SimpleNameSyntax name;
            ExpressionSyntax? qualifier;

            switch (context.Node)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    name = memberAccess.Name;
                    qualifier = memberAccess.Expression;
                    break;
                case MemberBindingExpressionSyntax memberBinding:
                    name = memberBinding.Name;
                    qualifier = null;
                    break;
                default:
                    return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(name).Symbol;

            var memberType = symbol switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                IEventSymbol @event => @event.Type,
                // method group reference. invoked method is covered by invocation action
                IMethodSymbol method when context.Node.Parent is not InvocationExpressionSyntax => method.ReturnType,
                _ => null,
            };

            if (symbol == null || memberType == null)
            {
                return;
            }

            // static member access on reflection type (e.g. `BindingFlags.Public`)
            // is already reported as type reference on the qualifier identifier.
            if (qualifier != null && context.SemanticModel.GetSymbolInfo(qualifier).Symbol is INamedTypeSymbol)
            {
                return;
            }

            var found = FindReflectionType(memberType)
                ?? GetReflectionReceiverType(context, GetReceiverSyntax((ExpressionSyntax)context.Node), symbol);
            if (found == null)
            {
                return;
            }

            Report(context, name.Identifier, found);
        }


        /*  locals declaration  ================================================================ */

        private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is VariableDeclarationSyntax declaration)
            {
                // explicitly typed declaration is reported as type reference.
                if (!declaration.Type.IsVar)
                {
                    return;
                }

                foreach (var variable in declaration.Variables)
                {
                    if (context.SemanticModel.GetDeclaredSymbol(variable) is not ILocalSymbol local)
                    {
                        continue;
                    }

                    var found = FindReflectionType(local.Type);
                    if (found != null)
                    {
                        Report(context, variable.Identifier, found);
                    }
                }
            }
            else if (context.Node is ForEachStatementSyntax forEach)
            {
                if (!forEach.Type.IsVar)
                {
                    return;
                }

                if (context.SemanticModel.GetDeclaredSymbol(forEach) is not ILocalSymbol local)
                {
                    return;
                }

                var found = FindReflectionType(local.Type);
                if (found != null)
                {
                    Report(context, forEach.Identifier, found);
                }
            }
        }


        /*  helper  ================================================================ */

        private static void Report(SyntaxNodeAnalysisContext context, SyntaxToken identifier, INamedTypeSymbol reflectionType)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule_SystemReflectionUsage,
                identifier.GetLocation(),
                identifier.Text,
                reflectionType.ToDisplayString()));
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

                    // e.g. IEnumerable<MethodInfo>, Task<MemberInfo[]>, Nullable<T>
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
