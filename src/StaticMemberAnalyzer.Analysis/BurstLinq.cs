// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

#pragma warning disable IDE0130  // NOTE: To use in both Analyzer and Codefix
namespace SatorImaging.StaticMemberAnalyzer//.Analysis
#pragma warning restore IDE0130
{
    public static class BurstLinq
    {
        // NOTE: T[], List<T>, ArraySegment<T>, and ImmutableArray<T> implement IReadOnlyCollection<T>.
        //       but ImmutableArray<T> doesn't implement ICollection<T>.

        // NOTE: Should not use array + arraySegment combo.
        //       System.Linq is highly optimized for major collection.

        /*  ElementAtOrDefault  ================================================================ */

        public static T? ElementAtOrDefault<T>(this IEnumerable<T> source, int index)
        {
            if (source is not IReadOnlyCollection<T> roc ||
                roc.Count > index)
            {
                int current = -1;
                foreach (var item in source)
                {
                    current++;
                    if (current == index)
                    {
                        return item;
                    }
                }
            }
            return default;
        }


        /*  Where  ================================================================ */

        public static IEnumerable<T> Where<T>(this SeparatedSyntaxList<T> source, Func<T, bool> static_lambda_where) where T : SyntaxNode
        {
            int count = source is IReadOnlyCollection<T> roc ? roc.Count : -1;
            if (count == 0)
            {
                return Array.Empty<T>();
            }
            return System.Linq.Enumerable.Where(source, static_lambda_where);
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> static_lambda_where)
        {
            int count = source is IReadOnlyCollection<T> roc ? roc.Count : -1;
            if (count == 0)
            {
                return Array.Empty<T>();
            }
            return System.Linq.Enumerable.Where(source, static_lambda_where);
        }

        /* =====  + Any  ===== */

        public static bool Where_Any<T>(
            this IEnumerable<T> source,
            Func<T, bool> static_lambda_where,
            Func<T, bool> static_lambda_any
        )
        {
            foreach (var item in source)
            {
                if (static_lambda_where.Invoke(item) &&
                    static_lambda_any(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Where_Any<T>(
            this ImmutableArray<T> source,
            Func<T, bool> static_lambda_where
        )
        {
            foreach (var item in source)
            {
                if (static_lambda_where.Invoke(item))
                {
                    return true;
                }
            }
            return false;
        }


        /*  OfType  ================================================================ */

        public static IEnumerable<T> OfType<T>(this IEnumerable<object> source)
        {
            int count = source is IReadOnlyCollection<T> roc ? roc.Count : -1;
            if (count == 0)
            {
                return Array.Empty<T>();
            }
            return System.Linq.Enumerable.OfType<T>(source);
        }

        /* =====  + Where  ===== */

        public static IEnumerable<T> OfType_Where<T>(
            this ImmutableArray<ISymbol> source,
            Func<T, bool> static_lambda_where
        )
        {
            foreach (var item in source)
            {
                if (item is T match &&
                    static_lambda_where(match))
                {
                    yield return match;
                }
            }
        }

        /* =====  + FirstOrDefault  ===== */

        public static T? OfType_FirstOrDefault<T>(
            this IEnumerable<object> source
        )
        {
            foreach (var item in source)
            {
                if (item is T match)
                {
                    return match;
                }
            }
            return default;
        }

        /* =====  + Any  ===== */

        public static bool OfType_Any<T>(
            this IEnumerable<object> source
        )
        {
            foreach (var item in source)
            {
                if (item is T)
                {
                    return true;
                }
            }
            return false;
        }


        /*  ToArray  ================================================================ */

        public static T[] ToArray<T>(this IEnumerable<T> source)
        {
            int count = source is IReadOnlyCollection<T> roc ? roc.Count : -1;
            if (count == 0)
            {
                return Array.Empty<T>();
            }

            using var e = source.GetEnumerator();

            if (count < 0)
            {
                return slow(e);
                static T[] slow(IEnumerator<T> e)
                {
                    var list = new List<T>(capacity: 8);
                    while (e.MoveNext())
                    {
                        list.Add(e.Current);
                    }
                    return list.ToArray();
                }
            }

            var result = new T[count];

            int i = -1;
            while (e.MoveNext())
            {
                i++;
                result[i] = e.Current;
            }
            return result;
        }


        /*  FirstOrDefault ================================================================ */

        public static T? FirstOrDefault<T>(this ImmutableArray<T> source)
        {
            return source.IsDefaultOrEmpty ? default : source[0];
        }

        public static T? FirstOrDefault<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                return item;
            }
            return default;
        }

        public static T? FirstOrDefault<T>(this ImmutableArray<T> source, Func<T, bool> static_lambda_first_or_default)
        {
            foreach (var item in source)
            {
                if (static_lambda_first_or_default.Invoke(item))
                {
                    return item;
                }
            }
            return default;
        }

        public static T? FirstOrDefault<T>(this IEnumerable<T> source, Func<T, bool> static_lambda_first_or_default)
        {
            foreach (var item in source)
            {
                if (static_lambda_first_or_default.Invoke(item))
                {
                    return item;
                }
            }
            return default;
        }


        /*  Any  ================================================================ */

        public static bool Any<T>(this SyntaxList<T> source, Func<T, bool> static_lambda_any) where T : SyntaxNode
        {
            foreach (var item in source)
            {
                if (static_lambda_any.Invoke(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Any<T>(this ImmutableArray<T> source, Func<T, bool> static_lambda_any)
        {
            foreach (var item in source)
            {
                if (static_lambda_any.Invoke(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Any<T>(this IEnumerable<T> source, Func<T, bool> static_lambda_any)
        {
            foreach (var item in source)
            {
                if (static_lambda_any.Invoke(item))
                {
                    return true;
                }
            }
            return false;
        }


        /*  Contains  ================================================================ */

        public static bool Contains<T>(this IEnumerable<T> source, T value)
        {
            foreach (var item in source)
            {
                if (EqualityComparer<T>.Default.Equals(item, value))
                {
                    return true;
                }
            }
            return false;
        }


        /*  For the Codefix  ================================================================ */

        public static T First<T>(this ImmutableArray<T> source)
        {
            foreach (var item in source)
            {
                return item;
            }
            throw new InvalidOperationException("The source sequence is empty.");
        }

        public static TOut? SelectMany_FirstOrDefault<T, TOut>(
            this SyntaxList<T> source,
            Func<T, SeparatedSyntaxList<TOut>> static_lambda_select_many,
            Func<TOut, bool> static_lambda_first_or_default
        )
            where T : SyntaxNode
            where TOut : SyntaxNode
        {
            foreach (var item in source)
            {
                foreach (var nest in static_lambda_select_many.Invoke(item))
                {
                    if (static_lambda_first_or_default.Invoke(nest))
                    {
                        return nest;
                    }
                }
            }
            return default;
        }
    }
}
