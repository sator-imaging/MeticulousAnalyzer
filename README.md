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
- [Async Task Analysis](#async-task-analysis) to detect missing await on `Task` or `ValueTask`
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

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)


> [!TIP]
> can suppress by comment `// Allow enum conversion`; see [Suppression Comment](#suppression-comment) section for detail


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



> [!TIP]
> can suppress by comment `// Don't dispose`; see [Suppression Comment](#suppression-comment) section for detail



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

# Async Task Analysis

Analyze if `Task` or `ValueTask` (including their generic versions) local variables are correctly awaited or returned on all code paths.

```cs
async Task Method()
{
    var t = Task.Run(...);
    //      ~~~~~~~~~~~~ Task is not awaited or returned
}
```


> [!TIP]
> can suppress by comment `// Don't await`; see [Suppression Comment](#suppression-comment) section for detail





&nbsp;

# Suppression Comment

Add a single-line comment starting with a specific string (case-insensitive but white space sensitive) immediately before the local variable declaration or discard assignment. Blank lines are ignored when searching for the suppression comment.

> [!NOTE]
> This suppression is effective for initial local variable declarations and discard assignments. Regular assignments to existing named variables cannot be suppressed by comments.
>
> Using a variable named `_` (e.g., `var _ = new Disposable();`) is NOT a discard and will not be suppressed by the comment.

```cs
// Don't dispose
var x = ...;

// Don't dispose
// Multiple single line comments are allowed but suppression comment must be the first.
// This is because analyzer looks for the first comment trivia of the token.
var x = ...;

// The following WON'T suppress because it's not the first comment line.
// (Blank lines are ignored when searching for the first comment)

// NOTE:
// Don't dispose
var x = ...;
```
