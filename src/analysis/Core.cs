// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SatorImaging.StaticMemberAnalyzer.Analysis
{
    public static class Core
    {
        // https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Utilities/Compiler/DiagnosticCategoryAndIdRanges.txt
        internal const string Category = nameof(StaticMemberAnalyzer);

        const string ConfigPrefix = "sator_imaging.";
        public const string Config_EnableImmutableVariable = ConfigPrefix + "immutable_variable";
        public const string Config_EnableDuckTypingRecognition = ConfigPrefix + "duck_typing_recognition";
        public const string Config_VisibleInternalNamespaces = ConfigPrefix + "visible_internal_namespaces";
        public const string Config_VisibleInternalTypes = ConfigPrefix + "visible_internal_types";

        public static bool GetConfiguration(CompilationStartAnalysisContext context, string key)
        {
            // GlobalOptions is NOT .editorconfig. Just check falsy.
            return context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(key, out var value)
                && !value.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] GetConfigurationArray(CompilationStartAnalysisContext context, string key)
        {
            if (context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(key, out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                var split = value.Split(',');
                for (int i = 0; i < split.Length; i++)
                {
                    split[i] = split[i].Trim();
                }
                return split;
            }
            return Array.Empty<string>();
        }


        /*  Debug Reporter  ================================================================ */

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization

        // NOTE: define in any case to avoid error.
        //       but this is not registered to analyzer when debug message flag is not set.
        const string RuleId_DebugError = "DEBUGxERROR";  // no hyphens!
        internal static readonly DiagnosticDescriptor Rule_DebugError = new(
            RuleId_DebugError,
            RuleId_DebugError,
            messageFormat: "{0}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RuleId_DebugError);

        const string RuleId_DebugWarn = "DEBUGxWARN";  // no hyphens!
        internal static readonly DiagnosticDescriptor Rule_DebugWarn = new(
            RuleId_DebugWarn,
            RuleId_DebugWarn,
            messageFormat: "{0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: RuleId_DebugWarn);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Report(Action<Diagnostic> reportMethod,
                                    DiagnosticDescriptor descriptor,
                                    Location location,
                                    object[]? messageFormatArgs
#if STMG_DEBUG_MESSAGE
                                    ,
                                    [CallerMemberName] string? memberName = null,
                                    [CallerLineNumber] int lineNumber = -1
#endif
            )
        {
#pragma warning disable CS0162

            // to allow Visual Studio refactoring features work in release code path
            if (
#if STMG_DEBUG_MESSAGE
                true
#else
                false
#endif
            )
            {
#if STMG_DEBUG_MESSAGE
                reportMethod.Invoke(Diagnostic.Create(
                    Rule_DebugWarn,
                    location,
                    $"\n{memberName} (#{lineNumber})\n{string.Format(descriptor.MessageFormat.ToString(), messageFormatArgs ?? Array.Empty<object>())}"
                    ));
#endif
            }
            else
            {
                reportMethod.Invoke(Diagnostic.Create(
                    descriptor,
                    location,
                    messageFormatArgs ?? Array.Empty<object>()
                    ));
            }
#pragma warning restore
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string NormalizeTextWithEllipsis(string? input)
        {
            return input?.Length > 72
                ? input.Substring(0, 72) + "..."
                : input ?? "<NULL TEXT>";
        }

        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, ISymbol symbol, Location location,
            [CallerMemberName] string? callerMember = null,
            [CallerLineNumber] int lineNumber = -1
            )
        {
            ReportDebugMessage(reportMethod, $"{callerMember}\n#{lineNumber}", ImmutableArray.Create(location),
                $"Symbol: {symbol.Name} ({symbol})",
                "> " + NormalizeTextWithEllipsis(symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().ToString())
                );
        }


        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, IOperation op,
            [CallerMemberName] string? callerMember = null,
            [CallerLineNumber] int lineNumber = -1
            )
        {
            ReportDebugMessage(reportMethod, op, op.Syntax.GetLocation(), callerMember, lineNumber);
        }

        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, IOperation op, Location location,
            [CallerMemberName] string? callerMember = null,
            [CallerLineNumber] int lineNumber = -1
            )
        {
            op = UnwrapAllNullCoalesceOperation(op);

            var firstChildOp = op.Children?.FirstOrDefault();

            ReportDebugMessage(reportMethod, $"{callerMember}\n#{lineNumber}", ImmutableArray.Create(location),
                $"Op: {op.Kind} ({op.Type?.Name})",
                $"Parent: {op.Parent?.UnwrapAllNullCoalesceOperation().Kind} ({op.Parent?.Type?.Name})",
                $"Grand Parent: {op.Parent?.Parent?.UnwrapAllNullCoalesceOperation().Kind} ({op.Parent?.Parent?.Type?.Name})",
                "> " + NormalizeTextWithEllipsis(op.Syntax?.ToString()),
                $"Child: {firstChildOp?.UnwrapAllNullCoalesceOperation().Kind} ({firstChildOp?.Type?.Name})"
                );
        }


        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, SyntaxNode syntax,
            [CallerMemberName] string? callerMember = null,
            [CallerLineNumber] int lineNumber = -1
            )
        {
            ReportDebugMessage(reportMethod, syntax, syntax.GetLocation(), callerMember, lineNumber);
        }

        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, SyntaxNode syntax, Location location,
            [CallerMemberName] string? callerMember = null,
            [CallerLineNumber] int lineNumber = -1
            )
        {
            var sb = new StringBuilder();
            foreach (var child in syntax.ChildNodes())
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(child.Kind().ToString());
            }

            ReportDebugMessage(reportMethod, $"{callerMember}\n#{lineNumber}", ImmutableArray.Create(syntax.GetLocation()),
                $"Syntax: {syntax.Kind()}",
                $"Parent: {syntax.Parent?.Kind()}",
                $"Grand Parent: {syntax.Parent?.Parent?.Kind()}",
                "> " + NormalizeTextWithEllipsis(syntax.ToString()),
                $"Children: {sb}"
                );
        }


        /* =====  internal  ===== */

        [Obsolete]
        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, string title, Location location, params string[]? messages)
        {
            ReportDebugMessage(reportMethod, title, ImmutableArray.Create(location), messages);
        }

        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage<T>(Action<Diagnostic> reportMethod, string title, T locations, params string[]? messages)
            where T : IEnumerable<Location>
        {
            if (locations == null)
                return;

            messages ??= Array.Empty<string>();
            var message = messages.Length > 0 ? title + "\n" + string.Join(separator: "\n", messages) : title;

            foreach (var loc in locations)
            {
                reportMethod(Diagnostic.Create(Rule_DebugError, loc, message));
            }
        }


        [Obsolete]
        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, string title, string? message, Location location)
        {
            ReportDebugMessage(reportMethod, title, message, ImmutableArray.Create(location));
        }

        [Obsolete]
        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage<T>(Action<Diagnostic> reportMethod, string title, string? message, T locations)
            where T : IEnumerable<Location>
        {
            if (locations == null)
                return;

            message = message != null ? title + "\n" + message : title;
            foreach (var loc in locations)
            {
                reportMethod(Diagnostic.Create(Rule_DebugError, loc, message));
            }
        }


        /*  node & operation  ================================================================ */

        internal static IOperation UnwrapAllNullCoalesceOperation(this IOperation op)
        {
            // NOTE: IConditionalAccessOperation returns entire conditional access chain operation.
            return (op as IConditionalAccessOperation)?.Operation ?? op;
        }


        /*  string op  ================================================================ */

        internal static string GetMemberNamePrefix(SyntaxNode? node)
        {
            var sb = new StringBuilder();

            var parent = node?.Parent;
            while (parent != null)
            {
                switch (parent)
                {
                    case TypeDeclarationSyntax type:
                        sb.Insert(index: 0, type.Identifier.Text);
                        break;
                    case NamespaceDeclarationSyntax ns:
                        sb.Insert(index: 0, ns.Name.ToString());
                        break;

                    default:
                        break;
                }
                parent = parent.Parent;
            }

            return sb.ToString();
        }


        static readonly SymbolDisplayFormat s_diagnosticMessageFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.None,
            delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
            parameterOptions: SymbolDisplayParameterOptions.IncludeName,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            localOptions: SymbolDisplayLocalOptions.None,
            kindOptions: SymbolDisplayKindOptions.None,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string ToDiagnosticMessageName(this ISymbol symbol)
        {
            return symbol switch
            {
                INamespaceSymbol ns => ns.IsGlobalNamespace ? "global" : ns.ToDisplayString(),
                _ => symbol.ToDisplayString(s_diagnosticMessageFormat),
            };
        }

        // string.Create and Concat(ReadOnlySpan) cannot be used in .net standard 2.0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string SpanConcat(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            Span<char> buffer = stackalloc char[left.Length + right.Length];
            left.CopyTo(buffer);
            right.CopyTo(buffer.Slice(left.Length));

            return buffer.ToString();
        }


        /*  Suppression  ================================================================ */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSuppressedByComment(IOperation operation, string suppressionComment)
        {
            var parentOp = operation.Parent;

            return (parentOp is ISimpleAssignmentOperation assignOp && IsSuppressedByComment(assignOp.Syntax, suppressionComment, isDiscardOperation: assignOp.Target is IDiscardOperation))
                // NOTE: Null check is not required.
                //       Always not null if code is valid, otherwise compile error.
                //       --> Initializer -> Declarator -> Declaration -> LocalDeclaration
                || (parentOp is IVariableInitializerOperation initOp && IsSuppressedByComment(initOp.Parent.Parent.Parent.Syntax, suppressionComment))
                ;
        }

        /// <param name="isDiscardOperation">
        /// Discard assignment is only allowed to be suppressed. e.g. `_ = Foo()`
        /// </param>
        internal static bool IsSuppressedByComment(SyntaxNode? node, string suppressionComment, bool isDiscardOperation = false)
        {
            SyntaxTrivia comment = default;

            if (node is LocalDeclarationStatementSyntax
                     // Allow suppression comment "Don't dispose" on field declaration
                     or FieldDeclarationSyntax
                     // Allow suppression comment "Allow allocation" on lambda declaration
                     or LambdaExpressionSyntax
                // Discard assignment is only allowed. e.g. _ = Foo;
                || (isDiscardOperation && node is AssignmentExpressionSyntax))
            {
                foreach (var trivia in node.GetFirstToken().LeadingTrivia)
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    {
                        comment = trivia;
                        break;
                    }
                }
            }

            return comment != default
                && comment.ToString().StartsWith(suppressionComment, StringComparison.OrdinalIgnoreCase);
        }


        /*  Polyfill  ================================================================ */

        public static bool IsKnownImmutableType(ITypeSymbol? symbol)
        {
            return symbol != null && symbol.SpecialType switch
            {
                SpecialType.System_String => true,

                _ => symbol.IsReadOnly
                  || symbol.TypeKind is TypeKind.Enum
                  || symbol.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T
                                                           or SpecialType.System_Collections_Generic_IReadOnlyList_T
                                                           or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
                                                           or SpecialType.System_Collections_IEnumerable
                  || (
                        symbol.ContainingNamespace is INamespaceSymbol
                        {
                            Name: "System", ContainingNamespace: INamespaceSymbol
                            {
                                IsGlobalNamespace: true,
                            }
                        }
                        && (
                            // NOTE: int or other primitive types are NOT readonly struct.
                            //       Instead, assumes system value types are immutable.
                            symbol.IsValueType ||

                            // Known readonly reference types from System (don't include struct)
                            symbol.Name is "Uri" or "Version" or "Type"
                        )
                     ),
            };
        }
    }
}
