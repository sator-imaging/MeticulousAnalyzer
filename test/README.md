# Test Conventions

This document is the authoritative guide for developers contributing tests to the StaticMemberAnalyzer project. Follow these conventions to maintain consistency across the test suite.

## Test Setup

- **Framework:** MSTest (`Microsoft.VisualStudio.TestTools.UnitTesting`)
- **Project:** `SatorImaging.StaticMemberAnalyzer.Test.csproj`
- **CI Configuration:** See `.github/workflows/test.yml` for full details
- **Test Command:**
  ```bash
  dotnet test ./test -c <Configuration> --verbosity minimal -p:DontReferenceItself=true
  ```
- **Configurations:** Tests are run in both `Debug` and `Release` configurations via the CI matrix
- **SDK:** .NET 10.x.x

## Test File and Class Naming Convention

### Format

```
{RuleId}_{AnalyzerOrCodeFixProviderName}Tests.cs
```

Optionally, if the test becomes large, add a `{TestSubjectOrFeature}` suffix:

```
{RuleId}_{AnalyzerOrCodeFixProviderName}Tests_{TestSubjectOrFeature}.cs
```

### Examples

- `SMA0020_EnumAnalyzerTests.cs`
- `SMA0026_EnumObfuscationCodeFixProviderTests.cs`
- `SMA0040_DisposableAnalyzerTests.cs`
- `SMA0040_DisposableAnalyzerTests_Suppression.cs`
- `SMA0040_DisposableAnalyzerTests_Boxing.cs`
- `SMA0040_DisposableAnalyzerTests_SwitchExpression.cs`
- `SMA7000_LambdaAnalyzerTests_EdgeCases.cs`

### Important

The `{TestSubjectOrFeature}` suffix **MUST NOT** be "NewTest", "Updated", or other meaningless, context-related, or user-instruction-related names. It must be a persistent, descriptive suffix that clearly identifies the feature or subject being tested.

## Test Method Naming Convention

### Format

```
{RuleId}_{Expectation}_{TestSubjectOrFeature}_{DescriptionOrCondition}
```

Where `{Expectation}` is typically one of:

| Expectation | Usage |
|-------------|-------|
| `Violation` | Analyzer reports a diagnostic |
| `Compliant` | Analyzer does not report a diagnostic |
| `CodeFix`   | Code fix is applied (covers both violation detection and fix) |
| `Config`    | Analyzer configuration/options test |

### Examples

- `SMA0020_Violation_CastToEnum`
- `SMA0020_Compliant_CastFromEnum_CompareToSame`
- `SMA0020_Compliant_CastFromEnum_CompareToSame_Nullable`
- `SMA0026_CodeFix_SimpleEnum`
- `SMA0026_CodeFix_GenericClassWithNestedEnum`
- `SMA0040_Violation_AssemblyAttribute_UnsuppressedType`
- `SMA0060_Config_RuleSuppression`

### Notes on Method Naming

- **CodeFix** test covers both violation detection and codefix functionality. Prefer "CodeFix" over "Violation" for those cases.
- Analyzer configuration tests should use "Config" as the expectation.
- **DO NOT** combine multiple tests into one test method.
- **BAD** naming examples (e.g., tests for suppression comment):
  - `SMA0000_Compliant_SuppressedByComment_SomeDescription`
  - `SMA0000_Violation_NotSuppressedByComment_SomeDescription`
  - Reason: These test the same subject but add a redundant "Not" prefix. The `Compliant`/`Violation` expectation already conveys that meaning.

## FixAllTest

### File Naming

```
FixAllTest_{RuleID}_{CodeFixProviderName}.cs
```

### Examples

- `FixAllTest_SMA0026_EnumObfuscationCodeFixProvider.cs`
- `FixAllTest_SMA7000_LambdaStaticCodeFixProvider.cs`
- `FixAllTest_SMA7001_LambdaStaticCodeFixProvider.cs`
- `FixAllTest_SMA8000_NamedArgumentCodeFixProvider.cs`
- `FixAllTest_SMA8002_NullSuppressionCodeFixProvider.cs`

### Code Structure

- Uses `SourceTemplate` and `FixedTemplate` as `const string` fields with format placeholders
- Templates use `.ReplaceLineEndings()` for cross-platform support (line-ending aware tests)
- Leading/trailing trivia **MUST** be included in FixAllTest templates (e.g., `/* Leading trivia */` and `// Trailing trivia`)
- Test method name is: `{RuleId}_CodeFix_FixAllInSolution`
- Tests use 3 source files (`Test0.cs`, `Test1.cs`, `Test2.cs`) with 3 diagnostics each (9 total)
- Sets `NumberOfIncrementalIterations = 9`
- Includes `TestState`, `FixedState`, and `BatchFixedState`
- **MUST** include the following TODO comment in the test method:

```csharp
// TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
//         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
//       It's available in Roslyn version (4.4.0 or later).
// test.FixAllScope = FixAllScope.Solution;
```

## Other Test Categories

### ConfigTest

- **File naming:** `ConfigTest_{AnalyzerName}.cs`
- **Examples:** `ConfigTest_DisposableAnalyzer.cs`, `ConfigTest_ReadOnlyVariableAnalyzer.cs`
- Tests analyzer behavior with different configuration/options

### CoreTest

- **File:** `CoreTest.cs`
- Unit tests for the `Core` utility class (helper methods used by analyzers)
- Tests methods like `IsKnownImmutableType`, `GetMemberNamePrefix`, `SpanConcat`, `IsSuppressedByComment`, `UnwrapAllNullCoalesceOperation`

### BurstLinqTests

- **File:** `BurstLinqTests.cs`
- Tests for custom LINQ-like extension methods used in the project (performance-optimized alternatives)
- Covers: `ElementAtOrDefault`, `Where`, `OfType`, `Any`, `Contains`, `FirstOrDefault`, `ToArray`, etc.

### ResourceStringTest

- **File:** `ResourceStringTest.cs`
- Validates all resource string properties are non-null (for test coverage of machine-generated properties)

## Verifiers

Located in `test/Verifiers/`:

| Verifier | Purpose |
|----------|---------|
| `CSharpAnalyzerVerifier` | Testing analyzers without code fixes |
| `CSharpCodeFixVerifier` | Testing analyzers with code fix providers |
| `CSharpCodeRefactoringVerifier` | Testing code refactoring providers |
| `FileHeaderCommentAnalyzerVerifier` | Specialized verifier for file header comment analysis |

### Usage Pattern

Declare a type alias at the top of the test file:

```csharp
// Analyzer-only test:
using VerifyCS = StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.SomeAnalyzer>;

// CodeFix test:
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.SomeAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.SomeCodeFixProvider>;

// Analyzer test without codefix (using EmptyCodeFixProvider):
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.SomeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
```
