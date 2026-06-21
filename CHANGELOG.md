# Changelog

## [5.0.0-rc.5](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v5.0.0-rc.5) (2026-06-21)

### 🚀 Features
* feat: Add SMA7010/SMA7011 System.Reflection usage analyzers by [@sator-ai-dev](https://github.com/sator-ai-dev) in [#374](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/374)
* feat: Update ExplicitNumberDeclarationAnalyzer to handle out var and foreach by [@sator-imaging](https://github.com/sator-imaging) in [#391](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/391)
* feat: Add SMA0080: internal cross-namespace access analyzer by [@sator-ai-dev](https://github.com/sator-ai-dev) in [#367](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/367)
### 📖 Documentation
* docs: simplify by [@sator-imaging](https://github.com/sator-imaging) in [#363](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/363)
* Update README table of contents by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#365](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/365)
* docs: Update TOC label for RULES.md in README files by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#368](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/368)
* Update README toc items by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#369](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/369)
### 📚 Other Changes
* Use ToDiagnosticMessageName() instead of .Name in Diagnostic.Create by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#354](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/354)
* Add 20 tests to increase analyzer code coverage by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#355](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/355)
* feat: complete ToDiagnosticMessageName migration for Diagnostic.Create by [@sator-imaging](https://github.com/sator-imaging) in [#356](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/356)
* Include outer type in nested type diagnostic names by [@sator-ai-dev](https://github.com/sator-ai-dev) in [#360](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/360)
* feat: use ToDiagnosticMessageName for all remaining Diagnostic.Create symbol args by [@sator-imaging](https://github.com/sator-imaging) in [#361](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/361)
* mv: debug->sandbox by [@sator-imaging](https://github.com/sator-imaging) in [#371](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/371)
* test: ci events by [@sator-imaging](https://github.com/sator-imaging) in [#377](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/377)
* Replace .WithSpan with marker syntax in tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#378](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/378)
* Increase test coverage for Core and Resources by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#380](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/380)
* Refactor code structure by [@sator-imaging](https://github.com/sator-imaging) in [#384](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/384)

### 🎉 New Contributors
* [@sator-ai-dev](https://github.com/sator-ai-dev) made their first contribution in [#360](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/360)

**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v5.0.0-rc.4...v5.0.0-rc.5


## [5.0.0-rc.4](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v5.0.0-rc.4) (2026-06-01)

### 🚀 Features
* Add cross-file static initialization tests by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#337](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/337)
* feat: add params support to named argument analysis and codefix by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#345](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/345)
* feat: add ToDiagnosticMessageName helper for generic type display by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#350](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/350)
### ✨ Bug Fixes
* fix(codefix): preserve separator trivia in EnumObfuscationCodeFixProvider by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#347](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/347)
* fix: add SMA0032 suppress info and clean up Description strings by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#351](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/351)
### 📖 Documentation
* docs: add test conventions README by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#340](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/340)
### 📚 Other Changes
* Align First() exceptions to ImmutableArray with DoesNotReturn throw helper by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#322](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/322)
* Increase branch coverage to >= 80% with 60 new tests by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#324](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/324)
* Targeted branch coverage tests for DisposableAnalyzer by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#326](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/326)
* Remove using System.Linq from source files by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#323](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/323)
* Rename config-related test methods to *_Config_* convention by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#327](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/327)
* Reorganize config tests into ConfigTest_ files by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#329](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/329)
* test: add ResourceTest for coverage (no reflection) by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#331](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/331)
* Add BurstLinq benchmark using BenchmarkDotNet by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#328](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/328)
* Add CoreTest.cs to increase Core.cs coverage by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#332](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/332)
* test: add missing EnumAnalyzer tests from sandbox/EnumSandbox.cs by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#335](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/335)
* Add missing DisposableAnalyzer tests from sandbox/DisposableSandbox.cs by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#336](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/336)
* Update BurstLinqBenchmark to multi-target net10.0 and net5.0 by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#334](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/334)
* perf: linq by [@sator-imaging](https://github.com/sator-imaging) in [#338](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/338)
* BurstLinq: add ICollection<T>.Contains fast path by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#341](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/341)
* perf: add benchmark for Linq_Where.ToImmutableArray by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#339](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/339)
* BurstLinq: use ICollection<T>.CopyTo in ToArray by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#343](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/343)
* test: add cast-and-forget tests for (new Disposable()) as object by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#344](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/344)
* docs: update FixAllTest conventions in test/README.md by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#346](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/346)
* Update diagnostic messages: tone, suppression help, cleanup by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#349](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/349)
* Rename Rule_ and RuleId_ fields to reflect actual targets by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#348](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/348)
* Remove xml docs to avoid unnecessary diffs by [@sator-imaging](https://github.com/sator-imaging) in [#352](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/352)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v5.0.0-rc.3...v5.0.0-rc.4


## [5.0.0-rc.3](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v5.0.0-rc.3) (2026-05-29)

### 🚀 Features
* Test coverage phase 3: NullSuppressionAnalyzer tests by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#316](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/316)
### 📚 Other Changes
* test: add missing enum analyzer tests (phase 3.2) by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#313](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/313)
* test: increase LambdaAnalyzer coverage (SMA7000/7001/7002) by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#314](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/314)
* test: increase TaskAnalyzer coverage (phase 2) by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#315](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/315)
* Add comprehensive BurstLinq unit tests by [@kiro-agent](https://github.com/kiro-agent)[bot] in [#317](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/317)

### New Contributors
* [@kiro-agent](https://github.com/kiro-agent)[bot] made their first contribution in [#313](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/313)

**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v5.0.0-rc.2...v5.0.0-rc.3


## [5.0.0-rc.2](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v5.0.0-rc.2) (2026-05-29)

### 🚀 Features
* feat: relax SMA8000 by [@sator-imaging](https://github.com/sator-imaging) in [#310](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/310)
* feat: remove VisualBasic things by [@sator-imaging](https://github.com/sator-imaging) in [#311](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/311)
### ✨ Bug Fixes
* fix: lol by [@sator-imaging](https://github.com/sator-imaging) in [#309](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/309)
### 📚 Other Changes
* Update test method naming convention by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#306](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/306)
* Refactor test naming convention to {RuleId}_{Name}Tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#307](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/307)
* Update analyzer configuration documentation in READMEs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#308](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/308)
* Implement missing SMA004* tests and fix test suite structure by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#312](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/312)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v5.0.0-rc.1...v5.0.0-rc.2


## [5.0.0-rc.1](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v5.0.0-rc.1) (2026-05-28)

### 📣 Breaking Changes
* feat!: AI created icon is refined by AI by [@sator-imaging](https://github.com/sator-imaging) in [#302](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/302)
### ✨ Bug Fixes
* fix: suppression comment for untracked cast by [@sator-imaging](https://github.com/sator-imaging) in [#301](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/301)
### 📚 Other Changes
* refactor: DocsGen is now file-based app by [@sator-imaging](https://github.com/sator-imaging) in [#298](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/298)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v4.6.0-rc.13...v5.0.0-rc.1


## [4.6.0-rc.13](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v4.6.0-rc.13) (2026-05-27)

### 📚 Other Changes
* Test Update phase 2.1: Reorganize SMA000* tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#283](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/283)
* Reorganize SMA001* Analyzer Tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#284](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/284)
* Reorganize SMA002* Enum Tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#285](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/285)
* Test Update Phase 2.4: Reorganize SMA003* Tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#286](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/286)
* Reorganize SMA004* Tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#287](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/287)
* Reorganize SMA005* tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#288](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/288)
* Test Update phase 2.7 (SMA006*) by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#289](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/289)
* Refactor FixAllTests by [@sator-imaging](https://github.com/sator-imaging) in [#290](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/290)
* Reorganize SMA007* tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#292](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/292)
* Test Reorganization Phase 2.9 (SMA700*) by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#293](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/293)
* Reorganize SMA800* Test Files by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#291](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/291)
* refactor: reorganize folders by [@sator-imaging](https://github.com/sator-imaging) in [#294](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/294)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v4.6.0-rc.12...v4.6.0-rc.13


## [4.6.0-rc.12](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v4.6.0-rc.12) (2026-05-27)

### 📚 Other Changes
* Rename test methods to follow standard pattern by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#273](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/273)
* chore: remove AssemblyInfo.cs by [@sator-imaging](https://github.com/sator-imaging) in [#278](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/278)
* Update README Table of Contents to align with implementation by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#277](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/277)
* refactor: massive csproj update by [@sator-imaging](https://github.com/sator-imaging) in [#279](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/279)
* Test Update Phase 1.5: Renaming and Duplicate Removal by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#275](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/275)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v4.6.0-rc.11...v4.6.0-rc.12


## [4.6.0-rc.11](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/v4.6.0-rc.11) (2026-05-27)

### ✨ Breaking Changes
* feat!: drop `.vsix` support by [@sator-imaging](https://github.com/sator-imaging) in [#271](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/271)
### 🚀 Features
* feat(disposable): massive refactor by [@sator-imaging](https://github.com/sator-imaging) in [#248](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/248)
* feat: coding assistance diagnostics by [@sator-imaging](https://github.com/sator-imaging) in [#262](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/262)
* feat: Allow Math and Mathf in SMA8000 analysis by [@sator-imaging](https://github.com/sator-imaging) in [#267](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/267)
* Expand LambdaAnalyzer delegate support and add async tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#265](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/265)
### 🧹 Bug Fixes
* Fix: Task discard is not recognized by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#239](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/239)
* Fix keyword handling and trivia preservation in LambdaStaticCodeFixProvider by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#264](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/264)
### 📖 Documentation
* Update SMA8002 TIP block and resx strings by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#243](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/243)
### 📚 Other Changes
* style by [@sator-imaging](https://github.com/sator-imaging) in [#240](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/240)
* Update Null suppression diagnostic message and documentation by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#241](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/241)
* Update EnumAnalyzer and comment suppression logic by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#242](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/242)
* refactor: DocsGen by [@sator-imaging](https://github.com/sator-imaging) in [#249](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/249)
* refactor: AnalyzerSandbox by [@sator-imaging](https://github.com/sator-imaging) in [#250](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/250)
* Add Fix All emulation tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#256](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/256)
* Add DisposableAnalyzer foreach tests and fix enumerator false positives by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#258](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/258)
* test: fix xplat problem by [@sator-imaging](https://github.com/sator-imaging) in [#268](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/268)
* Update FixAll tests with leading and trailing trivia by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#270](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/270)
* Add FixAllTest for LambdaAnalyzer by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#269](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/269)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v4.6.0-rc.10...v4.6.0-rc.11
