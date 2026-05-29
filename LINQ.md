# LINQ Migration Guide

## How to Migrate

`BurstLinq` (`src/analysis/BurstLinq.cs`) provides allocation-free, indirection-less extension methods that are API-compatible with `System.Linq`.

Its namespace is `SatorImaging.StaticMemberAnalyzer` (no `.Analysis`), shared by both the Analyzer and CodeFix projects. To migrate a file:

1. Remove `using System.Linq;`
2. BurstLinq methods resolve automatically via the shared namespace
3. If a method is missing, implement it in `BurstLinq.cs` following existing patterns


## Exemptions

These locations are exempt from migration:

- `eng/` - engineering/tooling scripts (e.g. `DocsGen.cs`)
- `debug/` - debug-only code
- `test/` - test files


## Concrete Over Interface

BurstLinq overloads prefer concrete collection types over interfaces:

- `ImmutableArray<T>`
- `SyntaxList<T>`
- `SeparatedSyntaxList<T>`

This lets the compiler resolve the most specific overload and avoids boxing/indirection. Fall back to `IReadOnlyList<T>` or `IEnumerable<T>` only as a catch-all when no concrete overload applies.

Example - `Where` has separate overloads for each:

```csharp
public static Linq_Where<T, ImmutableArray<T>> Where<T>(this ImmutableArray<T> source, ...)
public static Linq_Where<T, SyntaxList<T>> Where<T>(this SyntaxList<T> source, ...)
public static Linq_Where<T, SeparatedSyntaxList<T>> Where<T>(this SeparatedSyntaxList<T> source, ...)
public static Linq_Where<T, IReadOnlyList<T>> Where<T>(this IReadOnlyList<T> source, ...)
public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ...)  // fallback
```


## Available Methods

| Method | Receiver Types | Notes |
|--------|---------------|-------|
| `ElementAtOrDefault` | `IEnumerable<T>` | Fast path for `IReadOnlyList<T>` |
| `Where` | `ImmutableArray<T>`, `SyntaxList<T>`, `SeparatedSyntaxList<T>`, `IReadOnlyList<T>`, `IEnumerable<T>` | Returns struct iterator `Linq_Where<T, TList>` (except `IEnumerable` fallback) |
| `Where_Any` | `ImmutableArray<T>`, `Linq_OfType_Where` | Fused where+any |
| `OfType` | `IEnumerable<object>` | Returns struct iterator `Linq_OfType<T>` |
| `OfType_FirstOrDefault` | `IEnumerable<object>` | Fused OfType+FirstOrDefault |
| `OfType_Any` | `IEnumerable<object>` | Fused OfType+Any |
| `OfType_Where` | `ImmutableArray<ISymbol>` | Returns struct iterator `Linq_OfType_Where` |
| `Select` | `ImmutableArray<T>` | Returns `TResult[]` directly |
| `ToArray` | `IEnumerable<T>` | |
| `FirstOrDefault` | `Linq_OfType<T>`, `ImmutableArray<T>`, `IEnumerable<T>` | With and without predicate |
| `First` | `ImmutableArray<T>` | Throws on default/empty |
| `Any` | `SyntaxList<T>`, `ImmutableArray<T>`, `IEnumerable<T>` | With and without predicate |
| `Contains` | `IEnumerable<T>` | |
| `SelectMany_FirstOrDefault` | `SyntaxList<T>` | Fused SelectMany+FirstOrDefault |


## Migrated Files

- `src/analysis/Analyzers/LambdaAnalyzer.cs`
- `src/codefix/Providers/EnumObfuscationCodeFixProvider.cs`
- `src/codefix/Providers/NullSuppressionCodeFixProvider.cs`
- `src/codefix/Providers/NamedArgumentCodeFixProvider.cs`
- `src/codefix/Providers/LambdaStaticCodeFixProvider.cs`
