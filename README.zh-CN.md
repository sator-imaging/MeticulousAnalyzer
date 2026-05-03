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
- [只读变量分析](#只读变量分析) 检测对局部变量/参数赋值，以及可变参数传递
- [`Enum` 分析器与代码修复提供程序](#enum-分析器与代码修复提供程序) 防止用户层面的值转换，并支持 [Kotlin 风格 Enum 模式](#kotlin-风格-enum-模式)
- [Disposable 分析器](#disposable-分析器) 检测缺少 `using` 语句
- `struct` 无参构造函数误用分析
- `TSelf` 泛型类型参数与类型约束分析
- 文件头注释强制规则
- ~~对字段/属性等进行自定义消息标注与下划线~~

> [!TIP]
> 查看全部诊断规则: [**RULES.md**](RULES.md)



## 不稳定初始化分析

![Analyzer in Action](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/InAction.gif)

## `Enum` 类型分析

限制与整数之间的双向转换，彻底禁止用户代码直接进行 enum 值转换。

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/EnumAnalyzer.png)

## `TSelf` 类型参数分析

用于分析 CRTP（Curiously Recurring Template Pattern）中 `TSelf` 类型参数不匹配问题。

![TSelf Type Argument](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/GenericTypeArgTSelf.png)



## 类型、字段与属性标注 💯

> [!IMPORTANT]
> Underlining analyzer 已废弃。如需重新启用，请设置预处理符号 `STMG_ENABLE_UNDERLINING_ANALYZER` 并重新构建。

<details>

这是一个在 Visual Studio 编码时用于增强提示的附加功能。你不再需要通过 `Obsolete` 属性来标注类型、方法、字段和属性。

详见 [该章节](#标注--下划线)。


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/DrawUnderline.png)

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

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/EnumAnalyzer.png)


## 从混淆中排除 `Enum` 类型

提供注解与代码修复，避免混淆工具修改 enum 的字符串表示。

![Enum Code Fix](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/EnumCodeFix.png)

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



## 通过注释抑制

在局部变量声明或弃元（discard）赋值的正上方添加以 `// Don't dispose`（不区分大小写但区分空格）开头的单行注释。搜索抑制注释时会忽略空白行。

```cs
// Don't dispose
var d = new MyDisposable();

// 允许使用多个单行注释，且空白行会被忽略。
// Don't dispose
// - 因为它由外部库管理

_ = new MyDisposable();

// 以下代码不会被抑制，因为它不是第一个注释行。
// （搜索第一个注释时会忽略空白行）

// Don't dispose
var d = new MyDisposable();
```

> [!NOTE]
> 此抑制方式对局部变量的初始声明和弃元赋值有效。对现有命名变量的常规赋值无法通过注释来抑制。
>
> 使用名为 `_` 的变量（例如 `var _ = new Disposable();`）不是弃元，不会被注释抑制。



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

# 只读变量分析

该分析器通过标记写操作，帮助保持局部变量和参数的不可变性。

> [!IMPORTANT]
> 该分析默认情况下处于禁用状态。若要启用它，请将以下内容添加到 `.editorconfig` 文件。
>
> ```
> [*.cs]
> dotnet_analyzer_diagnostic.category-ImmutableVariable.severity = warning
> ```

<details>

- 赋值
    - `=`
    - `??=`
    - `= ref`
    - 解构赋值: `(x, y) = ...` / `(x, var y) = ...`
        - 允许解构声明赋值: `var (x, y) = ...`
    - *注*: 对 `out` 参数赋值始终允许
- 自增/自减
    - `++x`, `x++`, `--x`, `x--`
- 循环头中的特殊处理
    - 允许: `for` 循环头中的赋值和自增/自减
    - 允许: `while` 循环条件中的简单赋值
- 复合赋值
    - `+=`, `-=`, `*=`, `/=`, `%=`
    - `&=`, `|=`, `^=`, `<<=`, `>>=`
- 参数处理
    - 允许: 方法调用和对象创建（如 `Use(Create())`, `Use(new C())`）
    - 允许: 匿名对象和数组创建（如 `Use(new { X = 1 })`, `Use(new[] { 1, 2 })`）
    - 允许: Lambda 和匿名方法声明（如 `Use(x => x)`, `Use(delegate { })`）。请注意，函数体内部的修改操作仍会被分析和报告。
    - 允许: 调用点 `out var x` / `out T x` 声明
    - 允许: 根局部变量/参数名以 `mut_` 开头
    - 类型检查（`string` 按只读 struct 处理）
        - 允许: `IEnumerable`, `IEnumerable<T>` 和 `Enum` 类型
        - 引用类型参数（除 `string` 外）总是报告
        - struct 参数:
            - 允许: 被调用参数带 `in`
            - 允许: 被调用参数无修饰符且 struct 为 `readonly`
            - 否则报告


```cs
class Demo
{
    readonly struct ReadOnlyS { }
    struct MutableS { }

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
        result = 0;  // 允许：对 out 参数赋值

        param += 1;      // 报告：对参数赋值
        mut_param += 1;  // 允许：参数名以 mut_ 开头

        int foo = 0;
        foo = 1;     // 报告：对局部变量赋值
        foo++;       // 报告：局部变量自增

        var (x, y) = (42, 310);  // 允许：允许使用 var (...)
        (x, y) = (42, 310);      // 报告：解构赋值
        (x, var z) = (42, 310);  // 报告：混合解构会导致错误
                                    //           为了 Unity 兼容性，var z 也会报错

        // 允许：for 循环头中的赋值
        int i;
        for (i = 0; i < 10; i++)
        {
            i += 0;  // 报告：不在 for 循环头中
        }

        // 允许：while 循环头中的赋值
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            read = 0;  // 报告：不在 while 循环头中
        }

        int.TryParse("1", out var parsed);  // 允许：在调用点进行 out 声明
        int.TryParse("1", out parsed);      // 报告：out 覆盖了变量

        int.TryParse("1", out var mut_parsed);
        int.TryParse("1", out mut_parsed);  // 允许：mut_ 前缀

        int mut_counter = 0;
        mut_counter = 1;  // 允许：mut_ 前缀

        string key = "A";
        object keyObj = new object();
        var indexer = new Demo();
        _ = indexer[key];     // 允许：string 被视为只读结构体
        _ = indexer[keyObj];  // 报告：引用类型索引器键
        indexer = new();      // 报告：对局部变量赋值（引用类型）

        UseIn(s);                  // 允许：被调用参数带 in 修饰符
        UseReadOnly(rs);           // 允许：无修饰符的只读结构体
        UseRefType(Create());      // 允许：参数值为方法调用
        UseRefType(new object());  // 允许：参数值为对象创建
    }
}
```

> [!NOTE]
> 当赋值根节点是局部变量/参数时会被报告（例如 `foo.Bar.Value = 1` 中的 `foo`）。根节点是字段时不会报告。

</details>





&nbsp;

# 标注 / 下划线

> [!IMPORTANT]
> Underlining analyzer 已废弃。如需重新启用，请设置预处理符号 `STMG_ENABLE_UNDERLINING_ANALYZER` 并重新构建。

<details>

这是一个可选功能，可在类型、字段、属性、泛型类型/方法参数，以及方法/委托/Lambda 参数上绘制下划线。

由于 Visual Studio 的 UX 设计，`Info` 级别诊断下划线通常只显示在前几个字符上，而不是整个标记区域。为规避此问题，关键字处会绘制虚线下划线。


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/DrawUnderline.png)

> [!TIP]
> 消息以 `!` 开头时，会在关键字上添加 warning 标注，而不是 info 标注。


## 使用方法

为避免对该分析器产生依赖，下划线功能所需特性选用了内置的 `System.ComponentModel`，因此语法看起来会有些特殊。

分析器检查的是 C# 源码中的关键字标识，而非真实类型。只有在 C# 特性语法中使用 `DescriptionAttribute` 才会触发下划线。省略 `Attribute` 后缀或添加命名空间都不会被识别。


> [!TIP]
> `CategoryAttribute` can be used instead of `DescriptionAttribute`.
>
> 与 Description 不同，`CategoryAttribute` 只会在精确类型引用和构造函数（含 `base()`）上绘制下划线。继承类型、变量、字段和属性不会绘制。


```cs
using System.ComponentModel;

[DescriptionAttribute("Draw underline for IDE environment and show this message")]
//          ^^^^^^^^^ 需要 Attribute 后缀才能绘制下划线
public class WithUnderline
{
    [DescriptionAttribute]  // 无参形式将使用默认消息绘制下划线
    public static void Method() { }
}

// C# 语言规范允许省略 Attribute 后缀，但省略后将不会绘制下划线
// 为了避免与 VS 窗体设计器的原始设计用途冲突
[Description("No Underline")]
public class NoUnderline { }

// 指定命名空间时不会绘制下划线
[System.ComponentModel.DescriptionAttribute("...")]
public static int Underline_Not_Drawn = 0;

// 此代码将绘制下划线。允许在特性语法中添加 'Trivia'
[ /**/  DescriptionAttribute   (   "Underline will be drawn" )   /* hello, world. */   ]
public static int Underline_Drawn = 310;
```



## 详细级别控制

下划线共有 4 类：line head、line leading、line end 和 keyword。

默认情况下，静态字段分析器会绘制最详细的下划线。
你可以通过 `#pragma` 预处理指令、`SuppressMessage` 特性等方式屏蔽指定类型的下划线。


![Verbosity Control](https://raw.githubusercontent.com/sator-imaging/CSharp-StaticFieldAnalyzer/main/assets/VerbosityControl.png)



## Unity 提示

下划线功能基于 [Description](https://learn.microsoft.com/dotnet/api/system.componentmodel.descriptionattribute) 特性实现，该特性原本用于 Visual Studio 的可视化设计器（旧称 Form Designer）。

若要从 Unity 构建中移除不必要特性，请在 Unity 项目的 `Assets` 目录添加如下 `link.xml`：

```xml
<linker>
    <assembly fullname="System.ComponentModel">
        <type fullname="System.ComponentModel.DescriptionAttribute" preserve="nothing"/>
    </assembly>
</linker>
```

</details>





&nbsp;

# TODO

## Disposable 分析器

### 已知误检

- Lambda `return` 语句
    - `MethodArg(() => DisposableProperty);`
    - `MethodArg(() => { return DisposableProperty; });`
- `?:` 运算符
    - `DisposableProperty = condition ? null : disposableList[index];` 


## Enum 分析器功能
- 隐式转换抑制特性
    - `[assembly: EnumAnalyzer(SuppressImplicitCast = true)]`
        - ***不要*** 抑制转换到 `object` `Enum` `string` `int` 或其他 blittable 类型
        - （隐式转换运算符在大多数场景是有设计意图的，是否应默认抑制？）
- 允许类 Enum 类型存在仅 internal 的条目
  ```cs
  sealed class MyEnumLike
  {
      public static readonly MyEnumLike PublicEntry = new();
      internal static readonly MyEnumLike ForDebuggingPurpose = new();
  }
  ```
