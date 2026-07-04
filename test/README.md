# Test Conventions

This document is the authoritative guide for developers contributing tests to the StaticMemberAnalyzer project. Follow these conventions to maintain consistency across the test suite.

## Test Setup

- **Framework:** MSTest (`Microsoft.VisualStudio.TestTools.UnitTesting`)
- **Project:** `SatorImaging.StaticMemberAnalyzer.Tests.csproj`
- **CI Configuration:** [`.github/workflows/test.yml`](../.github/workflows/test.yml)
- **Test Command:**
  ```bash
  dotnet test ./test -c <Configuration> --verbosity minimal -p:DontReferenceItself=true
  ```
- **Configurations:** `Debug` and `Release` (CI matrix)
- **SDK:** .NET 10.x.x

## Test File and Class Naming Convention

### Format

```
{RuleId}_{AnalyzerOrCodeFixProviderName}Tests.cs
```

If the test file becomes large, add a `{TestSubjectOrFeature}` suffix:

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

The `{TestSubjectOrFeature}` suffix **MUST NOT** be "NewTest", "Updated", or other meaningless, context-related, or user-instruction-related names. Use a persistent, descriptive identifier that clearly represents the feature or subject being tested.

## Test Method Naming Convention

### Format

```
{RuleId}_{Expectation}_{TestSubjectOrFeature}_{DescriptionOrCondition}
```

Where `{Expectation}` is one of:

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

### Notes

- **CodeFix:** Covers both violation detection and codefix functionality. Use `CodeFix` rather than `Violation` as the expectation.
- **Config:** Use `Config` as the expectation for analyzer configuration tests.
- **One scenario per method:** DO NOT combine multiple tests into one test method.
- **BAD naming** (e.g., tests for suppression comment):
  - `SMA0000_Compliant_SuppressedByComment_SomeDescription`
  - `SMA0000_Violation_NotSuppressedByComment_SomeDescription`
  - **Reason:** The `Compliant`/`Violation` expectation already conveys the distinction. Do not add redundant prefixes like "Not" to the subject.

## FixAllTest

### File Naming

```
FixAllTest_{RuleID}_{CodeFixProviderName}.cs
```

### Reference

- `FixAllTest_SMA0026_EnumObfuscationCodeFixProvider.cs`
- `FixAllTest_SMA7000_LambdaStaticCodeFixProvider.cs`
- `FixAllTest_SMA7001_LambdaStaticCodeFixProvider.cs`
- `FixAllTest_SMA8000_NamedArgumentCodeFixProvider.cs`
- `FixAllTest_SMA8002_NullSuppressionCodeFixProvider.cs`

### Code Structure

- **Reference:** Refer to the reference files above for the code structure.
- **Template fields:** Use `SourceTemplate` and `FixedTemplate` as `const string` fields with format placeholders.
- **Cross-platform:** Use `.ReplaceLineEndings()` on templates for line-ending aware tests.
- **Trivia:** Include leading/trailing trivia in templates (e.g., `/* Leading trivia */` and `// Trailing trivia`).
- **Method name:** Use `{RuleId}_CodeFix_FixAllInSolution`.
- **Source files:** Use 3 files (`Test0.cs`, `Test1.cs`, `Test2.cs`) with 3 or more diagnostics each.
- **States:** Include `TestState`, `FixedState`, and `BatchFixedState`.
- **TODO comment:** Copy the source code comment below to notify developers of the limitation.
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
- **Purpose:** Tests analyzer behavior with different configuration/options

### CoreTest

- **File:** `CoreTest.cs`
- **Purpose:** Unit tests for the `Core` utility class (helper methods used by analyzers)
- **Covered methods:** `IsKnownImmutableType`, `GetMemberNamePrefix`, `SpanConcat`, `IsSuppressedByComment`, `UnwrapAllNullCoalesceOperation`

### BurstLinqTests

- **File:** `BurstLinqTests.cs`
- **Purpose:** Tests for custom LINQ-like extension methods used in the project (performance-optimized alternatives)
- **Covered methods:** `ElementAtOrDefault`, `Where`, `OfType`, `Any`, `Contains`, `FirstOrDefault`, `ToArray`, etc.

### ResourceStringTest

- **File:** `ResourceStringTest.cs`
- **Purpose:** Validates all resource string properties are non-null (for test coverage of machine-generated properties)

## Verifiers

**Location:** `test/Verifiers/`

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
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.SomeAnalyzer>;

// CodeFix test:
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.SomeAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.SomeCodeFixProvider>;

// Analyzer test without codefix (using EmptyCodeFixProvider):
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.SomeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
```
