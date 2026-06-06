# AGENTS.md

## Cursor Cloud specific instructions

### Product overview

**Static Member Analyzer** (`SatorImaging.StaticMemberAnalyzer`) is a Roslyn-based C# analyzer library shipped as a NuGet package. There is no web server or long-running application — development is validated via `dotnet build` and `dotnet test`.

### Prerequisites

- **.NET SDK 10.x** (matches CI in `.github/workflows/test.yml`)
- NuGet restore over the network (packages from `api.nuget.org`)

### Critical MSBuild property

`Directory.Build.props` auto-references the published NuGet package. When working in this repo, always pass:

```bash
-p:DontReferenceItself=true
```

on `dotnet restore`, `dotnet build`, and `dotnet test`. Without it, restore can hit a cyclic self-reference (`NU1108`).

### Common commands

| Task | Command |
|------|---------|
| Restore | `dotnet restore -p:DontReferenceItself=true` |
| Build (Release) | `dotnet build -c Release -p:DontReferenceItself=true` |
| Test (primary validation) | `dotnet test ./test -c Debug --verbosity minimal -p:DontReferenceItself=true` |
| Package | `dotnet build ./src/packaging -c Release -p:DontReferenceItself=true` → `.nupkg` in `src/packaging/bin/Release/` |

See `test/README.md` for test naming conventions and `StaticMemberAnalyzer.slnx` for the solution layout (`src/analysis`, `src/codefix`, `src/packaging`, `test`, `debug`).

### Lint / format

- No dedicated lint workflow. Compiler and Roslyn analyzer warnings (`RS10xx`) appear during `dotnet build`.
- `dotnet format` is available with the SDK but is not used in CI. If you use it, pass `-p:DontReferenceItself=true` via MSBuild or restore first; the packaging project will otherwise self-reference.

### Debug sandbox (`debug/`)

`debug/AnalyzerDebug.csproj` references the local analyzer as an `Analyzer` project reference and contains intentional violations (e.g. SMA0001 flaky initialization). **Expect build failures** there — that confirms analyzers are active. Use the MSTest suite for automated verification, not a clean debug build.

### Optional tooling (not required for routine dev)

- `eng/DocsGen.cs` — regenerate docs from `Resources.resx`
- `eng/BurstLinqBenchmark.cs` — performance benchmarks
- `Unity/` — pre-built DLLs for Unity drop-in (Unity Editor not available in cloud VM)

### NU1900 warnings

Intermittent `NU1900` (vulnerability audit) warnings against NuGet.org are benign if restore and build succeed.
