# Documentation Coverage Report

## Overall Check Result

Most features are well-documented in the English `README.md`. However, there are several rules specifically related to struct analysis and unusual enum definitions that are either completely missing or only briefly mentioned in the introduction without detailed explanations.

## Detailed Result Follows

| Feature | Status | Description |
| :--- | :--- | :--- |
| Static Field Analysis (SMA0001-SMA0004) | Documented | Covered in "Flaky Initialization Analysis" and "Cross-Referencing Problem". |
| TSelf Type Arg Analysis (SMA0010-SMA0012, SMA0015) | Documented | Covered in "TSelf Type Argument Analysis". |
| Enum Type Analysis (SMA0020-SMA0026, SMA0028) | Documented | Covered in "Enum Analyzer and Code Fix Provider" and "Kotlin-like Enum Pattern". |
| Unusual Enum Definition (SMA0027) | Missing | Not mentioned in README.md. |
| Invalid Struct Constructor (SMA0030) | Partially Documented | Mentioned in introduction list but lacks a detailed explanation section. |
| Mutable Struct Field marked as Read-Only (SMA0031) | Missing | Not mentioned in README.md. |
| Implicit Boxing Conversion (SMA0032) | Missing | Not mentioned in README.md. |
| Disposable Analysis (SMA0040-SMA0045) | Documented | Covered in "Disposable Analyzer" and "Disposable Implementation Analysis". |
| File Structure Analysis (SMA0050) | Partially Documented | Mentioned in introduction list but lacks a detailed explanation section. |
| Read-Only Variable Analysis (SMA0060-SMA0064) | Documented | Covered in "Read-Only Variable Analysis". |
| Literal Argument Analysis (SMA0070) | Documented | Covered in "Analysis for Code Review". |
| Annotating and Underlining (SMA9xxx) | Documented | Covered in "Annotating / Underlining" (marked as obsolete). |
