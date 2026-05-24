using System;

namespace AnalyzerDebug;

internal class StructTests
{
    struct HasCtor
    {
        //// uncomment following line will show error
        private HasCtor(Int32 _) { }
        public HasCtor(UInt32 _) { }
        internal HasCtor(Char _) { }
    }

    struct NoCtor { }

    static void Tests()
    {
        // WARNING
        var @struct = new HasCtor();
        HasCtor anony = new();

        // OK
        var okay = new HasCtor(0);
        HasCtor okay2 = new('A');

        // allowed due to no constructor is declared
        var allowed = new NoCtor();
        NoCtor allowed2 = new();
    }


    struct MutableStruct { }
    readonly struct ReadOnlyStruct { }

    class StructFieldTest
    {
        readonly ReadOnlyStruct readOnlyStruct;
        readonly MutableStruct mutableStruct;
    }
}

struct MutableStruct_ReadOnlyPrimitifeFields
{
    readonly int integer;
    readonly float number;
    readonly DateTime temporal;
    readonly string text;
}

class Class_ReadOnlyPrimitifeFields
{
    readonly int integer;
    readonly float number;
    readonly DateTime temporal;
    readonly string text;
}
