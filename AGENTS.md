# AGENTS.md

## Cursor Cloud specific instructions

### What this repo is

**SatorImaging.StaticMemberAnalyzer** is a Roslyn analyzer + code-fix NuGet package (C#). There is no web app or long-running server. End-to-end validation is **`dotnet test`** against in-memory C# snippets.

### Prerequisites

- **.NET SDK 10.x** (matches CI in `.github/workflows/test.yml`). Ubuntu: `sudo apt-get install -y dotnet-sdk-10.0`
- **.NET 8 runtime/SDK** for the test project (`net8.0`): `sudo apt-get install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0`
- **NuGet egress**: `dotnet restore` needs HTTPS to `https://api.nuget.org/v3/index.json` (and package CDN hosts). Without it, restore fails with `NU1301` / SSL errors.

### Critical MSBuild property

Always pass **`-p:DontReferenceItself=true`** for the main solution and tests. Otherwise `Directory.Build.props` pulls the published `SatorImaging.StaticMemberAnalyzer` package from NuGet and creates a cyclic self-reference. The `debug/AnalyzerDebug.csproj` project is exempt and does not need this flag.

### Standard commands (from repo root)

| Task | Command |
|------|---------|
| Restore | `dotnet restore -p:DontReferenceItself=true` |
| Build | `dotnet build StaticMemberAnalyzer.slnx -c Release -p:DontReferenceItself=true` |
| Test (CI) | `dotnet test ./test -c Release --verbosity minimal -p:DontReferenceItself=true` |
| Pack | `dotnet build src/packaging/SatorImaging.StaticMemberAnalyzer.Package.csproj -c Release -p:DontReferenceItself=true` |

### Lint

No separate lint script. Compile-time **Roslyn analyzer rules** (RS*, CS*) apply on build. Optional: `dotnet format` / `dotnet format analyzers` (see README). Repo `.editorconfig` sets some SMA00xx severities to `error`.

### Manual playground (`debug/`)

`debug/AnalyzerDebug.csproj` references the analyzer as a project and contains **intentionally broken** sample code. A full build often **fails** with SMA0001/SMA0002/etc. because those diagnostics are errors—this folder is for IDE/manual repro, not a green `dotnet run`. Prefer **`dotnet test`** for automated verification.

### Optional (out of scope for typical agent work)

- **Unity**: prebuilt DLLs under `Unity/`; manual Editor testing (see `Unity/README.md`).
- **Visual Studio**: Roslyn hive debugging (`devenv /rootsuffix Roslyn`) on Windows.
- **Docs**: `dotnet run -c Release ./eng/DocsGen.cs "./src/analysis/Resources.resx" "./RULES.md"`

### Solution layout

`StaticMemberAnalyzer.slnx` → `src/analysis`, `src/codefix`, `src/packaging`, `test/`.
