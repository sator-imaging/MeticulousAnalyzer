# References
- When implementing codefix provider: [Read this](./src/README.md)
- When using LINQ (System.Linq): [Read this](./src/README.md#linq-migration-guide)
- When adding new tests: [Read this](./test/README.md)
- Diagnostic reporting location fallback: Analyzers must always report diagnostics; when primary locations are null or missing, fall back to context node or symbol locations rather than exiting early.

# Idioms for performance

```cs
for (int i = 0; i < list.Count; i++)
```

This is slow in .NET Standard 2.0 environment.

Change this to:

```cs
for (int i = 0, count = list.Count; i < count; i++)
```

This can reduce property access that is not inlined in older runtime. Note that array `.Length` access does not require this optimization as JIT optimizes array loops.
