# Test Conventions

This document defines the naming conventions, file structure, and patterns for tests in the StaticMemberAnalyzer project.

## Test Setup

| Item | Value |
|------|-------|
| Framework | MSTest (`Microsoft.VisualStudio.TestTools.UnitTesting`) |
| Project | `SatorImaging.StaticMemberAnalyzer.Test.csproj` |
| CI Configuration | [`.github/workflows/test.yml`](../.github/workflows/test.yml) |
| SDK | .NET 10.x.x |
| Configurations | `Debug`, `Release` (matrix) |

### Test Command

```bash
dotnet test ./test -c <Configuration> --verbosity minimal -p:DontReferenceItself=true
```

## Test File and Class Naming

### Format

```
{RuleId}_{AnalyzerOrCodeFixProviderName}Tests.cs
```

### Format (with feature suffix)

```
{RuleId}_{AnalyzerOrCodeFixProviderName}Tests_{TestSubjectOrFeature}.cs
```

This format is used when a test file becomes large and needs to be split by feature.

### Examples

| File | Description |
|------|-------------|
| `SMA0020_EnumAnalyzerTests.cs` | Standard single-file test |
| `SMA0026_EnumObfuscationCodeFixProviderTests.cs` | CodeFix provider test |
| `SMA0040_DisposableAnalyzerTests.cs` | Base test file |
| `SMA0040_DisposableAnalyzerTests_Suppression.cs` | Split by feature: suppression |
| `SMA0040_DisposableAnalyzerTests_Boxing.cs` | Split by feature: boxing |
| `SMA0040_DisposableAnalyzerTests_SwitchExpression.cs` | Split by feature: switch expression |
| `SMA7000_LambdaAnalyzerTests_EdgeCases.cs` | Split by feature: edge cases |

### Suffix Requirements

1. The `{TestSubjectOrFeature}` suffix **MUST** be a persistent, descriptive identifier of the feature or subject under test.
2. The following suffix names are **prohibited**: "NewTest", "Updated", or any other meaningless, context-related, or user-instruction-derived names.

## Test Method Naming

### Format

```
{RuleId}_{Expectation}_{TestSubjectOrFeature}_{DescriptionOrCondition}
```

### Expectation Values

| Expectation | Meaning |
|-------------|---------|
| `Violation` | Analyzer reports a diagnostic |
| `Compliant` | Analyzer does not report a diagnostic |
| `CodeFix` | Code fix is applied (covers both violation detection and fix) |
| `Config` | Analyzer configuration/options behavior |

### Examples

| Method Name | Category |
|-------------|----------|
| `SMA0020_Violation_CastToEnum` | Violation detection |
| `SMA0020_Compliant_CastFromEnum_CompareToSame` | Compliant case |
| `SMA0020_Compliant_CastFromEnum_CompareToSame_Nullable` | Compliant with condition |
| `SMA0026_CodeFix_SimpleEnum` | Code fix application |
| `SMA0026_CodeFix_GenericClassWithNestedEnum` | Code fix with context |
| `SMA0040_Violation_AssemblyAttribute_UnsuppressedType` | Violation with condition |
| `SMA0060_Config_RuleSuppression` | Configuration test |

### Rules

1. A CodeFix test covers both violation detection and fix application. Use `CodeFix` as the expectation, not `Violation`.
2. Analyzer configuration tests use `Config` as the expectation.
3. Each test method tests exactly one scenario. Do not combine multiple assertions for different scenarios into a single method.
4. The `Compliant`/`Violation` expectation inherently expresses the positive/negative condition. Do not add redundant prefixes (e.g., "Not") to the subject.

### Prohibited Naming Pattern

| Prohibited | Correct | Reason |
|------------|---------|--------|
| `SMA0000_Compliant_SuppressedByComment_Desc` | `SMA0000_Compliant_SuppressionComment_Desc` | `Compliant` already implies "suppressed" |
| `SMA0000_Violation_NotSuppressedByComment_Desc` | `SMA0000_Violation_SuppressionComment_Desc` | `Violation` already implies "not suppressed" |

## FixAllTest

### File Naming

```
FixAllTest_{RuleId}_{CodeFixProviderName}.cs
```

### Reference Files

| File | Description |
|------|-------------|
| `FixAllTest_SMA0026_EnumObfuscationCodeFixProvider.cs` | Enum obfuscation code fix |
| `FixAllTest_SMA7000_LambdaStaticCodeFixProvider.cs` | Lambda static code fix |
| `FixAllTest_SMA7001_LambdaStaticCodeFixProvider.cs` | Lambda static code fix (variant) |
| `FixAllTest_SMA8000_NamedArgumentCodeFixProvider.cs` | Named argument code fix |
| `FixAllTest_SMA8002_NullSuppressionCodeFixProvider.cs` | Null suppression code fix |

These files serve as the reference implementation. Refer to them for code structure when creating new FixAllTest files.

### Structure Requirements

| Requirement | Detail |
|-------------|--------|
| Template fields | `SourceTemplate` and `FixedTemplate` as `const string` with format placeholders |
| Cross-platform | Templates use `.ReplaceLineEndings()` |
| Trivia | Leading/trailing trivia **MUST** be included (e.g., `/* Leading trivia */` and `// Trailing trivia`) |
| Method name | `{RuleId}_CodeFix_FixAllInSolution` |
| Source files | 3 files (`Test0.cs`, `Test1.cs`, `Test2.cs`) with 3 diagnostics each |
| Iterations | `NumberOfIncrementalIterations = 9` |
| States | `TestState`, `FixedState`, and `BatchFixedState` |

### Required TODO Comment

Every FixAllTest method **MUST** include the following source code comment to notify developers of the current limitation:

```csharp
// TODO: FixAllProvider test cannot be done with current Roslyn version (3.8.0).
//         e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
//       It's available in Roslyn version (4.4.0 or later).
// test.FixAllScope = FixAllScope.Solution;
```

## Other Test Categories

### ConfigTest

| Item | Value |
|------|-------|
| File naming | `ConfigTest_{AnalyzerName}.cs` |
| Purpose | Tests analyzer behavior with different configuration/options |
| Examples | `ConfigTest_DisposableAnalyzer.cs`, `ConfigTest_ReadOnlyVariableAnalyzer.cs` |

### CoreTest

| Item | Value |
|------|-------|
| File | `CoreTest.cs` |
| Purpose | Unit tests for the `Core` utility class (helper methods used by analyzers) |
| Covered methods | `IsKnownImmutableType`, `GetMemberNamePrefix`, `SpanConcat`, `IsSuppressedByComment`, `UnwrapAllNullCoalesceOperation` |

### BurstLinqTests

| Item | Value |
|------|-------|
| File | `BurstLinqTests.cs` |
| Purpose | Tests for custom LINQ-like extension methods (performance-optimized alternatives) |
| Covered methods | `ElementAtOrDefault`, `Where`, `OfType`, `Any`, `Contains`, `FirstOrDefault`, `ToArray` |

### ResourceStringTest

| Item | Value |
|------|-------|
| File | `ResourceStringTest.cs` |
| Purpose | Validates all resource string properties are non-null (coverage for machine-generated properties) |

## Verifiers

### Location

`test/Verifiers/`

### Available Verifiers

| Verifier | Purpose |
|----------|---------|
| `CSharpAnalyzerVerifier` | Testing analyzers without code fixes |
| `CSharpCodeFixVerifier` | Testing analyzers with code fix providers |
| `CSharpCodeRefactoringVerifier` | Testing code refactoring providers |
| `FileHeaderCommentAnalyzerVerifier` | Specialized verifier for file header comment analysis |

### Usage Pattern

Type alias declaration at the top of the test file:

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
