// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SatorImaging.StaticMemberAnalyzer.Analysis
{
    //https://github.com/dotnet/roslyn/blob/main/docs/wiki/Roslyn-Overview.md#solutions-projects-documents
    public static class Core
    {
        //https://github.com/dotnet/roslyn-analyzers/blob/main/src/Utilities/Compiler/DiagnosticCategoryAndIdRanges.txt
        internal const string Category = "Usage";
        internal const string AssistanceCategory = "Coding Assistance";


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


        /*  report  ================================================================ */

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


        /*  DEBUG  ================================================================ */

        [Conditional(conditionString: "STMG_DEBUG_MESSAGE")]
        internal static void ReportDebugMessage(Action<Diagnostic> reportMethod, ISymbol symbol, Location location,
            [CallerMemberName] string? callerMember = null,
            [CallerLineNumber] int lineNumber = -1
            )
        {
            ReportDebugMessage(reportMethod, $"{callerMember}\n#{lineNumber}", ImmutableArray.Create(location),
                $"Symbol: {symbol.Name} ({symbol})",
                "> " + new string(symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().ToString().Take(count: 72).ToArray())
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
            op = UnwrapNullCoalesceOperation(op);

            ReportDebugMessage(reportMethod, $"{callerMember}\n#{lineNumber}", ImmutableArray.Create(location),
                $"Op: {op.Kind} ({op.Type?.Name})",
                $"Parent: {op.Parent?.UnwrapNullCoalesceOperation().Kind} ({op.Parent?.Type?.Name})",
                $"Grand Parent: {op.Parent?.Parent?.UnwrapNullCoalesceOperation().Kind} ({op.Parent?.Parent?.Type?.Name})",
                "> " + new string(op.Syntax?.ToString().Take(count: 72).ToArray()),
                $"Child: {op.Children?.FirstOrDefault()?.UnwrapNullCoalesceOperation().Kind} ({op.Children?.FirstOrDefault()?.Type?.Name})"
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
            ReportDebugMessage(reportMethod, $"{callerMember}\n#{lineNumber}", ImmutableArray.Create(syntax.GetLocation()),
                $"Syntax: {syntax.Kind()}",
                $"Parent: {syntax.Parent?.Kind()}",
                $"Grand Parent: {syntax.Parent?.Parent?.Kind()}",
                "> " + new string(syntax.ToString().Take(count: 72).ToArray()),
                $"Children: {string.Join(separator: ", ", syntax.ChildNodes().Select(x => x.Kind().ToString()))}"
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

        internal static SyntaxNode UnwrapParenthesizeAndNullSuppressorNodes(this SyntaxNode syntax)
        {
            while (syntax.Parent is ParenthesizedExpressionSyntax || syntax.Parent.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                syntax = syntax.Parent;
            }
            return syntax;
        }


        internal static IOperation UnwrapNullCoalesceOperation(this IOperation op)
        {
            return (op as IConditionalAccessOperation)?.Operation ?? op;
        }


        /*  string op  ================================================================ */

        [ThreadStatic, DescriptionAttribute] static StringBuilder? ts_sb;

        internal static string GetMemberNamePrefix(SyntaxNode? node)
        {
            var sb = (ts_sb ??= new());
            sb.Length = 0;  // don't clear! it will set capacity = 0 and allocated memory is gone!!

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
                }
                parent = parent.Parent;
            }

            return sb.ToString();
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

    }
}
