[![NuGet](https://img.shields.io/nuget/vpre/SatorImaging.StaticMemberAnalyzer)](https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語-789)](./README.ja.md)
[![🇨🇳](https://img.shields.io/badge/🇨🇳-简体中文-789)](./README.zh-CN.md)
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)





基于 Roslyn 的分析器，用于诊断静态字段/属性初始化以及其他问题。

- [不稳定初始化分析](#不稳定初始化分析) 检测不稳定初始化
    - 静态字段与属性声明顺序错误
    - partial 类型跨文件成员引用
    - 跨类型静态字段的 [交叉引用问题](#交叉引用问题)
- [代码审查分析](#代码审查分析) (字面量参数分析)
- [只读变量分析](#只读变量分析) 检测对局部变量/参数赋值，以及可变参数传递
- [`Enum` 分析器与代码修复提供程序](#enum-分析器与代码修复提供程序) 防止用户层面的值转换，并支持 [Kotlin 风格 Enum 模式](#kotlin-风格-enum-模式)
- [Disposable 分析器](#disposable-分析器) 检测缺少 `using` 语句
- [Async Task 分析](#async-task-分析) 检测 `Task` 或 `ValueTask` 缺少 await
- `struct` 无参构造函数误用分析
- `TSelf` 泛型类型参数与类型约束分析
- 文件头注释强制规则
- ~~对字段/属性等进行自定义消息标注与下划线~~

> [!TIP]
> 查看全部诊断规则: [**RULES.md**](RULES.md)



## 不稳定初始化分析

![Analyzer in Action](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/InAction.gif)

## `Enum` 类型分析

限制与整数之间的双向转换，彻底禁止用户代码直接进行 enum 值转换。

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)

## `TSelf` 类型参数分析

用于分析 CRTP（Curiously Recurring Template Pattern）中 `TSelf` 类型参数不匹配问题。

![TSelf Type Argument](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/GenericTypeArgTSelf.png)



## 类型、字段与属性标注 💯

> [!IMPORTANT]
> Underlining analyzer 已废弃。如需重新启用，请设置预处理符号 `STMG_ENABLE_UNDERLINING_ANALYZER` 并重新构建。

<details>

这是一个在 Visual Studio 编码时用于增强提示的附加功能。你不再需要通过 `Obsolete` 属性来标注类型、方法、字段和属性。

详见 [该章节](#标注--下划线)。


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/DrawUnderline.png)

</details>





&nbsp;

# 安装

- NuGet
	- https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer
    - ```
      PM> Install-Package SatorImaging.StaticMemberAnalyzer
      ```


## Visual Studio 2019 或更早版本

该分析器在 Visual Studio 2022 上已测试。

你也可以在更早版本的 Visual Studio 中使用。请按项目中的说明更新 `Vsix` 项目文件后再构建。





&nbsp;

# Unity 集成

该分析器可用于 Unity 2020.2 及以上版本，详见：

[Unity/README.md](Unity/README.md)





&nbsp;

# 交叉引用问题

这是一个设计层面的问题，会让初始化逻辑变得复杂，并且只在特定条件下触发初始化错误。

即使当前看起来运行正常，也必须修复，以避免在大型代码库中难以手工发现的潜在问题。静态字段初始化失败通常不会直接抛出易见错误。


```cs
class A {
    public static int Value = B.Other;
    public static int Other = 310;
}

class B {
    public static int Other = 620;
    public static int Value = A.Other;  // 结果将是 '0' 而不是 '310'
}

public static class Test
{
    public static void Main()
    {
        System.Console.WriteLine(A.Value);  // 620
        System.Console.WriteLine(A.Other);  // 310
        System.Console.WriteLine(B.Value);  // 0   👈👈👈
        System.Console.WriteLine(B.Other);  // 620

        // 当改变类成员访问顺序时，它可以正常工作 🤣
        // 详见下一节的解释
        //System.Console.WriteLine(B.Value);  // 310  👈 正确!!
        //System.Console.WriteLine(B.Other);  // 620
        //System.Console.WriteLine(A.Value);  // 620
        //System.Console.WriteLine(A.Other);  // 310
    }
}
```


**C# 编译器初始化顺序**

- `A.Value = B.Other;`
    - // 访问成员触发 `B` 初始化
    - `B.Other = 620;`
    - `B.Value = A.Other;`  // BUG: 读取未初始化 `A.Other`，结果为 0
    - // 然后把 `B.Other` 的值 620 赋给 `A.Value`
- `A.Other = 310;`  // 在这里才初始化，这个值不会回填到 B.Value


如果先读取 B 侧，初始化顺序会改变，结果也会随之变化。

- `B.Other = 620;`
- `B.Value = A.Other;`
    - // 访问成员触发 `A` 初始化
    - `A.Value = B.Other;`  // 正确: `B.Other` 已先初始化
    - `A.Other = 310;`





&nbsp;

# `Enum` 分析器与代码修复提供程序

enum 的处理很容易变得混乱。通常应避免在业务代码中直接做与整数/字符串之间的转换与解析。

该分析器可帮助你将 enum 处理集中并封装到统一的工具层中。

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)


> [!TIP]
> 可以通过注释 `// Allow enum conversion` 来抑制；详见 [通过注释抑制](#通过注释抑制) 章节


## 从混淆中排除 `Enum` 类型

提供注解与代码修复，避免混淆工具修改 enum 的字符串表示。

![Enum Code Fix](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumCodeFix.png)

> [!NOTE]
> `Obfuscation` 属性来自 C# 基础库，本身不提供混淆功能。它只是向识别该属性的混淆工具传递配置。


## Kotlin 风格 Enum 模式

> [!IMPORTANT]
> 如需启用，请设置预处理符号 `STMG_ENABLE_KOTLIN_ENUM` 并重新构建。

<details>

用于辅助实现 Kotlin 风格的 enum class 模式。

类 enum 类型要求：
- 存在 `MyEnumLike[]` 或 `ReadOnlyMemory<MyEnumLike>` 字段
    - 字段名以 `Entries` 开头（区分大小写）或以 `entries` 结尾（不区分大小写）时，会检查初始化器正确性
- 类型带 `sealed` 修饰符
- 仅允许 `private` 构造函数
- 存在名为 `Entries` 的 `public static` 成员
- 不应声明/重写 `public bool Equals`


```cs
public class EnumLike
//           ~~~~~~~~ 警告：类型缺少 sealed 修饰符且存在公开构造函数
//                          * 此警告仅在类型包含名为 'Entries' 的成员时出现
{
    public static readonly EnumLike A = new("A");
    public static readonly EnumLike B = new("B");

    public static ReadOnlySpan<EnumLike> Entries => EntriesAsMemory.Span;

    // 'Entries' 必须按声明顺序包含所有 'public static readonly' 字段
    static readonly EnumLike[] _entries = new[] { B, A };
    //                                    ~~~~~~~~~~~~~~ 顺序错误!!

    // 可以使用 'ReadOnlyMemory<T>' 代替数组
    public static readonly ReadOnlyMemory<EnumLike> EntriesAsMemory = new(new[] { A, B });


    /* ===  Kotlin 风格 enum 模板  === */

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


### 类 Enum 类型的优势

<p><details --open><summary>优势</summary>

Kotlin 风格 enum（代数数据类型）可以防止无效值被创建。

```cs
var invalid = Activator.CreateInstance(typeof(EnumLike));

if (EnumLike.A == invalid || EnumLike.B == invalid)
{
    // 永远不会执行到此代码路径
    // 每个类 enum 条目都是一个类实例，需要 ReferenceEquals 匹配
}
```


不过在 `switch` 中使用会稍显别扭。

```cs
var val = EnumLike.A;

switch (val)
{
    // 带有 case 守卫的模式匹配...!!
    case EnumLike when val == EnumLike.A:
        System.Console.WriteLine(val);
        break;

    case EnumLike when val == EnumLike.B:
        System.Console.WriteLine(val);
        break;
}

// 此模式生成相同的 AOT 编译代码
switch (val)
{
    // 无类型的 case 守卫
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

# Disposable 分析器

```cs
var d = new Disposable();
//      ~~~~~~~~~~~~~~~~ 未找到 using 语句

d = (new object()) as IDisposable;
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 在可释放类型之间转换
```


以下情况不会报警：
- 在 `return` 语句中创建实例
    - `return new Disposable();`
- 赋值给字段或属性
    - `m_field = new Disposable();`
- 在可释放类型之间转换
    - `var x = myDisposable as IDisposable;`



> [!TIP]
> 可以通过注释 `// Don't dispose` 来抑制；详见 [通过注释抑制](#通过注释抑制) 章节



## Disposable 实现分析

分析 `IDisposable` 成员是否在 `Dispose` 方法中被正确释放。

- 目标成员类型
    - 实例字段
    - *注意*: 不支持属性和 `IAsyncDisposable`
- 目标方法查找顺序
    1. `Dispose(bool)`
    2. `public void Dispose()`
    3. `IDisposable.Dispose` (显式接口实现)

> [!NOTE]
> 拥有可释放成员的类型必须实现 `IDisposable` 接口。

### 如何修复

在类的释放方法中调用被报告成员的 `Dispose()` 方法。

```cs
class Test : IDisposable
{
    private MyDisposable _field = new();
//          ~~~~~~~~~~~~ 警告: 未释放的成员

    public void Dispose()
    {
        _field.Dispose();  // OK: 现在已正确释放
    }
}
```



## 抑制 `Disposable` 分析

> [!IMPORTANT]
> 如需启用，请设置预处理符号 `STMG_ENABLE_DISPOSABLE_ANALYZER_ATTRIBUTE` 并重新构建。

<details>

若需对指定类型抑制分析，声明名为 `DisposableAnalyzerSuppressor` 的特性并加到程序集上。

```cs
[assembly: DisposableAnalyzerSuppressor(typeof(Task), typeof(Task<>))]  // 默认忽略 Task 和 Task<T>

[Conditional("DEBUG"), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute
{
    public DisposableAnalyzerSuppressor(params Type[] _) { }
}
```

</details>





&nbsp;

# Async Task 分析

分析 `Task` 或 `ValueTask`（包括其泛型版本）局部变量是否在所有代码路径中都被正确 await 或返回。

```cs
async Task Method()
{
    var t = Task.Run(...);
    //      ~~~~~~~~~~~~ Task 未被 await 或返回
}
```


> [!TIP]
> 可以通过注释 `// Don't await` 来抑制；详见 [通过注释抑制](#通过注释抑制) 章节





&nbsp;

# 通过注释抑制

在局部变量声明或弃元（discard）赋值的正上方添加以特定字符串（不区分大小写但区分空格）开头的单行注释。搜索抑制注释时会忽略空白行。

> [!NOTE]
> 此抑制方式对局部变量的初期声明和弃元赋值有效。对现有命名变量的常规赋值无法通过注释来抑制。
>
> 使用名为 `_` 的变量（例如 `var _ = new Disposable();`）不是弃元，不会被注释抑制。

```cs
// Don't dispose
var x = ...;

// Don't dispose
// 允许使用多个单行注释，但抑制注释必须是第一行。
// 这是因为分析器会查找该标记的第一个注释琐事（trivia）。
var x = ...;

// 以下代码不会被抑制，因为它不是第一个注释行。
// （搜索第一个注释时会忽略空白行）

// NOTE:
// Don't dispose
var x = ...;
```
