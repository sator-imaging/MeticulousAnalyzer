using System;

//[assembly: AnalyzerDebug.EnumLikePattern("Entries")]

namespace AnalyzerDebug;

//[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
//public sealed class EnumLikePatternAttribute : Attribute { public EnumLikePatternAttribute(string memberName) { } }

internal record EnumLikeRecord { readonly static EnumLikeRecord[] Entries; }
internal struct EnumLikeStruct { readonly static EnumLikeStruct[] Entries; }
internal interface IEnumLikePattern { readonly static IEnumLikePattern[] Entries; }
internal abstract class EnumLikeAbstract { readonly static EnumLikeAbstract[] Entries; }
// not able --> internal static class EnumLikeStatic { readonly static EnumLikeStatic[] Entries; }


sealed class CorrectEnumLike
{
    private CorrectEnumLike() { }

    public static readonly CorrectEnumLike A = new();
    public static readonly CorrectEnumLike B = new();

    public static ReadOnlySpan<CorrectEnumLike> Entries => rom_entries.Span;

    static readonly ReadOnlyMemory<CorrectEnumLike> rom_entries = new(new[] { A, B });
}


public class NonEnum : IEquatable<NonEnum>
{
    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();

    public bool Equals() => true;
    public bool Equals(NonEnum? other) => throw new NotImplementedException();
    bool IEquatable<NonEnum>.Equals(NonEnum? other) => throw new NotImplementedException();
}

//sealed
internal class EnumLikePattern : IEquatable<EnumLikePattern>
{
    //protected
    EnumLikePattern()
    {
    }

    // only private and protected ctor is allowed
    public EnumLikePattern(ulong _) { }
    internal EnumLikePattern(int _) { }

    // 'public static' Entries required (any type of return value is accepted)
    public static void entries() { }
    public /*static*/ void Entries() { }

    // 'public bool Equals' get warning including explicit interface implementations
    public bool Equals(EnumLikePattern? other) => true;
    public override bool Equals(object? obj) => base.Equals(obj);
    bool IEquatable<EnumLikePattern>.Equals(EnumLikePattern? other) => throw new NotImplementedException();
    // non-public, non-bool-returning methods are allowed
    internal bool Equals(int _) => true;
    public void Equals() { }

    internal class EnumLikeGeneric<T> { readonly static EnumLikeGeneric<T>[] Entries; }  // not public 'Entries'

    // all of `public readonly static` field must be included in `Entries` array initializer
    public readonly static EnumLikePattern A = new();
    public readonly static EnumLikePattern B = new();
    //// uncomment the following line to report error
    //public readonly static EnumLikePattern C = new();

    // ERROR: implicit array creation
    readonly static EnumLikePattern[] OK_Entries = new[] { A, B };  // OK
    readonly static EnumLikePattern[] NG_entries = new[] { B, A };  // invalid order
    readonly static EnumLikePattern[] _entries;
    readonly static EnumLikePattern[] _Entries = [];
    readonly static EnumLikePattern[] _0_entries = [null!];
    readonly static EnumLikePattern[] _1_Entries = new[] { A };

    // ERROR: explicitly typed array creation
    readonly static EnumLikePattern[] ___OK_Entries = new EnumLikePattern[] { A, B };  // OK
    readonly static EnumLikePattern[] ___NG_entries = new EnumLikePattern[] { B, A };  // invalid order
    readonly static EnumLikePattern[] __entries;
    readonly static EnumLikePattern[] __Entries = new EnumLikePattern[0];
    readonly static EnumLikePattern[] ___entries = new EnumLikePattern[0] { };
    readonly static EnumLikePattern[] ___0_entries = new EnumLikePattern[] { };
    readonly static EnumLikePattern[] ___1_Entries = new EnumLikePattern[] { A };

    // ERROR: ReadOnlyMemory
    readonly static ReadOnlyMemory<EnumLikePattern> Entries_ = new EnumLikePattern[0];
    readonly static ReadOnlyMemory<EnumLikePattern> Entries__ = new EnumLikePattern[0] { };
    readonly static ReadOnlyMemory<EnumLikePattern> Entries___ = new(new EnumLikePattern[] { A, A, B });

    // OK: ReadOnlyMemory creation
    readonly static ReadOnlyMemory<EnumLikePattern> Entries_______ = new[] { A, B };
    readonly static ReadOnlyMemory<EnumLikePattern> Entries______ = new(new[] { A, B });
    readonly static ReadOnlyMemory<EnumLikePattern> Entries_____ = new EnumLikePattern[] { A, B };
    readonly static ReadOnlyMemory<EnumLikePattern> Entries____ = new(new EnumLikePattern[] { A, B });

    // OK: type doesn't match
    readonly static int[] ______entries = new int[0];
    readonly static ReadOnlyMemory<int> _______entries = new int[0];

    // OK: name doesn't match
    readonly static ReadOnlyMemory<EnumLikePattern> entries___;  // targeting only when field name starting with 'Entries' (case-sensitive)
    readonly static ReadOnlyMemory<EnumLikePattern> ____entries2 = new EnumLikePattern[0];
    readonly static ReadOnlyMemory<EnumLikePattern> the_entries2 = new EnumLikePattern[0];
    readonly static ReadOnlyMemory<EnumLikePattern> ____Entries2 = new EnumLikePattern[0];
    readonly static ReadOnlyMemory<EnumLikePattern> the_Entries2 = new EnumLikePattern[0];

}
