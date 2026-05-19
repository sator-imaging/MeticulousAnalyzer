// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

#pragma warning disable IDE0130  // NOTE: To use in both Analyzer and Codefix
namespace SatorImaging.StaticMemberAnalyzer//.Analysis
#pragma warning restore IDE0130
{
    public static class BurstLinq
    {
        // NOTE: T[], List<T>, ArraySegment<T>, and ImmutableArray<T> implement IReadOnlyCollection<T>.
        //       but ImmutableArray<T> or other list types from Roslyn doesn't implement ICollection<T>.

        /*  ElementAtOrDefault  ================================================================ */

        public static T? ElementAtOrDefault<T>(this IEnumerable<T> source, int index)
        {
            if (source is not IReadOnlyCollection<T> roc ||
                roc.Count > index)
            {
                if (source is IReadOnlyList<T> rolist)
                {
                    return rolist[index];
                }
                else
                {
                    return slow(source, index);
                    static T? slow(IEnumerable<T> source, int index)
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
                        return default;
                    }
                }
            }
            return default;
        }


        /*  Where  ================================================================ */

        /* =====  + Any  ===== */

        public static bool Where_Any<T>(
            this Linq_OfType_Where<ISymbol, ImmutableArray<ISymbol>, T> source,
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
            if (!source.IsDefaultOrEmpty)
            {
                foreach (var item in source)
                {
                    if (static_lambda_where.Invoke(item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /* =====  Enumerator  ===== */

        public static Linq_Where<T, SeparatedSyntaxList<T>> Where<T>(this SeparatedSyntaxList<T> source, Func<T, bool> static_lambda_where)
            where T : SyntaxNode
            => new(source, static_lambda_where);

        public static Linq_Where<T, IReadOnlyList<T>> Where<T>(this IReadOnlyList<T> source, Func<T, bool> static_lambda_where)
            => new(source, static_lambda_where);

        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> static_lambda_where)
            => System.Linq.Enumerable.Where(source, static_lambda_where);

        [StructLayout(LayoutKind.Auto)]
        public readonly struct Linq_Where<T, TList>
            where TList : IReadOnlyList<T>
        {
            readonly TList source;
            readonly Func<T, bool> predicate;
            public Linq_Where(TList source, Func<T, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }
            public Enumerator GetEnumerator() => new(source, predicate);

            public ImmutableArray<T> ToImmutableArray()
            {
                var source = this.source;
                var predicate = this.predicate;

                var builder = ImmutableArray.CreateBuilder<T>(initialCapacity: source.Count);
                for (int i = 0, count = source.Count; i < count; i++)
                {
                    var item = source[i];
                    if (predicate.Invoke(item))
                    {
                        builder.Add(item);
                    }
                }
                return builder.ToImmutableArray();
            }

            [StructLayout(LayoutKind.Auto)]
            public ref struct Enumerator
            {
                readonly TList source;
                readonly Func<T, bool> predicate;
                int index;
                public Enumerator(TList source, Func<T, bool> predicate)
                {
                    this.source = source;
                    this.predicate = predicate;
                    this.index = -1;
                }

                readonly public T Current => source[index];

                public bool MoveNext()
                {
                    var source = this.source;
                    int count = source.Count;
                    if (count > 0)
                    {
                        int index = this.index;
                        while ((++index) < count)
                        {
                            if (predicate.Invoke(source[index]))
                            {
                                this.index = index;
                                return true;
                            }
                        }
                    }
                    this.index = -1;
                    return false;
                }
            }
        }


        /*  OfType  ================================================================ */

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

        /* =====  Enumerator  ===== */

        //public static IEnumerable<T> OfType<T>(this IEnumerable<object> source) => System.Linq.Enumerable.OfType<T>(source);
        public static Linq_OfType<T> OfType<T>(this IEnumerable<object> source) => new(source);

        [StructLayout(LayoutKind.Auto)]
        public readonly struct Linq_OfType<T>
        {
            readonly IEnumerable<object> source;
            readonly bool isEmpty;
            public Linq_OfType(IEnumerable<object> source)
            {
                this.source = source;
                this.isEmpty = source is IReadOnlyCollection<object> roc && roc.Count > 0;
            }

            public Enumerator GetEnumerator() => new(source, isEmpty);

            [StructLayout(LayoutKind.Auto)]
            public ref struct Enumerator //: IDisposable
            {
                IEnumerator<object>? mut_enumerator;
                public Enumerator(IEnumerable<object> e, bool isEmpty)
                {
                    this.mut_enumerator = isEmpty ? null : e.GetEnumerator();
                    Current = (((default)))!;
                }

                //readonly public void Dispose() => mut_enumerator?.Dispose();
                public T Current { private set; get; }

                public bool MoveNext()
                {
                    var e = mut_enumerator;
                    if (e is not null)
                    {
                        while (e.MoveNext())
                        {
                            if (e.Current is T match)
                            {
                                Current = match;
                                return true;
                            }
                        }
                    }
                    Current = (((default)))!;
                    return false;
                }
            }
        }

        /* =====  Enumerator (+ Where)  ===== */

        public static Linq_OfType_Where<ISymbol, ImmutableArray<ISymbol>, T> OfType_Where<T>(
            this ImmutableArray<ISymbol> source,
            Func<T, bool> static_lambda_where
        )
        => new(source, static_lambda_where);

        [StructLayout(LayoutKind.Auto)]
        public readonly struct Linq_OfType_Where<TSource, TList, TOut>
            where TList : IReadOnlyList<TSource>
        {
            readonly TList source;
            readonly Func<TOut, bool> predicate;
            public Linq_OfType_Where(TList source, Func<TOut, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }

            public Enumerator GetEnumerator() => new(source, predicate);

            [StructLayout(LayoutKind.Auto)]
            public ref struct Enumerator
            {
                readonly TList source;
                readonly Func<TOut, bool> predicate;
                int index;
                public Enumerator(TList source, Func<TOut, bool> predicate)
                {
                    this.source = source;
                    this.predicate = predicate;
                    this.index = -1;
                    this.Current = (((default)))!;
                }

                public TOut Current { private set; get; }

                public bool MoveNext()
                {
                    var source = this.source;
                    var predicate = this.predicate;

                    int count = source.Count;
                    int index = this.index;
                    while ((++index) < count)
                    {
                        if (source[index] is TOut match &&
                            predicate.Invoke(match))
                        {
                            this.index = index;
                            this.Current = match;
                            return true;
                        }
                    }
                    this.index = -1;
                    this.Current = (((default)))!;
                    return false;
                }
            }
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
            if (!source.IsDefaultOrEmpty)
            {
                foreach (var item in source)
                {
                    if (static_lambda_first_or_default.Invoke(item))
                    {
                        return item;
                    }
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
            if (!source.IsDefaultOrEmpty)
            {
                foreach (var item in source)
                {
                    if (static_lambda_any.Invoke(item))
                    {
                        return true;
                    }
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
            return source[0];
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
