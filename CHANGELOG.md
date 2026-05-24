# Changelog

## [9.9.9](https://github.com/sator-imaging/StaticMemberAnalyzer/releases/tag/untagged-255374490e9997683c75) (2026-05-24)

### 🚀 Features
* Add task local variable tracking feature by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#190](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/190)
* Add ExplicitNumberDeclarationAnalyzer (SMA8001) by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#208](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/208)
* Add ternary expression support to DisposableAnalyzer by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#214](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/214)
* Implicit boxing suppression and README updates by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#216](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/216)
* Add explicit number analyzer tests for members and method returns by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#217](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/217)
* Add Null Suppression analyzer and code fix (SMA8002) by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#212](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/212)
* Update ArgumentAnalyzer: Boolean parameter exemption for 'true'/'false' methods by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#225](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/225)
* feat(disposable): massive refactor by [@sator-imaging](https://github.com/sator-imaging) in [#248](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/248)
### 🧹 Bug Fixes
* Fix Enum analyzer null-conditional access support by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#195](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/195)
* fix: broken codefixes by recalculating nodes from diagnostics in Fix All scenarios by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#228](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/228)
* Fix "Fix All" support in NamedArgumentCodeFixProvider (SMA8000) by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#229](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/229)
* fix: CLI 'Fix All' functionality by aligning equivalenceKey with title by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#233](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/233)
* fix: disposable analyzer misdetection by [@sator-imaging](https://github.com/sator-imaging) in [#237](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/237)
* Fix: Task discard is not recognized by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#239](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/239)
### 📖 Documentation
* Update Argument Analyzer documentation in READMEs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#203](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/203)
* Update READMEs for test framework exemption clarification by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#227](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/227)
* Update SMA8002 TIP block and resx strings by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#243](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/243)
### 📚 Other Changes
* Work/update argument analyzer boolean expression by [@sator-imaging](https://github.com/sator-imaging) in [#202](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/202)
* Add implicit conversion tests to ArgumentAnalyzer by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#205](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/205)
* Update terminology to "Async context analysis" by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#204](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/204)
* Centralize suppression comment handling by [@sator-imaging](https://github.com/sator-imaging) in [#209](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/209)
* perf & refactor by [@sator-imaging](https://github.com/sator-imaging) in [#210](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/210)
* bot: release/v4.6.0-rc.3 by [@github-actions](https://github.com/github-actions)[bot] in [#213](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/213)
* Add unit tests for builtin primitives and disposable field suppression by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#211](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/211)
* optimize by [@sator-imaging](https://github.com/sator-imaging) in [#218](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/218)
* bot: release/v4.6.0-rc.4 by [@github-actions](https://github.com/github-actions)[bot] in [#220](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/220)
* optimize phase 1 by [@sator-imaging](https://github.com/sator-imaging) in [#222](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/222)
* simplify by [@sator-imaging](https://github.com/sator-imaging) in [#226](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/226)
* Optimize codefix providers and remove redundant checks by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#234](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/234)
* [bot] Sync `.github` (20260522-000326) by [@github-actions](https://github.com/github-actions)[bot] in [#238](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/238)
* style by [@sator-imaging](https://github.com/sator-imaging) in [#240](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/240)
* Update Null suppression diagnostic message and documentation by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#241](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/241)
* [bot] Sync `.github` (20260523-000806) by [@github-actions](https://github.com/github-actions)[bot] in [#244](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/244)
* [bot] Changelog 4.5.1 (20260523-021923) by [@github-actions](https://github.com/github-actions)[bot] in [#245](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/245)
* Update EnumAnalyzer and comment suppression logic by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#242](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/242)
* refactor: DocsGen by [@sator-imaging](https://github.com/sator-imaging) in [#249](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/249)
* refactor: AnalyzerDebug by [@sator-imaging](https://github.com/sator-imaging) in [#250](https://github.com/sator-imaging/StaticMemberAnalyzer/pull/250)


**Full Changelog**: https://github.com/sator-imaging/StaticMemberAnalyzer/compare/v4.6.0-rc.2...v9.9.9
