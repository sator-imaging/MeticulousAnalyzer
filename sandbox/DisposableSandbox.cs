using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using NIE = System.NotImplementedException;

[assembly: AnalyzerSandbox.DisposableAnalyzerSuppressor(typeof(object), typeof(AnalyzerSandbox.DisposableTests.DisposableNoNoWarn))]

#pragma warning disable SMA0032 // Implicit Boxing Conversion
#pragma warning disable SMA0044 // Missing Dispose Implementation
#pragma warning disable SMA0045 // Missing IDisposable Interface
#pragma warning disable SMA0070 // Task Not Awaited
#pragma warning disable SMA8002 // Null suppression operation
#pragma warning disable SMA8000 // Literal should be passed as named argument

namespace AnalyzerSandbox;

[Conditional("DEBUG"), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute { public DisposableAnalyzerSuppressor(params Type[] _) { } }


internal class DisposableTests
{
    // OK: expect no warnings until 'void Test()'
    class ClassNoInterface { public void Dispose() { } }
    class ClassWithInterface : IDisposable { public void Dispose() { } }
    class ClassDeriveDisposable : ClassWithInterface { }
    struct StructNoInterface { public void Dispose() { } }
    struct StructWithInterface : IDisposable { public void Dispose() { } }
    ref struct RefLikeHidden { void Dispose() { } }
    ref struct RefLikeAccessible { internal void Dispose() { } }

    public class DisposableNoNoWarn : IDisposable { public void Dispose() { } }

    class ClassAsyncDisposable : IAsyncDisposable { public ValueTask DisposeAsync() => throw new NIE(); }
    struct StructAsyncDisposable : IAsyncDisposable { public ValueTask DisposeAsync() => throw new NIE(); }
    ref struct RefLikeAsyncDisposable { public ValueTask DisposeAsync() => throw new NIE(); }
    ref struct RefLikeAsyncHidden { ValueTask DisposeAsync() => throw new NIE(); }

    public class DisposableBase : IDisposable { public void Dispose() { } }
    public class DisposableDerived : DisposableBase
    {
        public DisposableDerived() { }
        public DisposableDerived(int _) { }
    }

    public class Disposable : IDisposable
    {
        public Disposable() { }
        public Disposable(int _) { }
        public static Disposable New => new();
        public Disposable Self => this;
        public Disposable GetSelf() => this;
        public void Dispose() { }
        public string Return() => string.Empty;
    }

    class Disposable<T> : IDisposable { public void Dispose() { } }

    class ImplicitConversion : IDisposable
    {
        public void Dispose() { }
        public class NonDisposable();
        public static implicit operator string(ImplicitConversion? self) => string.Empty;
    }


    // OK: field initializer never get warning
    ClassNoInterface classNoInterface = new();
    ClassWithInterface classWithInterface = new();
    ClassDeriveDisposable classDeriveDisposable = new();
    StructNoInterface structNoInterface = new();
    StructWithInterface structWithInterface = new();
    ClassAsyncDisposable classAsyncDisposable = new();
    StructAsyncDisposable structAsyncDisposable = new();

    static object? StaticFieldOfTypeObject;
    static string? StringField;
    static IDisposable? IDisposableField;

    // OK: no error on return statement
    Disposable MethodReturn1() => new();
    Disposable MethodReturn2() { return new Disposable(); }

    class DisposableContainer { public Disposable Disposable; }
    DisposableContainer[] DisposableContainerArrayField = new[] { new DisposableContainer(), new DisposableContainer(), };
    List<DisposableContainer> DisposableContainerListField = [new DisposableContainer(), new DisposableContainer(),];
    Disposable[] DisposableArrayField = new Disposable[2];
    List<Disposable> DisposableListField = [.. new Disposable[2]];

    [Obfuscation(Exclude = true, ApplyToMembers = true)] public enum EInt { Value, Other, Etcetera }
    EInt EnumValueField = EInt.Other;
    Exception ExceptionField = new Exception();


    void Test(Disposable methodParam, EInt value)
    {
        // OK: using statement found
        using Disposable localVar = new Disposable();
        using var genericDisposable = new Disposable<int>();

        // OK: even though Task and Task<T> implement IDisposable
        _ = Task.CompletedTask;
        _ = Task.CompletedTask.GetAwaiter();
        _ = new Task(() => { });
        _ = new Task<int>(() => 0);
        _ = new ValueTask(new Task(() => { }));
        _ = new ValueTask(new Task(() => { })).AsTask();
        _ = new ValueTask<int>(new Task<int>(() => 0));
        _ = new ValueTask<int>(new Task<int>(() => 0)).AsTask();

        // OK: field or property access
        var fa = DisposableArrayField[0];
        fa = DisposableArrayField[1];
        DisposableArrayField[0] = new Disposable();
        DisposableArrayField[1] = DisposableArrayField[1];
        var fl = DisposableListField[0];
        fl = DisposableListField[1];
        DisposableListField[0] = new Disposable();
        DisposableListField[1] = DisposableListField[0];
        // NG: but warning on 'create and forget'
        DisposableListField.Add(new Disposable());

        var flField = DisposableContainerListField[0].Disposable;
        flField = DisposableContainerListField[1].Disposable;
        var faField = DisposableContainerArrayField[0].Disposable;
        faField = DisposableContainerArrayField[1].Disposable;

        var localList = new List<IDisposable>();
        // NG: warning collection initialization with new()
        var localArray = new IDisposable[] { new Disposable() };
        // NG: warning on assignment to local collection variables (existing instance; left hand side only)
        localArray[0] = localArray[1];
        localList[0] = localList[1];
        // NG: warning on assignment to local collection (both side)
        localArray[0] = new Disposable();
        localList[0] = new Disposable();
        // NG: adding new instance to local collection variable
        localList.Add(new Disposable());

        // NG: assignment to local array (both side)
        localArray[0] = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        // NG: assignment to local list (indexer; both side)
        localList[0] = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        // NG: assign to field but casting to non-IDisposable (right hand side only)
        StaticFieldOfTypeObject = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        }
        as object
        ;


        // OK: with using statement
        using  // <-- comment out to show warning on switch expression
        var switchExpr = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        // OK: assign to field (array) is allowed
        IDisposableField = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        // OK: assign to field (array) is also allowed
        DisposableArrayField[0] = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };


        // OK: cast operation won't show warning if both type are disposable
        _ = localVar as IDisposable;
        _ = (IDisposable)localVar;
        _ = methodParam as IDisposable;
        _ = (IDisposable)methodParam;
        // NG: cast from/to non-disposable
        _ = localVar as object;
        _ = methodParam as object;
        IDisposable notUsing1 = ((new object() as IDisposable))!;
        IDisposable notUsing2 = new object() as IDisposable;
        _ = (new object()) as Disposable;
        _ = (IDisposable)(new object());
        _ = (new Disposable()) as object;

        // OK: if receiver is existing instance (property)
        localVar.Dispose();
        _ = localVar.ToString();
        _ = localVar.Self;
        _ = localVar.Self.ToString();
        _ = localVar.Self.Self;
        _ = localVar.Self.Self.ToString();
        // NG: but warning on method that returns disposable instance
        _ = localVar.GetSelf().ToString();
        _ = localVar.GetSelf().Self;
        _ = localVar.GetSelf()?.Self;
        _ = localVar.GetSelf();
        _ = localVar.GetSelf().GetSelf();
        _ = localVar.GetSelf().GetSelf().Self;
        _ = localVar.GetSelf().Self.GetSelf();
        _ = localVar.GetSelf().Self.GetSelf().Self;
        _ = localVar.Self.Self.GetSelf().Self.GetSelf()?.GetSelf();
        _ = localVar.GetSelf()?.GetSelf()?.GetSelf();
        _ = localVar?.GetSelf().Self;
        _ = localVar?.GetSelf()?.Self;
        _ = localVar?.GetSelf().ToString();
        _ = localVar?.GetSelf()?.ToString();
        _ = localVar?.Self.GetSelf();
        _ = localVar?.Self?.GetSelf();
        // OK: no warn on chained property access.
        _ = localVar?.Self;
        _ = localVar?.Self?.Self;
        _ = localVar?.Self?.ToString();
        _ = localVar?.Self?.ToString();
        // NG: but get warn on intermediate method invocation (on `.Self.GetSelf()`)
        _ = localVar?.Self?.Self.GetSelf()?.Self.Self?.Self.GetSelf()?.Self.Self?.ToString()?.ToString();


        // OK: 'if' or other flow controls
        if (localVar == null || localVar is IDisposable ID && ID is { } && ID is not object)
        {
            switch (localVar)
            {
                case Disposable X when X is IDisposable && X == null:
                    while (localVar != null)
                    {
                    }
                    do { } while (localVar != null);
                    break;
            }
        }
        // NG: but warn on instantiate
        else if (new Disposable() != null)
        {
            switch (new Disposable())
            {
                case Disposable X when X is IDisposable && X == null:
                    while (new Disposable() == null)
                    {
                    }
                    do { } while (new Disposable() != null);
                    break;
            }
        }

        DisposableBase GetDisposable(EInt _)
        {
            // create in 'return' statement won't show warning
            return this.EnumValueField switch
            {
                EInt.Value => new DisposableBase(),
                EInt.Other => new DisposableDerived(310),  // TODO: warn when switch arms return different IDisposable type

                EInt.Etcetera => throw new Exception(),
                _ => throw ExceptionField,
            };
        }

        using var usingDisposableFromMethod = GetDisposable(EInt.Other);
        using var disposable = new Disposable();

        // OK: field assignment to IDisposable won't show warning as they should be managed by field owner
        IDisposableField = (((new object()) as IDisposable)!);
        IDisposableField = (new object()) as Disposable;
        IDisposableField = (IDisposable)(new object());
        // NG: but assigning to non-disposable field get warning
        StaticFieldOfTypeObject = ((new object() as IDisposable)!);
        StaticFieldOfTypeObject = (new object()) as Disposable;
        StaticFieldOfTypeObject = (IDisposable)(new object());
        StaticFieldOfTypeObject = new Disposable();
        StaticFieldOfTypeObject = disposable;
        StaticFieldOfTypeObject = genericDisposable;

        // OK: left hand side is field or property
        IDisposableField = new Disposable();
        IDisposableField = new ImplicitConversion();

        // NG: don't allow `create and forget`
        StaticFieldOfTypeObject = new ImplicitConversion();
        StaticFieldOfTypeObject = new Disposable();
        string s = new ImplicitConversion();
        s = new ImplicitConversion();
        StringField = new ImplicitConversion();
        _ = new Disposable();
        _ = new Disposable().Self;
        _ = new Disposable().GetSelf();
        _ = new Disposable().GetSelf().ToString();

        // OK: receiver is field or property
        _ = Disposable.New;
        _ = Disposable.New.ToString();
        _ = Disposable.New?.ToString();

        using var convertibleDisposable = new ImplicitConversion();
        // NG: implicit cast to non-IDisposable
        StringField = convertibleDisposable;
        // NG: implicit cast on local variable assignment
        s = convertibleDisposable;

        // NG: right hand side show warning
        IDisposable d = new ImplicitConversion();
        d = new ImplicitConversion();
        d = new Disposable();

        // OK: assignment won't get warning including implicit cast
        d = notUsing1;
        d = convertibleDisposable;

        IDisposableField = new ClassWithInterface();
        IDisposableField = new ClassDeriveDisposable();
        IDisposableField = new StructWithInterface();

        //// OLD TEST
        //IDisposableField = new RefLikeAccessible();
        //StaticFieldOfTypeObject = new RefLikeAccessible();
        //StaticFieldOfTypeObject = new ClassWithInterface();
        //StaticFieldOfTypeObject = new ClassDeriveDisposable();
        //StaticFieldOfTypeObject = new StructWithInterface();

        //----------------------------------------------------------------------
        // NG: disposable without using statement
        //----------------------------------------------------------------------

        new ClassWithInterface();
        var cwi = new ClassWithInterface();
        Create<ClassWithInterface>();
        _ = Create<ClassWithInterface>();
        MethodArg(new ClassWithInterface());

        new ClassDeriveDisposable();
        var cdd = new ClassDeriveDisposable();
        Create<ClassDeriveDisposable>();
        _ = Create<ClassDeriveDisposable>();
        MethodArg(new ClassDeriveDisposable());

        new StructWithInterface();
        var swi = new StructWithInterface();
        Create<StructWithInterface>();
        _ = Create<StructWithInterface>();
        MethodArg(new StructWithInterface());

        //// NOTE: Duck-typing support is dropped.
        //new RefLikeAccessible();
        //var rla = new RefLikeAccessible();
        //CreateRefLikeAccessible();
        //_ = CreateRefLikeAccessible();
        //MethodArg(new RefLikeAccessible());


        //----------------------------------------------------------------------
        // OK: using-ed & method accepts variable
        //----------------------------------------------------------------------

        using (new ClassWithInterface()) { }
        using var cwiOK = new ClassWithInterface();
        using (Create<ClassWithInterface>()) { }
        using var cwiMethodOK = Create<ClassWithInterface>();
        // NG: but got warn on argument that leads cast to 'object'
        MethodArg(cwiOK);
        MethodArg(cwiMethodOK);

        using (new ClassDeriveDisposable()) { }
        using var cddOK = new ClassDeriveDisposable();
        using (Create<ClassDeriveDisposable>()) { }
        using var cddMethodOK = Create<ClassDeriveDisposable>();
        // NG: but got warn on argument that leads cast to 'object'
        MethodArg(cddOK);
        MethodArg(cddMethodOK);

        using (new StructWithInterface()) { }
        using var swiOK = new StructWithInterface();
        using (Create<StructWithInterface>()) { }
        using var swiMethodOK = Create<StructWithInterface>();
        // NG: but got warn on argument that leads cast to 'object'
        MethodArg(swiOK);
        MethodArg(swiMethodOK);

        //// NOTE: Duck-typing support is dropped.
        //using (new RefLikeAccessible()) { }
        //using var rlaOK = new RefLikeAccessible();
        //using (CreateRefLikeAccessible()) { }
        //using var rlaMethodOK = CreateRefLikeAccessible();
        //MethodArg(rlaOK);
        //MethodArg(rlaMethodOK);
    }


    static async void TestAsync()
    {
        //// OLD TEST
        //IDisposableField = new ClassAsyncDisposable();
        //IDisposableField = new StructAsyncDisposable();
        //IDisposableField = new RefLikeAsyncDisposable();
        //StaticFieldOfTypeObject = new RefLikeAsyncDisposable();
        //StaticFieldOfTypeObject = new ClassAsyncDisposable();
        //StaticFieldOfTypeObject = new StructAsyncDisposable();

        //----------------------------------------------------------------------
        // NG: not using
        //----------------------------------------------------------------------

        new ClassAsyncDisposable();
        var cad = new ClassAsyncDisposable();
        Create<ClassAsyncDisposable>();
        _ = Create<ClassAsyncDisposable>();
        MethodArg(new ClassAsyncDisposable());

        new StructAsyncDisposable();
        var sad = new StructAsyncDisposable();
        Create<StructAsyncDisposable>();
        _ = Create<StructAsyncDisposable>();
        MethodArg(new StructAsyncDisposable());

        //// NOTE: Duck-typing support is dropped.
        //new RefLikeAsyncDisposable();
        //var rlad = new RefLikeAsyncDisposable();
        //CreateRefLikeAsyncDisposable();
        //_ = CreateRefLikeAsyncDisposable();
        //MethodArg(new RefLikeAsyncDisposable());


        //----------------------------------------------------------------------
        // OK: await-using-ed
        //----------------------------------------------------------------------

        await using (new ClassAsyncDisposable()) { }
        await using var cadOK = new ClassAsyncDisposable();
        await using (Create<ClassAsyncDisposable>()) { }
        await using var cadMethodOK = Create<ClassAsyncDisposable>();
        // NG: but got warn on argument that leads cast to 'object'
        MethodArg(cadOK);
        MethodArg(cadMethodOK);

        await using (new StructAsyncDisposable()) { }
        await using var sadOK = new StructAsyncDisposable();
        await using (Create<StructAsyncDisposable>()) { }
        await using var sadMethodOK = Create<StructAsyncDisposable>();
        // NG: but got warn on argument that leads cast to 'object'
        MethodArg(sadOK);
        MethodArg(sadMethodOK);

        await using (new RefLikeAsyncDisposable()) { }
        await using var rladOK = new RefLikeAsyncDisposable();
        await using (CreateRefLikeAsyncDisposable()) { }
        await using var rladMethodOK = CreateRefLikeAsyncDisposable();
        ////// NOTE: Duck-typing support is dropped.
        //// NG: but got warn on argument that leads cast to 'object'
        //MethodArg(rladOK);
        //MethodArg(rladMethodOK);

    }


    static void TestNonDisposable()
    {
        //// OLD TEST
        //IDisposableField = new ClassNoInterface();
        //IDisposableField = new StructNoInterface();
        //IDisposableField = new RefLikeHidden();
        //IDisposableField = new RefLikeAsyncHidden();
        //StaticFieldOfTypeObject = new RefLikeHidden();
        //StaticFieldOfTypeObject = new RefLikeAsyncHidden();
        StaticFieldOfTypeObject = new ClassNoInterface();
        StaticFieldOfTypeObject = new StructNoInterface();

        //----------------------------------------------------------------------
        // OK: not disposable
        //----------------------------------------------------------------------

        new ClassNoInterface();
        var cni = new ClassNoInterface();
        Create<ClassNoInterface>();
        _ = Create<ClassNoInterface>();
        MethodArg(new ClassNoInterface());

        new StructNoInterface();
        var sni = new StructNoInterface();
        Create<StructNoInterface>();
        _ = Create<StructNoInterface>();
        MethodArg(new StructNoInterface());

        new RefLikeHidden();
        var rlh = new RefLikeHidden();
        CreateRefLikeHidden();
        _ = CreateRefLikeHidden();
        MethodArg(new RefLikeHidden());

        new RefLikeAsyncHidden();
        var rlah = new RefLikeAsyncHidden();
        CreateRefLikeAsyncHidden();
        _ = CreateRefLikeAsyncHidden();
        MethodArg(new RefLikeAsyncHidden());
    }


    static void MethodArg(object _) { }
    static void MethodArg(RefLikeHidden _) { }
    static void MethodArg(RefLikeAccessible _) { }
    static void MethodArg(RefLikeAsyncDisposable _) { }
    static void MethodArg(RefLikeAsyncHidden _) { }

    static T Create<T>() where T : new() => new();
    static RefLikeHidden CreateRefLikeHidden() => new();
    static RefLikeAccessible CreateRefLikeAccessible() => new();
    static RefLikeAsyncDisposable CreateRefLikeAsyncDisposable() => new();
    static RefLikeAsyncHidden CreateRefLikeAsyncHidden() => new();


    void SuppressionTestAlpha()
    {
        // Don't dispose: the following line don't show error.
        var x = new DisposableNoNoWarn();

        // Don't dispose: Discard can have suppression comment
        _ = new DisposableNoNoWarn();
    }

    void SuppressionTestBravo()
    {
        IDisposable _;

        // Don't dispose
        _ = new Disposable();  // NG: Assigning to the variable named '_' (not discarding).
                               //     **DELETING** the above declaration wil solve (comment-out won't solve because "Don't dispose" is not the first line).
    }
}
