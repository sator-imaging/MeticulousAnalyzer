using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using NIE = System.NotImplementedException;

[assembly: AnalyzerCheck.DisposableAnalyzerSuppressor(typeof(object), typeof(AnalyzerCheck.DisposableTests.DisposableNoNoWarn))]

namespace AnalyzerCheck;

[Conditional("DEBUG"), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute { public DisposableAnalyzerSuppressor(params Type[] _) { } }


internal class DisposableTests
{
    // expect no warnings until 'void Test()'
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


    // field initializer never get warning
    ClassNoInterface classNoInterface = new();
    ClassWithInterface classWithInterface = new();
    ClassDeriveDisposable classDeriveDisposable = new();
    StructNoInterface structNoInterface = new();
    StructWithInterface structWithInterface = new();
    ClassAsyncDisposable classAsyncDisposable = new();
    StructAsyncDisposable structAsyncDisposable = new();

    static object? ObjectField;
    static string? StringField;
    static IDisposable? IDisposableField;

    // no error on return statement
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
        _ = new Disposable();  // Warning: Assigning to the variable '_' (not discarding).
                               //          Removing the above line wil solve (comment-out won't solve).
    }


    void Test(Disposable methodParam, EInt value)
    {
        // no warn if using statement found
        using Disposable localVar = new Disposable();
        using var genericDisposable = new Disposable<int>();

        // no warn because analysis on this type is suppressed by assembly attribute
        var noWarn = new DisposableNoNoWarn();

        // expect no warning even though Task and Task<T> implement IDisposable
        _ = Task.CompletedTask;
        _ = Task.CompletedTask.GetAwaiter();
        _ = new Task(() => { });
        _ = new Task<int>(() => 0);
        _ = new ValueTask(new Task(() => { }));
        _ = new ValueTask(new Task(() => { })).AsTask();
        _ = new ValueTask<int>(new Task<int>(() => 0));
        _ = new ValueTask<int>(new Task<int>(() => 0)).AsTask();

        // no error, field or property access
        var fa = DisposableArrayField[0];
        fa = DisposableArrayField[1];
        DisposableArrayField[0] = new Disposable();
        DisposableArrayField[1] = DisposableArrayField[1];
        var fl = DisposableListField[0];
        fl = DisposableListField[1];
        DisposableListField[0] = new Disposable();
        DisposableListField[1] = DisposableListField[0];
        DisposableListField.Add(new Disposable());        // expect: warning on 'create and forget'

        var flField = DisposableContainerListField[0].Disposable;
        flField = DisposableContainerListField[1].Disposable;
        var faField = DisposableContainerArrayField[0].Disposable;
        faField = DisposableContainerArrayField[1].Disposable;

        // warning on assignment to local collection variables
        var localArray = new IDisposable[] { new Disposable() };
        localArray[0] = new Disposable();
        localArray[0] = localArray[1];
        var localList = new List<IDisposable>(localArray);
        localList[0] = new Disposable();
        localList[0] = localList[1];
        localList.Add(new Disposable());


        // expect: warning
        localArray[0] = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        // expect: warning
        localList[0] = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        // expect: warning
        ObjectField = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        }
        as object
        ;

        using  // <-- comment out to show warning on switch expression
        var switchExpr = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        DisposableArrayField[0] = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };

        IDisposableField = value switch
        {
            EInt.Value => new Disposable(),
            _ => throw new Exception(),
        };


        // cast operation: won't show warning if both type are disposable
        _ = localVar as IDisposable;
        _ = (IDisposable)localVar;
        _ = methodParam as IDisposable;
        _ = (IDisposable)methodParam;
        _ = localVar as object;     // expect: warn
        _ = methodParam as object;  // expect: warn

        // show warning
        IDisposable notUsing1 = ((new object() as IDisposable))!;
        IDisposable notUsing2 = new object() as IDisposable;
        _ = (new object()) as Disposable;
        _ = (IDisposable)(new object());
        _ = (new Disposable()) as object;

        // no warn if receiver is existing instance but method which returns disposable instance
        localVar.Dispose();
        _ = localVar.ToString();
        _ = localVar.Self;
        _ = localVar.Self.ToString();
        _ = localVar.Self.Self;
        _ = localVar.Self.Self.ToString();
        // warning on method that returns disposable instance
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
        _ = localVar?.Self;
        _ = localVar?.Self?.Self;
        _ = localVar?.Self?.ToString();
        _ = localVar?.Self?.ToString();
        _ = localVar?.Self?.Self.GetSelf()?.ToString();


        // no warn in 'if' or other flow controls
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
        // warn on creation
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
            //using
            //var ret =
            return
            this.EnumValueField switch
            {
                EInt.Value => new DisposableBase(),
                EInt.Other => new DisposableDerived(310),  // check: when each switch arm return different type, it may show error

                EInt.Etcetera => throw new Exception(),
                _ => throw ExceptionField,
            };
        }

        using var usingDisposableFromMethod = GetDisposable(EInt.Other);


        // field assignment won't show warning
        IDisposableField = (((new object()) as IDisposable)!);
        IDisposableField = (new object()) as Disposable;
        IDisposableField = (IDisposable)(new object());
        //IDisposableField = (new Disposable()) as object;
        ObjectField = ((new object() as IDisposable)!);
        ObjectField = (new object()) as Disposable;
        ObjectField = (IDisposable)(new object());
        ObjectField = new Disposable() as object;   // warn: it's casted from disposable to non-disposable

        // OK: left hand is field or property
        IDisposableField = new Disposable();
        IDisposableField = new ImplicitConversion();

        // NG: don't allow `create and forget`
        ObjectField = new ImplicitConversion();
        ObjectField = new Disposable();
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
        StringField = convertibleDisposable;  // OK: assigning local variable
        s = convertibleDisposable;            // NG: implicit cast on local variable assignment

        // show warning
        IDisposable d = new ImplicitConversion();
        d = new ImplicitConversion();
        d = new Disposable();

        // assignment won't get warning including implicit cast
        d = notUsing1;
        d = convertibleDisposable;

        IDisposableField = new ClassWithInterface();
        IDisposableField = new ClassDeriveDisposable();
        IDisposableField = new StructWithInterface();
        //IDisposableField = new RefLikeAccessible();
        //ObjectField = new RefLikeAccessible();
        //ObjectField = new ClassWithInterface();
        //ObjectField = new ClassDeriveDisposable();
        //ObjectField = new StructWithInterface();

        //----------------------------------------------------------------------
        // NG: disposable not using-ed
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

        new RefLikeAccessible();
        var rla = new RefLikeAccessible();
        CreateRefLikeAccessible();
        _ = CreateRefLikeAccessible();
        MethodArg(new RefLikeAccessible());


        //----------------------------------------------------------------------
        // OK: using-ed & method accepts variable
        //----------------------------------------------------------------------

        using (new ClassWithInterface()) { }
        using var cwiOK = new ClassWithInterface();
        using (Create<ClassWithInterface>()) { }
        using var cwiMethodOK = Create<ClassWithInterface>();
        MethodArg(cwiOK);
        MethodArg(cwiMethodOK);

        using (new ClassDeriveDisposable()) { }
        using var cddOK = new ClassDeriveDisposable();
        using (Create<ClassDeriveDisposable>()) { }
        using var cddMethodOK = Create<ClassDeriveDisposable>();
        MethodArg(cddOK);
        MethodArg(cddMethodOK);

        using (new StructWithInterface()) { }
        using var swiOK = new StructWithInterface();
        using (Create<StructWithInterface>()) { }
        using var swiMethodOK = Create<StructWithInterface>();
        MethodArg(swiOK);
        MethodArg(swiMethodOK);

        using (new RefLikeAccessible()) { }
        using var rlaOK = new RefLikeAccessible();
        using (CreateRefLikeAccessible()) { }
        using var rlaMethodOK = CreateRefLikeAccessible();
        MethodArg(rlaOK);
        MethodArg(rlaMethodOK);
    }


    static async void TestAsync()
    {
        //IDisposableField = new ClassAsyncDisposable();
        //IDisposableField = new StructAsyncDisposable();
        //IDisposableField = new RefLikeAsyncDisposable();
        //ObjectField = new RefLikeAsyncDisposable();
        //ObjectField = new ClassAsyncDisposable();
        //ObjectField = new StructAsyncDisposable();

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

        new RefLikeAsyncDisposable();
        var rlad = new RefLikeAsyncDisposable();
        CreateRefLikeAsyncDisposable();
        _ = CreateRefLikeAsyncDisposable();
        MethodArg(new RefLikeAsyncDisposable());


        //----------------------------------------------------------------------
        // OK: await-using-ed
        //----------------------------------------------------------------------

        await using (new ClassAsyncDisposable()) { }
        await using var cadOK = new ClassAsyncDisposable();
        await using (Create<ClassAsyncDisposable>()) { }
        await using var cadMethodOK = Create<ClassAsyncDisposable>();
        MethodArg(cadOK);
        MethodArg(cadMethodOK);

        await using (new StructAsyncDisposable()) { }
        await using var sadOK = new StructAsyncDisposable();
        await using (Create<StructAsyncDisposable>()) { }
        await using var sadMethodOK = Create<StructAsyncDisposable>();
        MethodArg(sadOK);
        MethodArg(sadMethodOK);

        await using (new RefLikeAsyncDisposable()) { }
        await using var rladOK = new RefLikeAsyncDisposable();
        await using (CreateRefLikeAsyncDisposable()) { }
        await using var rladMethodOK = CreateRefLikeAsyncDisposable();
        MethodArg(rladOK);
        MethodArg(rladMethodOK);

    }


    static void TestNonDisposable()
    {
        //IDisposableField = new ClassNoInterface();
        //IDisposableField = new StructNoInterface();
        //IDisposableField = new RefLikeHidden();
        //IDisposableField = new RefLikeAsyncHidden();
        //ObjectField = new RefLikeHidden();
        //ObjectField = new RefLikeAsyncHidden();
        ObjectField = new ClassNoInterface();
        ObjectField = new StructNoInterface();

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
}
