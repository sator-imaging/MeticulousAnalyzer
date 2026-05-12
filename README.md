[![NuGet](https://img.shields.io/nuget/vpre/SatorImaging.StaticMemberAnalyzer)](https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語-789)](./README.ja.md)
[![🇨🇳](https://img.shields.io/badge/🇨🇳-简体中文-789)](./README.zh-CN.md)
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)





Roslyn-based analyzer to provide diagnostics of static fields and properties initialization and more.

- [Flaky Initialization Analysis](#flaky-initialization-analysis) detects flaky initialization
    - Wrong order of static field and property declaration
    - Partial type member reference across files
    - [Cross-Referencing Problem](#cross-referencing-problem) of static field across type
- [Analysis for Code Review](#analysis-for-code-review) (Literal Argument Analysis)
- [Immutable/Read-Only Variable Analysis](#read-only-variable-analysis) detects assignment to locals/parameters and writable call-site argument passing
- [`Enum` Type Analysis](#enum-analyzer-and-code-fix-provider) to prevent user-level value conversion & [more](#kotlin-like-enum-pattern)
- [`Disposable` Analysis](#disposable-analyzer) to detect missing using statement
- `struct` parameter-less constructor misuse analysis
- `TSelf` generic type argument & type constraint analysis
- File header comment enforcement
- ~~Annotating and underlining field, property or etc with custom message~~

> [!TIP]
> Find out all diagnostic rules: [**RULES.md**](RULES.md)



## Flaky Initialization Analysis

![Analyzer in Action](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/InAction.gif)

## Enum Type Analysis

Restrict both cast from/to integer number! Disallow user-level enum value conversion completely!!

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)

## `TSelf` Type Argument Analysis

Analyze `TSelf` type argument mismatch for Curiously Recurring Template Pattern (CRTP).

![TSelf Type Argument](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/GenericTypeArgTSelf.png)



## Annotation for Type, Field and Property 💯

> [!IMPORTANT]
> Underlining analyzer is obsolete: to enable it again, set the preprocessor symbol `STMG_ENABLE_UNDERLINING_ANALYZER` and rebuild.

<details>

There is fancy extra feature to take your attention while coding in Visual Studio. No more need to use `Obsolete` attribute in case of annotating types, methods, fields and properties.

See [the following section](#annotating--underlining) for details.


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/DrawUnderline.png)

</details>





&nbsp;

# Installation

- NuGet
	- https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer
    - ```
      PM> Install-Package SatorImaging.StaticMemberAnalyzer
      ```


## Visual Studio 2019 or Earlier

Analyzer is tested on Visual Studio 2022.

You could use this analyzer on older versions of Visual Studio. To do so, update `Vsix` project file by following instructions written in memo and build project.





&nbsp;

# Unity Integration

This analyzer can be used with Unity 2020.2 or above. See the following page for detail.

[Unity/README.md](Unity/README.md)





&nbsp;

# Cross-Referencing Problem

It is a design bug makes all things complex. Not only that but also it causes initialization error only when meet a specific condition.

So it must be fixed even if app works correctly at a moment, to prevent simple but complicated potential bug which is hard to find in large code base by hand. As you know static fields will never report error when initialization failed!!


```cs
class A {
    public static int Value = B.Other;
    public static int Other = 310;
}

class B {
    public static int Other = 620;
    public static int Value = A.Other;  // will be '0' not '310'
}

public static class Test
{
    public static void Main()
    {
        System.Console.WriteLine(A.Value);  // 620
        System.Console.WriteLine(A.Other);  // 310
        System.Console.WriteLine(B.Value);  // 0   👈👈👈
        System.Console.WriteLine(B.Other);  // 620

        // when changing class member access order, it works correctly 🤣
        // see the following section for detailed explanation
        //System.Console.WriteLine(B.Value);  // 310  👈 correct!!
        //System.Console.WriteLine(B.Other);  // 620
        //System.Console.WriteLine(A.Value);  // 620
        //System.Console.WriteLine(A.Other);  // 310
    }
}
```


**C# Compiler Initialization Sequence**

- `A.Value = B.Other;`
    - // 'B' initialization is started by member access
    - `B.Other = 620;`
    - `B.Value = A.Other;`  // BUG: B.Value will be 0 because reading uninitialized `A.Other`
    - // then, assign `B.Other` value (620) to `A.Value`
- `A.Other = 310;`  // initialized here!! this value is not assigned to B.Value


When reading B value first, initialization order is changed and resulting value is also changed accordingly:

- `B.Other = 620;`
- `B.Value = A.Other;`
    - // 'A' initialization is started by member access
    - `A.Value = B.Other;`  // correct: B.Other is initialized before reading value
    - `A.Other = 310;`





&nbsp;

# `Enum` Analyzer and Code Fix Provider

Enum type handling is really headaching. To make enum operation under control, good to avoid user-level enum handling such as converting to integer or string, parse from string and etc.

This analyzer will help centerizing and encapsulating enum handling in app's central enum utility.

> [!NOTE]
> `HasFlag` method is intentionally allowed to be invoked.

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)


**Suppress by Comment**

Add a single-line comment starting with `// Allow enum conversion` (case-insensitive but white space sensitive) immediately before the local variable declaration. Blank lines are ignored when searching for the suppression comment.

```cs
// Allow enum conversion
var x1 = (ETest)1;

// Allow enum conversion: because it is managed by external library.
// - Multiple single line comments are allowed but '// Allow enum conversion' must be the first.
var x2 = ETest.Value.ToString();

// The following WON'T suppress because it's not the first comment line.
// (Blank lines are ignored when searching for the first comment)

// Allow enum conversion
var x = (ETest)1;
```

> [!NOTE]
> This suppression is effective for initial local variable declarations. Regular assignments and discard assignments to existing variables cannot be suppressed by comments.


## Excluding Enum Type from Obfuscation

Helpful annotation and code fix for enum types which prevents modification of string representation by obfuscation tool.

![Enum Code Fix](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumCodeFix.png)

> [!NOTE]
> `Obfuscation` attribute is from C# base library and it does NOT provide feature to obfuscate compiled assembly. It just provides configuration option to obfuscation tools which recognizing this attribute.


## Kotlin-like Enum Pattern

> [!IMPORTANT]
> To use this feature, set the preprocessor symbol `STMG_ENABLE_KOTLIN_ENUM` and rebuild.

<details>

Analysis to help implementing Kotlin-style enum class.

Here are Enum-like type requirements:
- `MyEnumLike[]` or `ReadOnlyMemory<MyEnumLike>` field(s) exist
    - analyzer will check field initializer correctness if name is starting with `Entries` (case-sensitive) or ending with `entries` (case-insensitive)
- `sealed` modifier on type
- `private` constructor only
- `public static` member called `Entries` exists
- `public bool Equals` method should not be declared/overridden


```cs
public class EnumLike
//           ~~~~~~~~ WARN: no `sealed` modifier on type and public constructor exists
//                          * this warning appears only if type has member called 'Entries'
{
    public static readonly EnumLike A = new("A");
    public static readonly EnumLike B = new("B");

    public static ReadOnlySpan<EnumLike> Entries => EntriesAsMemory.Span;

    // 'Entries' must have all of 'public static readonly' fields in declared order
    static readonly EnumLike[] _entries = new[] { B, A };
    //                                    ~~~~~~~~~~~~~~ wrong order!!

    // 'ReadOnlyMemory<T>' can be used instead of array
    public static readonly ReadOnlyMemory<EnumLike> EntriesAsMemory = new(new[] { A, B });


    /* ===  Kotlin style enum template  === */

    static int AUTO_INCREMENT = 0;  // iota

    public readonly int Ordinal;
    public readonly string Name;

    private EnumLike(string name) { Ordinal = AUTO_INCREMENT++; Name = name; }

    public override string ToString()
    {
        const string SEP = ": ";
        Span<char> span = stackalloc char[Name.Length + 11 + SEP.Length];  // 11 for int.MinValue.ToString().Length

        Ordinal.TryFormat(span, out var written);
        SEP.AsSpan().CopyTo(span.Slice(written));
        written += SEP.Length;
        Name.AsSpan().CopyTo(span.Slice(written));
        written += Name.Length;

        return span.Slice(0, written).ToString();
    }
}
```


### Benefits of Enum-like Types

<p><details --open><summary>Benefits</summary>

Kotlin-like enum (algebraic data type) can prevent invalid value creation.

```cs
var invalid = Activator.CreateInstance(typeof(EnumLike));

if (EnumLike.A == invalid || EnumLike.B == invalid)
{
    // this code path won't be reached
    // each enum like entry is a class instance and ReferenceEquals match required
}
```


Unfortunately, use in `switch` statement is a bit weird.

```cs
var val = EnumLike.A;

switch (val)
{
    // pattern matching with case guard...!!
    case EnumLike when val == EnumLike.A:
        System.Console.WriteLine(val);
        break;

    case EnumLike when val == EnumLike.B:
        System.Console.WriteLine(val);
        break;
}

// this pattern generates same AOT compiled code
switch (val)
{
    // typeless case guard
    case {} when val == EnumLike.A:
        System.Console.WriteLine(val);
        break;

    case {} when val == EnumLike.B:
        System.Console.WriteLine(val);
        break;
}
```

<!------- End of Details Tag -------></details></p>

</details>





&nbsp;

# Disposable Analyzer

```cs
var d = new Disposable();
//      ~~~~~~~~~~~~~~~~ no `using` statement found

d = (new object()) as IDisposable;
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ cast from/to disposable
```


Analyzer won't show warning in the following condition:
- instance is created on `return` statement
    - `return new Disposable();`
- assign instance to field or property
    - `m_field = new Disposable();`
- cast between disposable types
    - `var x = myDisposable as IDisposable;`



**Suppress by Comment**

Add a single-line comment starting with `// Don't dispose` (case-insensitive but white space sensitive) immediately before the local variable declaration or discard assignment. Blank lines are ignored when searching for the suppression comment.

```cs
// Don't dispose
var d = new MyDisposable();

// Don't dispose because it is managed by external library.
// - Multiple single line comments are allowed but '// Don't dispose' must be the first.
_ = new MyDisposable();

// The following WON'T suppress because it's not the first comment line.
// (Blank lines are ignored when searching for the first comment)

// Don't dispose
var d = new MyDisposable();
```

> [!NOTE]
> This suppression is effective for initial local variable declarations and discard assignments. Regular assignments to existing named variables cannot be suppressed by comments.
>
> Using a variable named `_` (e.g., `var _ = new Disposable();`) is NOT a discard and will not be suppressed by the comment.



## Disposable Implementation Analysis

Analyze if `IDisposable` members are correctly disposed of in the `Dispose` method.

- Target Member Types
    - Instance fields
    - *Note*: Properties and `IAsyncDisposable` are not supported
- Target Method Discovery Order
    1. `Dispose(bool)`
    2. `public void Dispose()`
    3. `IDisposable.Dispose` (explicit interface implementation)

> [!NOTE]
> Types with disposable members must also implement the `IDisposable` interface.

### How to Fix

Call the `Dispose()` method of the reported member within the class's disposal method.

```cs
class Test : IDisposable
{
    private MyDisposable _field = new();
//          ~~~~~~~~~~~~ WARN: undisposed member

    public void Dispose()
    {
        _field.Dispose();  // OK: now correctly disposed
    }
}
```



## Suppress `Disposable` Analysis

> [!IMPORTANT]
> To use this feature, set the preprocessor symbol `STMG_ENABLE_DISPOSABLE_ANALYZER_ATTRIBUTE` and rebuild.

<details>

To suppress analysis for specified types, declare attribute named `DisposableAnalyzerSuppressor` and add it to assembly.

```cs
[assembly: DisposableAnalyzerSuppressor(typeof(Task), typeof(Task<>))]  // Task and Task<T> are ignored by default

[Conditional("DEBUG"), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute
{
    public DisposableAnalyzerSuppressor(params Type[] _) { }
}
```

</details>





&nbsp;

# Analysis for Code Review

## Literal Argument Analysis

Literal arguments can be difficult to understand without IDE assistance, especially during code reviews in a web browser. Using named arguments or variables for literals makes the code self-documenting and easier to review.

```cs
Foo(0, 0, true);
//  ~  ~  ~~~~ literal arguments are difficult to understand

Foo(timeoutSeconds, maxThreads: 0, ignoreErrors: true);
//  ^^^^^^^^^^^^^^  ^^^^^^^^^^     ^^^^^^^^^^^^
//  Now arguments are self-explanatory!
```

> [!NOTE]
> `string`, `System.Exception` and `System.IO` methods/constructors are intentionally allowed. In addition, for method and constructor calls, the first argument can omit named argument if its type is `string` or `char`. For attributes, named argument can be omitted if there is only one argument.





&nbsp;

# Read-Only Variable Analysis

This analyzer helps keep local values and parameters immutable by flagging write operations.  

> [!IMPORTANT]
> This analysis is disabled by default. To enable it, add the following to your `.editorconfig` file.
>
> ```
> [*.cs]
> dotnet_analyzer_diagnostic.category-ImmutableVariable.severity = warning
> ```

> [!NOTE]
> Restarting the IDE may be required for `.editorconfig` changes to take effect.

<details>

- Assignment
    - `=`
    - `??=`
    - `= ref`
    - Deconstruction assignment: `(x, y) = ...` / `(x, var y) = ...`
        - Deconstruction declaration assignment is allowed: `var (x, y) = ...`
    - *Note*: Assignment to `out` method parameter is always allowed
- Increment and decrement
    - `++x`, `x++`, `--x`, `x--`
- Special handling for loop headers
    - Allowed: Assignment and increment/decrement in `for` loop header
    - Allowed: Simple assignment in `while` loop condition
- Compound assignment
    - `+=`, `-=`, `*=`, `/=`, `%=`
    - `&=`, `|=`, `^=`, `<<=`, `>>=`
- Property access
    - Warn on property access unless:
        - It's an auto-property.
        - It's a getter-only property.
        - The property or its getter is marked with the `readonly` modifier.
- Method call
    - Warn on instance method calls unless the method is marked with the `readonly` modifier.
    - *Note*: Reference type methods cannot have the `readonly` modifier and are always flagged.
- Argument handling
    - Allowed: Method invocation and object creation (e.g. `Use(Create())`, `Use(new C())`)
    - Allowed: Anonymous object and array creation (e.g. `Use(new { X = 1 })`, `Use(new[] { 1, 2 })`)
    - Allowed: Lambda and anonymous method declarations (e.g. `Use(x => x)`, `Use(delegate { })`). Note that mutations within the function body are still analyzed and reported.
    - Allowed: `out var x` / `out T x` declaration at call site
    - Allowed: Root local/parameter name starts with `mut_`
    - Type checks (`string` is treated as readonly struct)
        - Allowed: `IEnumerable`, `IEnumerable<T>` and `Enum` types
        - Reference type argument (except string) is always reported
        - Struct argument:
            - Allowed: Callee parameter has `in` modifier
            - Allowed: Callee parameter has no modifier and struct is `readonly`
            - Otherwise reported


```cs
class Demo
{
    readonly struct ReadOnlyS { }
    struct MutableS
    {
        public int AutoProp { get; set; }
        public int ReadOnlyProp => 0;
        public void MutableMethod() { }
        public readonly void ReadOnlyMethod() { }

        // Non-auto property with setter
        public int CustomProp { get => 0; set { } }
    }

    static object Create() => new object();
    static void UseRefType(object value) { }
    static void UseIn(in MutableS value) { }
    static void UseReadOnly(ReadOnlyS value) { }
    public int this[string key] => 0;
    public int this[object key] => 0;

    void Test(
        int param,
        int mut_param,
        MutableS s,
        ReadOnlyS rs,
        ref int refValue,
        out int result
    )
    {
        result = 0;  // Allowed: assignment to `out` parameter

        param += 1;      // Reported: parameter assignment
        mut_param += 1;  // Allowed: `mut_` prefix on parameter

        int foo = 0;
        foo = 1;     // Reported: local assignment
        foo++;       // Reported: local increment

        var (x, y) = (42, 310);  // Allowed: var (...) is allowed
        (x, y) = (42, 310);      // Reported: deconstruction assignment
        (x, var z) = (42, 310);  // Reported: mixed deconstruction causes error
                                    //           For Unity compatibility, `var z` also get error

        // Allowed: assignment in for-header
        int i;
        for (i = 0; i < 10; i++)
        {
            i += 0;  // Reported: not in for-header
        }

        // Allowed: assignment in while-header
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            read = 0;  // Reported: not in while-header
        }

        int.TryParse("1", out var parsed);  // Allowed: out declaration at call site
        int.TryParse("1", out parsed);      // Reported: out overwrites variable

        int.TryParse("1", out var mut_parsed);
        int.TryParse("1", out mut_parsed);  // Allowed: `mut_` prefix

        int mut_counter = 0;
        mut_counter = 1;  // Allowed: `mut_` prefix

        string key = "A";
        object keyObj = new object();
        var indexer = new Demo();
        _ = indexer[key];     // Allowed: string is treated readonly-struct
        _ = indexer[keyObj];  // Reported: reference type indexer key
        indexer = new();      // Reported: local assignment (reference type)

        UseIn(s);                  // Allowed: callee parameter is `in`
        UseReadOnly(rs);           // Allowed: readonly struct with no modifier
        UseRefType(Create());      // Allowed: argument value is invocation
        UseRefType(new object());  // Allowed: argument value is object creation

        s.AutoProp = 1;       // Reported: parameter assignment
        _ = s.CustomProp;     // Reported: property access can change state
        _ = s.ReadOnlyProp;   // Allowed: getter-only or auto-property
        s.MutableMethod();    // Reported: method call can change state
        s.ReadOnlyMethod();   // Allowed: readonly method
    }
}
```

> [!NOTE]
> Member access assignments are reported when rooted at local/parameter (e.g. `foo.Bar.Value = 1` where `foo` is local/parameter), but not when rooted at field.

</details>





&nbsp;

# Annotating / Underlining

> [!IMPORTANT]
> Underlining analyzer is obsolete: to enable it again, set the preprocessor symbol `STMG_ENABLE_UNDERLINING_ANALYZER` and rebuild.

<details>

There is optional feature to draw underline on selected types, fields, properties, generic type/method arguments and parameters of method, delegate and lambda function.

As of Visual Studio's UX design, `Info` severity diagnostic underlines are drawn only on a few leading chars, not drawn whole marked area. So for workaround, underline on keyword is dashed.


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/DrawUnderline.png)

> [!TIP]
> `!`-starting message will add warning annotation on keyword instead of info diagnostic annotation.


## How to Use

To avoid dependency to this analyzer, required attribute for underlining is chosen from builtin `System.ComponentModel` assembly so that syntax is little bit weird.

Analyzer is checking identifier keyword in C# source code, not checking actual C# type. `DescriptionAttribute` in C# attribute syntax is the only keyword to draw underline. Omitting `Attribute` or adding namespace are not recognized.


> [!TIP]
> `CategoryAttribute` can be used instead of `DescriptionAttribute`.
>
> By contrast from Description, CategoryAttribute draws underline only on exact type reference and constructors including `base()`. Any inherited types, variables, fields and properties don't get underline.


```cs
using System.ComponentModel;

[DescriptionAttribute("Draw underline for IDE environment and show this message")]
//          ^^^^^^^^^ `Attribute` suffix is required to draw underline
public class WithUnderline
{
    [DescriptionAttribute]  // parameter-less will draw underline with default message
    public static void Method() { }
}

// C# language spec allows to omit `Attribute` suffix but when omitted, underline won't be drawn
// to avoid conflict with originally designed usage for VS form designer
[Description("No Underline")]
public class NoUnderline { }

// underline won't be drawn when namespace is specified
[System.ComponentModel.DescriptionAttribute("...")]
public static int Underline_Not_Drawn = 0;

// this code will draw underline. 'Trivia' is allowed to being added in attribute syntax
[ /**/  DescriptionAttribute   (   "Underline will be drawn" )   /* hello, world. */   ]
public static int Underline_Drawn = 310;
```



## Verbosity Control

There are 4 types of underline, line head, line leading, line end and keyword.

By default, static field analyzer will draw most verbose underline.
You can omit specific type of underline by using `#pragma` preprocessor directive or adding `SuppressMessage` attribute or etc.


![Verbosity Control](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/VerbosityControl.png)



## Unity Tips

Underlining is achieved by using [Description](https://learn.microsoft.com/dotnet/api/system.componentmodel.descriptionattribute) attribute designed for Visual Studio's visual designer, formerly known as form designer.

To remove unnecessary attribute from Unity build, add the following `link.xml` file in Unity project's `Assets` folder.

```xml
<linker>
    <assembly fullname="System.ComponentModel">
        <type fullname="System.ComponentModel.DescriptionAttribute" preserve="nothing"/>
    </assembly>
</linker>
```

</details>
