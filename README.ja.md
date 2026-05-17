[![NuGet](https://img.shields.io/nuget/vpre/SatorImaging.StaticMemberAnalyzer)](https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語-789)](./README.ja.md)
[![🇨🇳](https://img.shields.io/badge/🇨🇳-简体中文-789)](./README.zh-CN.md)
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)





Roslyn ベースのアナライザーです。静的フィールド/プロパティ初期化やその他の問題を診断します。

- [初期化の不安定性解析](#初期化の不安定性解析) で不安定な初期化を検出
    - 静的フィールド/プロパティ宣言順の誤り
    - partial 型でのファイル跨ぎ参照
    - 型を跨ぐ静的フィールドの [相互参照問題](#相互参照問題)
- [コードレビュー向けの解析](#コードレビュー向けの解析) (リテラル引数の解析)
- [読み取り専用変数解析](#読み取り専用変数解析) でローカル/引数への代入と可変な引数受け渡しを検出
- [`Enum` アナライザーとコード修正プロバイダー](#enum-アナライザーとコード修正プロバイダー) でユーザー側の値変換を禁止し、[Kotlin 風 Enum パターン](#kotlin-風-enum-パターン) も検査
- [Disposable アナライザー](#disposable-アナライザー) で `using` の欠落を検出
- [Async Task 解析](#async-task-解析) で `Task` または `ValueTask` の await 欠落を検出
- `struct` の引数なしコンストラクター誤用解析
- `TSelf` ジェネリック型引数と型制約の解析
- ファイルヘッダーコメントの強制
- ~~カスタムメッセージでのフィールド/プロパティ等の注釈と下線表示~~

> [!TIP]
> 診断ルール一覧: [**RULES.md**](RULES.md)



## 初期化の不安定性解析

![Analyzer in Action](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/InAction.gif)

## `Enum` 型解析

整数との相互キャストを制限します。ユーザーコードでの enum 値変換を全面的に禁止できます。

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)

## `TSelf` 型引数解析

CRTP (Curiously Recurring Template Pattern) 向けに `TSelf` 型引数の不一致を解析します。

![TSelf Type Argument](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/GenericTypeArgTSelf.png)



## 型・フィールド・プロパティへの注釈 💯

> [!IMPORTANT]
> Underlining analyzer は廃止扱いです。再度有効化するには、プリプロセッサシンボル `STMG_ENABLE_UNDERLINING_ANALYZER` を設定して再ビルドしてください。

<details>

Visual Studio でのコーディング時に注意を引く追加機能です。型/メソッド/フィールド/プロパティへの注釈に `Obsolete` 属性を使う必要がなくなります。

[以下のセクション](#注釈--下線表示) で詳細を確認できます。


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/DrawUnderline.png)

</details>





&nbsp;

# インストール

- NuGet
	- https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer
    - ```
      PM> Install-Package SatorImaging.StaticMemberAnalyzer
      ```


## Visual Studio 2019 以前

このアナライザーは Visual Studio 2022 でテストされています。

旧バージョンの Visual Studio でも利用可能です。その場合は `Vsix` プロジェクトのメモに従って設定を更新し、ビルドしてください。





&nbsp;

# Unity 連携

このアナライザーは Unity 2020.2 以降で利用できます。詳細は次のページを参照してください。

[Unity/README.md](Unity/README.md)





&nbsp;

# 相互参照問題

これは設計上の問題で、複雑さを増やすだけでなく特定条件下でのみ初期化エラーを引き起こします。

一見動いていても、手作業では発見しづらい潜在バグの原因になるため修正が必要です。静的フィールドは初期化失敗を例外として報告しない点にも注意が必要です。


```cs
class A {
    public static int Value = B.Other;
    public static int Other = 310;
}

class B {
    public static int Other = 620;
    public static int Value = A.Other;  // '310' ではなく '0' になる
}

public static class Test
{
    public static void Main()
    {
        System.Console.WriteLine(A.Value);  // 620
        System.Console.WriteLine(A.Other);  // 310
        System.Console.WriteLine(B.Value);  // 0   👈👈👈
        System.Console.WriteLine(B.Other);  // 620

        // メンバーのアクセス順を変えると正しく動作する 🤣
        // 詳細は次項を参照
        //System.Console.WriteLine(B.Value);  // 310  👈 正しい!!
        //System.Console.WriteLine(B.Other);  // 620
        //System.Console.WriteLine(A.Value);  // 620
        //System.Console.WriteLine(A.Other);  // 310
    }
}
```


**C# Compiler Initialization Sequence**

- `A.Value = B.Other;`
    - // `B` の初期化がメンバーアクセスで開始
    - `B.Other = 620;`
    - `B.Value = A.Other;`  // BUG: 未初期化 `A.Other` を読むため 0
    - // その後 `B.Other` の値 620 を `A.Value` に代入
- `A.Other = 310;`  // ここで初期化。B.Value には反映されない


先に B 側を読むと初期化順が変わり、結果も変わります。

- `B.Other = 620;`
- `B.Value = A.Other;`
    - // `A` の初期化がメンバーアクセスで開始
    - `A.Value = B.Other;`  // 正常: `B.Other` は先に初期化済み
    - `A.Other = 310;`





&nbsp;

# `Enum` アナライザーとコード修正プロバイダー

enum の扱いは複雑になりがちです。整数/文字列への変換や文字列からの解析などをユーザーコードで直接行わないようにすると、運用を一元化しやすくなります。

このアナライザーは、アプリ中央の enum ユーティリティへ処理を集約するのに役立ちます。

![Enum Analyzer](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumAnalyzer.png)


> [!TIP]
> コメント `// Allow enum conversion` により抑制できます。詳細は [コメントによる抑制](#コメントによる抑制) セクションを参照してください


## 難読化から `Enum` 型を除外

難読化ツールによる文字列表現の変更を防ぐための注釈とコード修正を提供します。

![Enum Code Fix](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/EnumCodeFix.png)

> [!NOTE]
> `Obfuscation` 属性は C# 標準ライブラリの属性であり、単体で難読化機能を提供するものではありません。対応ツールに設定を伝えるためのものです。


## Kotlin 風 Enum パターン

> [!IMPORTANT]
> 有効化するには、プリプロセッサシンボル `STMG_ENABLE_KOTLIN_ENUM` を設定して再ビルドしてください。

<details>

Kotlin 風 enum class の実装を支援する解析です。

Enum ライク型の要件:
- `MyEnumLike[]` または `ReadOnlyMemory<MyEnumLike>` フィールドが存在
    - フィールド名が `Entries` で始まる (大文字小文字区別) か `entries` で終わる (大文字小文字非区別) 場合、初期化子の妥当性を検査
- 型に `sealed` 修飾子
- コンストラクターは `private` のみ
- `Entries` という名前の `public static` メンバーが存在
- `public bool Equals` を宣言/オーバーライドしない


```cs
public class EnumLike
//           ~~~~~~~~ 警告: sealed 修飾子がなく、公開コンストラクターが存在する
//                          * この警告は 'Entries' メンバーを持つ場合にのみ表示される
{
    public static readonly EnumLike A = new("A");
    public static readonly EnumLike B = new("B");

    public static ReadOnlySpan<EnumLike> Entries => EntriesAsMemory.Span;

    // 'Entries' はすべての 'public static readonly' フィールドを宣言順に保持する必要がある
    static readonly EnumLike[] _entries = new[] { B, A };
    //                                    ~~~~~~~~~~~~~~ 順序が正しくない!!

    // 配列の代わりに 'ReadOnlyMemory<T>' も使用可能
    public static readonly ReadOnlyMemory<EnumLike> EntriesAsMemory = new(new[] { A, B });


    /* ===  Kotlin 風 Enum テンプレート  === */

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


### Enum ライク型の利点

<p><details --open><summary>利点</summary>

Kotlin 風 enum (代数的データ型) は無効値の生成を防ぎやすくします。

```cs
var invalid = Activator.CreateInstance(typeof(EnumLike));

if (EnumLike.A == invalid || EnumLike.B == invalid)
{
    // このコードパスには到達しない
    // 各 Enum ライクエントリはクラスインスタンスであり、ReferenceEquals での一致が必要
}
```


ただし `switch` での利用は少し独特になります。

```cs
var val = EnumLike.A;

switch (val)
{
    // ケースガード付きのパターンマッチング...!!
    case EnumLike when val == EnumLike.A:
        System.Console.WriteLine(val);
        break;

    case EnumLike when val == EnumLike.B:
        System.Console.WriteLine(val);
        break;
}

// このパターンは同じ AOT コンパイル済みコードを生成する
switch (val)
{
    // 型指定なしのケースガード
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

# Disposable アナライザー

```cs
var d = new Disposable();
//      ~~~~~~~~~~~~~~~~ using 文が見つからない

d = (new object()) as IDisposable;
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Disposable 型への/からのキャスト
```


次の条件では警告を出しません:
- `return` 文でインスタンスを生成
    - `return new Disposable();`
- フィールド/プロパティへの代入
    - `m_field = new Disposable();`
- `IDisposable` 型同士のキャスト
    - `var x = myDisposable as IDisposable;`



> [!TIP]
> コメント `// Don't dispose` により抑制できます。詳細は [コメントによる抑制](#コメントによる抑制) セクションを参照してください



## Disposable 実装の解析

`IDisposable` のメンバーが `Dispose` メソッド内で正しく破棄されているかを解析します。

- 対象となるメンバー
    - インスタンスフィールド
    - *注意*: プロパティおよび `IAsyncDisposable` はサポートされていません
- 対象メソッドの検索順序
    1. `Dispose(bool)`
    2. `public void Dispose()`
    3. `IDisposable.Dispose` (明示的なインターフェース実装)

> [!NOTE]
> Disposable メンバーを持つ型は、`IDisposable` インターフェースを実装している必要があります。

### 修正方法

警告が表示されているメンバーの `Dispose()` メソッドを、クラスの破棄メソッド内で呼び出します。

```cs
class Test : IDisposable
{
    private MyDisposable _field = new();
//          ~~~~~~~~~~~~ 警告: 破棄されていないメンバー

    public void Dispose()
    {
        _field.Dispose();  // OK: 正しく破棄されるようになりました
    }
}
```



## `Disposable` 解析の抑制

> [!IMPORTANT]
> 有効化するには、プリプロセッサシンボル `STMG_ENABLE_DISPOSABLE_ANALYZER_ATTRIBUTE` を設定して再ビルドしてください。

<details>

特定型の解析を抑制するには、`DisposableAnalyzerSuppressor` という属性を定義し、アセンブリに付与します。

```cs
[assembly: DisposableAnalyzerSuppressor(typeof(Task), typeof(Task<>))]  // Task と Task<T> はデフォルトで無視される

[Conditional("DEBUG"), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute
{
    public DisposableAnalyzerSuppressor(params Type[] _) { }
}
```

</details>





&nbsp;

# Async Task 解析

`Task` または `ValueTask` (ジェネリック版を含む) のローカル変数が、すべてのコードパスで正しく await または return されているかを解析します。

```cs
async Task Method()
{
    var t = Task.Run(...);
    //      ~~~~~~~~~~~~ Task が await または return されていない
}
```


> [!TIP]
> コメント `// Don't await` により抑制できます。詳細は [コメントによる抑制](#コメントによる抑制) セクションを参照してください





&nbsp;

# コメントによる抑制

ローカル変数の宣言または破棄（discard）代入の直前に、特定の文字列（大文字小文字は区別しないが空白文字は区別する）で始まる 1 行コメントを追加します。抑制コメントを検索する際、空白行は無視されます。

> [!NOTE]
> この抑制はローカル変数の初期宣言および破棄代入に対して有効です。既存の命名された変数への通常の代入は、コメントによって抑制することはできません。
>
> `_` という名前の変数（例：`var _ = new Disposable();`）を使用することは破棄（discard）ではなく、コメントによる抑制は行われません。

```cs
// Don't dispose
var x = ...;

// Don't dispose
// 複数行の単一行コメントが許可されますが、抑制コメントが最初である必要があります。
// これは、アナライザーがトークンの最初のコメントトリビアを検索するためです。
var x = ...;

// 以下は最初のコメント行ではないため抑制されません。
// （最初のコメントを検索する際、空白行は無視されます）

// NOTE:
// Don't dispose
var x = ...;
```
