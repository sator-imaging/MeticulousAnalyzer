# LINQ Migration Status

## Completed

Files where `using System.Linq` has been removed (BurstLinq provides compliant methods):

- `src/analysis/Analyzers/LambdaAnalyzer.cs`
- `src/codefix/Providers/EnumObfuscationCodeFixProvider.cs`
- `src/codefix/Providers/NullSuppressionCodeFixProvider.cs`
- `src/codefix/Providers/NamedArgumentCodeFixProvider.cs`

## Remaining - Iterators Required

### `src/analysis/Analyzers/UnderliningAnalyzer.cs`

| Line | Usage | Required Iterator |
|------|-------|-------------------|
| 237 | `ctx.CodeBlock.DescendantNodesAndSelf().OfType<AttributeSyntax>()` | `OfType<TOut>(this IEnumerable<TSource>)` for non-object source types |
| 613 | `anonyOp.Children.OfType<IArgumentOperation>().ToImmutableArray()` | `OfType<TOut>(this IEnumerable<TSource>)` + `ToImmutableArray()` on OfType result |

### `src/codefix/Providers/LambdaStaticCodeFixProvider.cs`

| Line | Usage | Required Iterator |
|------|-------|-------------------|
| 47 | `node.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().FirstOrDefault()` | `OfType<TOut>(this IEnumerable<TSource>)` for non-object source + combo `OfType_FirstOrDefault` |
| 85-94 | `method.Parameters.Select(p => ...).ToArray()` | `Select<TSource, TResult>(this IReadOnlyList<TSource>, Func<TSource, TResult>)` + `.ToArray()` |
| 96-105 | `method.Parameters.Select(p => ...).ToArray()` | `Select<TSource, TResult>(this IReadOnlyList<TSource>, Func<TSource, TResult>)` + `.ToArray()` |

### `eng/DocsGen.cs`

| Line | Usage | Required Iterator |
|------|-------|-------------------|
| 99 | `analyzerInfo.Values.Select(static x => x.title.Length).DefaultIfEmpty(0).Max()` | `Select`, `DefaultIfEmpty`, `Max` |
| 102 | `analyzerInfo.Values.OrderBy(...).ThenBy(...)` | `OrderBy`, `ThenBy` |
| 136 | `root.Elements().ToArray()` | `ToArray` (already in BurstLinq but needs `IEnumerable<XElement>` to resolve) |
| 146 | `dataTags.OrderBy(...)` | `OrderBy` |
