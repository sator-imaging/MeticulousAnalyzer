# CodeFix Practices

- Trivia (comments, whitespace, newlines) must always be preserved. Never strip trivia from nodes being moved or replaced unless intentionally relocating it to a new position.
- Use `SeparatedSyntaxList<T>` APIs (`RemoveAt`, `Replace`, `WithArguments`) to manipulate argument lists instead of rebuilding from scratch. This preserves all original separators and their trivia.
- When preserving original comma separators between arguments, use `GetSeparator(index)` on the original list rather than generating new comma tokens.
- Use `ToMinimalDisplayString(semanticModel, position)` for type names in generated code. This respects the file's `using` directives and produces the shortest valid name.
- Use `SyntaxFacts.GetKeywordKind` to detect C# keywords used as parameter names and prefix with `@` when generating `NameColon` syntax.
- Prefer reusing existing helper methods (e.g. `TryUnwrapConversion`) over duplicating logic inline.
- Pass diagnostic properties (`ImmutableDictionary`) from the analyzer to the codefix to communicate context (e.g. `isParams` flag) without re-analyzing.
- FixAll must work across multiple files. FixAll tests should use `TestState`, `FixedState`, and `BatchFixedState` with multiple source files.
- FixAll tests must include leading and trailing trivia (comments) around diagnostic spans to verify trivia is preserved after the fix.
- Use `NumberOfIncrementalIterations` matching the total number of diagnostics when testing incremental FixAll.
- Use `ReplaceLineEndings()` on test source templates to normalize line endings across platforms.


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
- `sandbox/` - analyzer playground
- `test/` - test files


## Concrete Over Interface

BurstLinq overloads prefer concrete collection types over interfaces. Add overloads for specific struct/concrete collection types as they arise:

- `ImmutableArray<T>`, `SyntaxList<T>`, `SeparatedSyntaxList<T>`, etc.
- Other struct collections may exist in Roslyn or the codebase - don't limit to the above

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


## Benchmark

`eng/BurstLinqBenchmark.cs` is a BenchmarkDotNet-based benchmark comparing BurstLinq extension methods against `System.Linq.Enumerable` equivalents.

### How to Run

Requires .NET 10 SDK (for file-based app support).

```
dotnet run eng/BurstLinqBenchmark.cs -c Release
```

### GitHub Actions

```yaml
- run: dotnet run eng/BurstLinqBenchmark.cs -c Release -- --exporters github
- run: cat BenchmarkDotNet.Artifacts/results/*-report-github.md >> $GITHUB_STEP_SUMMARY
```
