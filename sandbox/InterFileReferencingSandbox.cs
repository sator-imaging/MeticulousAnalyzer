using System;

namespace AnalyzerSandbox;

internal class InterFileReferencing
{
    public readonly static int CrossRef = 10 + OtherClass.I + 20;  // error
    public readonly static Double PublicDouble = 99.99;
}

partial struct PartialStruct
{
    public readonly static int Value = InMainFile;  // error
    public readonly static int InAnotherFile = 310;
    public readonly static int OK = Value + InAnotherFile;
}
