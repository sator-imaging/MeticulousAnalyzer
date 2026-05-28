[![NuGet](https://img.shields.io/nuget/vpre/SatorImaging.StaticMemberAnalyzer)](https://www.nuget.org/packages/SatorImaging.StaticMemberAnalyzer)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語-789)](./README.ja.md)
[![🇨🇳](https://img.shields.io/badge/🇨🇳-简体中文-789)](./README.zh-CN.md)
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)





Roslyn ベースのアナライザーです。静的フィールド/プロパティ初期化やその他の問題を診断します。

- [初期化の不安定性解析](#初期化の不安定性解析) で不安定な初期化を検出
    - 型を跨ぐ静的フィールドの [相互参照問題](#相互参照問題)
- [コードレビュー向けの解析](#コードレビュー向けの解析) で名前付き引数や数値型の明示的宣言などを検査
- [読み取り専用変数解析](#読み取り専用変数解析) でローカル/引数への代入と可変な引数受け渡しを検出
- [`Enum` アナライザーとコード修正プロバイダー](#enum-アナライザーとコード修正プロバイダー) でユーザー側の値変換を禁止し、[Kotlin 風 Enum パターン](#kotlin-風-enum-パターン) も検査
- [Disposable アナライザー](#disposable-アナライザー) で `using` の欠落などを検出
- [非同期コンテキスト解析](#非同期コンテキスト解析) で `Task` または `ValueTask` の await 欠落を検出
- [構造体解析](#構造体解析) で引数なしコンストラクターの誤用などを検出
- [`TSelf` 型引数解析](#tself-型引数解析) で CRTP 等をサポート
- [ファイルヘッダーコメントの強制](RULES.md#file-structure-analysis) (詳細は [**RULES.md**](RULES.md) (英語) を参照)
- [コメントによる抑制](#コメントによる抑制) で特定の診断を無視
- ~~[型・フィールド・プロパティへの注釈](#型・フィールド・プロパティへの注釈-) でコーディング中の注意を喚起~~
- [コーディング支援](RULES.md#coding-assistance) パフォーマンスとコード品質向上のための解析を含む、全ての診断ルール一覧: [**RULES.md**](RULES.md) (英語)



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

> [!TIP]
> `sator_imaging.duck_typing_recognition = true` を設定することで、`IDisposable` の "ダックタイピング" 認識を有効にできます。詳細は [アナライザーの設定方法](#アナライザーの設定方法) を参照してください。


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

# 非同期コンテキスト解析

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

# コードレビュー向けの解析

## リテラル引数の解析

リテラル引数は、IDE の支援がない環境（Web ブラウザでのコードレビューなど）では、その意味を理解するのが困難です。名前付き引数や変数を使用することで、コードが自己文書化され、レビューが容易になります。

```cs
Foo(true, 0);
//  ~~~~  ~ リテラル引数は意味が分かりにくい

Foo(ignoreErrors: true, timeoutSeconds: 0);
//  ^^^^^^^^^^^^        ^^^^^^^^^^^^^^
//  引数の意味が自己説明的になりました！
```

> [!NOTE]
> `string`、`System.Text`、または `System.IO` のメソッドとコンストラクタは意図的に許可されています。また、最初の引数が `string` または `char` 型の場合は、名前付き引数を省略できます。メソッド呼び出しの場合に限り、最初の引数が `int` 型でも名前付き引数を省略できます。インデクサーの引数もこの解析の対象外です。
>
> なお、`null` や `default` リテラル、および boolean 式（パターンマッチングを含む。例: `foo is not null` や `x == y`）は名前付き引数の省略対象外であり、その位置や含まれる名前空間に関係なく、常に名前を指定する必要があります。
>
> (既知のアサーションおよび数学メソッドは、すべてのチェックの対象外です)


## 数値型の明示的な宣言

`sbyte` から `decimal` までのすべてのシステムプリミティブな数値型は、`var` ではなく明示的な型で宣言する必要があります。

```cs
var integer = 1;
//  ~~~~~~~
var floating = 1;
//  ~~~~~~~~ 報告: 変数は var ではなく明示的な数値型で宣言する必要があります
```

期待されるコード:

```cs
long integer = 1;
double floating = 1;
```

> [!IMPORTANT]
> この解析は `var` 宣言のみを対象とし、暗黙的な型変換は考慮しません。


## Null 抑制演算子

視覚的な注意を促し、テキストベースのトレーサビリティを向上させるため、Null 抑制演算子を使用する場合は 3 つの括弧で囲む必要があります。

```cs
var x = foo!;
//      ~~~~ 報告: Null 抑制演算子を使用する場合は 3 つの括弧で囲む必要があります
```

期待されるコード:

```cs
var x = (((foo)))!;
```

> [!TIP]
> `dotnet format analyzers --diagnostics SMA8002` を実行してコード修正を適用することで、コードベース内のすべての Null 抑制箇所を可視化できます。
>
> その後、`!` 演算子の代わりに `Debug.Assert(foo is not null);` を使用して、Release ビルドでの実行時オーバーヘッドを発生させることなく安全に抑制することを強く推奨します。





&nbsp;

# 読み取り専用変数解析

このアナライザーは、書き込み操作を検出してローカル値/引数の不変性維持を支援します。

> [!IMPORTANT]
> この解析はデフォルトで無効になっています。`sator_imaging.immutable_variable = true` を設定することで有効化できます。詳細は [アナライザーの設定方法](#アナライザーの設定方法) を参照してください。

<details>

- 代入
    - `=`
    - `??=`
    - `= ref`
    - 分解代入: `(x, y) = ...` / `(x, var y) = ...`
        - 分解「宣言」代入は許可: `var (x, y) = ...`
    - *注*: メソッド `out` 引数への代入は常に許可
- インクリメント/デクリメント
    - `++x`, `x++`, `--x`, `x--`
- ループヘッダーの特殊な扱い
    - 許可: `for` ループのヘッダー内での代入とインクリメント/デクリメント
    - 許可: `while` ループの条件式内での単純代入
- 複合代入
    - `+=`, `-=`, `*=`, `/=`, `%=`
    - `&=`, `|=`, `^=`, `<<=`, `>>=`
- プロパティへのアクセス
    - 以下の場合を除き、プロパティへのアクセスに対して警告を表示します。
        - 自動プロパティである場合。
        - ゲッターのみのプロパティである場合。
        - プロパティまたはそのゲッターに `readonly` 修飾子が付与されている場合。
- メソッドの呼び出し
    - インスタンスメソッドの呼び出しに対して、メソッドに `readonly` 修飾子が付与されていない場合に警告を表示します。
    - *注*: 参照型のメソッドには `readonly` 修飾子を付与できないため、常に報告されます。
- 引数処理
    - 許可: メソッド呼び出し/オブジェクト生成 (例: `Use(Create())`, `Use(new C())`)
    - 許可: 匿名オブジェクト/配列生成 (例: `Use(new { X = 1 })`, `Use(new[] { 1, 2 })`)
    - 許可: ラムダ式/匿名メソッド宣言 (例: `Use(x => x)`, `Use(delegate { })`)。ただし、関数内の書き込み操作は引き続き解析・報告されます。
    - 許可: 呼び出し側 `out var x` / `out T x` 宣言
    - 許可: ルートローカル/引数名が `mut_` で始まる
    - 型チェック (`string` は読み取り専用 struct 相当として扱う)
        - 許可: `IEnumerable`, `IEnumerable<T>`, `Enum` 型
        - 参照型引数 (`string` 以外) は常に報告
        - struct 引数:
            - 許可: 呼び出し先引数が `in`
            - 許可: 呼び出し先引数に修飾子なし かつ struct が `readonly`
            - それ以外は報告


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

        // 自動プロパティではない、セッターを持つプロパティ
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
        result = 0;  // 許可: out 引数への代入

        param += 1;      // 報告: 引数への代入
        mut_param += 1;  // 許可: 引数名が mut_ で始まっている

        int foo = 0;
        foo = 1;     // 報告: ローカル変数への代入
        foo++;       // 報告: ローカル変数のインクリメント

        var (x, y) = (42, 310);  // 許可: var (...) は許可される
        (x, y) = (42, 310);      // 報告: 分解代入
        (x, var z) = (42, 310);  // 報告: 混在した分解はエラーを引き起こす
                                    //           Unity との互換性のため、var z もエラーになる

        // 許可: for ヘッダー内での代入
        int i;
        for (i = 0; i < 10; i++)
        {
            i += 0;  // 報告: for ヘッダー外
        }

        // 許可: while ヘッダー内での代入
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            read = 0;  // 報告: while ヘッダー外
        }

        int.TryParse("1", out var parsed);  // 許可: 呼び出し先での out 宣言
        int.TryParse("1", out parsed);      // 報告: out が変数を上書きしている

        int.TryParse("1", out var mut_parsed);
        int.TryParse("1", out mut_parsed);  // 許可: mut_ プレフィックス

        int mut_counter = 0;
        mut_counter = 1;  // 許可: mut_ プレフィックス

        string key = "A";
        object keyObj = new object();
        var indexer = new Demo();
        _ = indexer[key];     // 許可: string は読み取り専用構造体として扱われる
        _ = indexer[keyObj];  // 報告: 参照型のインデクサーキー
        indexer = new();      // 報告: ローカル変数への代入（参照型）

        UseIn(s);                  // 許可: 呼び出し先の引数が in 修飾子付き
        UseReadOnly(rs);           // 許可: 修飾子なしの読み取り専用構造体
        UseRefType(Create());      // 許可: 引数がメソッド呼び出し
        UseRefType(new object());  // 許可: 引数がオブジェクト生成

        s.AutoProp = 1;       // 報告: 引数への代入
        _ = s.CustomProp;     // 報告: プロパティアクセスによる状態変化の可能性
        _ = s.ReadOnlyProp;   // 許可: ゲッターのみ、または自動プロパティ
        s.MutableMethod();    // 報告: メソッド呼び出しによる状態変化の可能性
        s.ReadOnlyMethod();   // 許可: readonly メソッド
    }
}
```

> [!NOTE]
> ローカル/引数をルートにしたメンバー代入 (例: `foo.Bar.Value = 1` の `foo`) は報告対象です。フィールドをルートにした場合は報告しません。

</details>





&nbsp;

# 構造体解析

構造体（`struct`）型の使用を分析し、一般的なミスやパフォーマンスの問題を防止します。

- SMA0030: Invalid Struct Constructor
    - 明示的にコンストラクターが宣言されている場合、引数なしのコンストラクターを使用すべきではありません。
- SMA0031: Mutable Struct Field marked as Read-Only
    - 可変な構造体型を `readonly` フィールドに設定すべきではありません。
- SMA0032: Implicit Boxing Conversion
    - 構造体から参照型（インターフェースを含む）への暗黙的な変換は、ボクシングを引き起こします。なお、明示的なキャストはこの解析の対象外です。

> [!TIP]
> 暗黙的なボクシング解析（SMA0032）は、コメント `// Allow boxing` により抑制できます。詳細は [コメントによる抑制](#コメントによる抑制) セクションを参照してください。


&nbsp;

# 注釈 / 下線表示

> [!IMPORTANT]
> Underlining analyzer は廃止扱いです。再度有効化するには、プリプロセッサシンボル `STMG_ENABLE_UNDERLINING_ANALYZER` を設定して再ビルドしてください。

<details>

型、フィールド、プロパティ、ジェネリック型/メソッド引数、メソッド/デリゲート/ラムダ引数に下線を描画するオプション機能です。

Visual Studio の仕様上、`Info` 重要度の下線は先頭数文字にしか描画されない場合があります。その回避として、キーワード上の下線は破線で描画されます。


![Draw Underline](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/DrawUnderline.png)

> [!TIP]
> メッセージを `!` で始めると、info ではなく warning 注釈としてキーワードに表示します。


## 使い方

このアナライザーへの依存を避けるため、下線用属性には組み込みの `System.ComponentModel` を利用します。そのため記法はやや独特です。

解析は C# の実型ではなく、ソース上の識別子キーワードを見ます。下線描画対象として認識されるのは C# 属性構文での `DescriptionAttribute` だけです。`Attribute` の省略や名前空間付き指定は認識されません。


> [!TIP]
> `CategoryAttribute` can be used instead of `DescriptionAttribute`.
>
> Description と異なり、`CategoryAttribute` は厳密な型参照とコンストラクター (`base()`) のみに下線を描画します。継承型・変数・フィールド・プロパティには適用されません。


```cs
using System.ComponentModel;

[DescriptionAttribute("Draw underline for IDE environment and show this message")]
//          ^^^^^^^^^ 下線を描画するには Attribute サフィックスが必要
public class WithUnderline
{
    [DescriptionAttribute]  // 引数なしはデフォルトメッセージで下線を描画する
    public static void Method() { }
}

// C# 仕様では Attribute サフィックスを省略できるが、省略すると下線は描画されない
// VS フォームデザイナー向けの元々の用途との競合を避けるため
[Description("No Underline")]
public class NoUnderline { }

// 名前空間が指定されている場合、下線は描画されない
[System.ComponentModel.DescriptionAttribute("...")]
public static int Underline_Not_Drawn = 0;

// このコードは下線を描画する。属性構文に 'Trivia' を含めることが可能
[ /**/  DescriptionAttribute   (   "Underline will be drawn" )   /* hello, world. */   ]
public static int Underline_Drawn = 310;
```



## 詳細度の制御

下線には 4 種類あります: line head, line leading, line end, keyword。

デフォルトでは静的フィールドアナライザーが最も詳細な下線を描画します。
`#pragma` プリプロセッサディレクティブや `SuppressMessage` 属性などで特定種類の下線を抑制できます。


![Verbosity Control](https://raw.githubusercontent.com/sator-imaging/StaticMemberAnalyzer/main/assets/VerbosityControl.png)



## Unity 向けヒント

下線表示は、Visual Studio のビジュアルデザイナー (旧 Form Designer) 向けの [Description](https://learn.microsoft.com/dotnet/api/system.componentmodel.descriptionattribute) 属性を使って実現しています。

Unity ビルドから不要属性を除去するには、Unity プロジェクトの `Assets` フォルダーに次の `link.xml` を追加してください。

```xml
<linker>
    <assembly fullname="System.ComponentModel">
        <type fullname="System.ComponentModel.DescriptionAttribute" preserve="nothing"/>
    </assembly>
</linker>
```

</details>





&nbsp;

# コメントによる抑制

ローカル変数の宣言または破棄（discard）代入の直前に、特定の文字列（大文字小文字は区別しないが空白文字は区別する）で始まる 1 行コメントを追加します。抑制コメントを検索する際、空白行は無視されます。

> [!NOTE]
> この抑制はローカル変数の初期宣言および破棄代入に対して有効です。既存の命名された変数への通常の代入は、コメントによって抑制することはできません。
>
> `_` という名前の変数（例：`var _ = new Disposable();`）を使用することは破棄（discard）ではなく、コメントによる抑制は行われません。

```cs
// Don't dispose
_ = new MyDisposable();

// Don't dispose: 複数行の単一行コメントが許可されますが、
// 抑制コメントが最初である必要があります。
var x = new MyDisposable();

// 以下は最初のコメント行ではないため抑制されません。
// （最初のコメントを検索する際、空白行は無視されます）

// Don't dispose because...
var x = new MyDisposable();
```


&nbsp;

# アナライザーの設定方法

設定は `.editorconfig` ではなく `.globalconfig` ファイルで行います。

```ini
is_global = true

# 読み取り専用変数解析
sator_imaging.immutable_variable = true

# Disposable 解析
sator_imaging.duck_typing_recognition = true
```

フォーマットの詳細については、以下を参照してください。
https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-files#format
