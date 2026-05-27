# Test Update - Duplicate Test Removal

The following tests have been removed as they were duplicates of existing tests or redundant given other test suites.

| Removed Test Name | File Name | Kept Duplicate Test Name | File Name |
| --- | --- | --- | --- |
| `SMA0026_Violate_EnumObfuscation` | `src/StaticMemberAnalyzer.Test/EnumAnalyzerUnitTests.cs` | `SMA0026_CodeFix_SimpleEnum` | `src/StaticMemberAnalyzer.Test/EnumObfuscationCodeFixProviderUnitTests.cs` |
| `SMA7000_CodeFix_AddStaticModifierPreservesFormatting_ReproIssue3` | `src/StaticMemberAnalyzer.Test/LambdaStaticCodeFixProviderReproTests.cs` | `SMA7000_CodeFix_NonStaticLambda` | `src/StaticMemberAnalyzer.Test/LambdaAnalyzerUnitTests.cs` |
| `SMA8000_Violate_MethodLiteralArguments` | `src/StaticMemberAnalyzer.Test/ArgumentAnalyzerUnitTests.cs` | `SMA8000_CodeFix_MethodLiteralArguments` | `src/StaticMemberAnalyzer.Test/NamedArgumentCodeFixProviderUnitTests.cs` |
| `SMA8000_Violate_ConstructorLiteralArguments` | `src/StaticMemberAnalyzer.Test/ArgumentAnalyzerUnitTests.cs` | `SMA8000_CodeFix_ConstructorLiteralArguments` | `src/StaticMemberAnalyzer.Test/NamedArgumentCodeFixProviderUnitTests.cs` |
| `SMA8000_Violate_AttributeArguments` | `src/StaticMemberAnalyzer.Test/ArgumentAnalyzerUnitTests.cs` | `SMA8000_CodeFix_AttributeArguments` | `src/StaticMemberAnalyzer.Test/NamedArgumentCodeFixProviderUnitTests.cs` |
