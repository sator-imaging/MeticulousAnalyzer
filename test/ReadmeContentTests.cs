// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    /// <summary>
    /// Tests verifying that README files contain the consolidated RULES.md bullet format
    /// introduced in the PR that merges the "File Header Comment Enforcement" and
    /// "Coding Assistance" bullet items into a single bullet referencing RULES.md.
    /// </summary>
    [TestClass]
    public class ReadmeContentTests
    {
        // Locate repo root relative to the test assembly output directory.
        // Test output is typically under test/bin/<config>/<tfm>/.
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(typeof(ReadmeContentTests).Assembly.Location)!,
                "..", "..", "..", "..", ".."));

        private static string ReadReadme(string fileName)
        {
            string path = Path.Combine(RepoRoot, fileName);
            Assert.IsTrue(File.Exists(path), $"README file not found at: {path}");
            return File.ReadAllText(path);
        }

        // ------------------------------------------------------------------ //
        //  README.md (English)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadmeMd_ContainsConsolidatedRulesMdBullet()
        {
            string content = ReadReadme("README.md");
            // New single bullet: starts with a link to RULES.md then lists sub-features.
            Assert.IsTrue(
                content.Contains("[RULES.md](RULES.md):"),
                "README.md should contain the consolidated '- [RULES.md](RULES.md):' bullet.");
        }

        [TestMethod]
        public void ReadmeMd_ConsolidatedBulletContainsFileHeaderCommentLink()
        {
            string content = ReadReadme("README.md");
            // The anchor link for file-structure-analysis must be present inside the new bullet.
            Assert.IsTrue(
                content.Contains("[File Header Comment Enforcement](RULES.md#file-structure-analysis)"),
                "README.md should contain an inline link to RULES.md#file-structure-analysis.");
        }

        [TestMethod]
        public void ReadmeMd_ConsolidatedBulletContainsCodingAssistanceLink()
        {
            string content = ReadReadme("README.md");
            // The anchor link for coding-assistance must be present inside the new bullet.
            Assert.IsTrue(
                content.Contains("[Coding Assistance](RULES.md#coding-assistance)"),
                "README.md should contain an inline link to RULES.md#coding-assistance.");
        }

        [TestMethod]
        public void ReadmeMd_DoesNotContainOldStandaloneFileHeaderBullet()
        {
            string content = ReadReadme("README.md");
            // Old format: "- [File Header Comment Enforcement](RULES.md#file-structure-analysis)"
            // as an independent bullet (i.e., the bullet text starts with the link, not preceded
            // by a prior link on the same line).
            Assert.IsFalse(
                Regex.IsMatch(content,
                    @"^\s*-\s*\[File Header Comment Enforcement\]",
                    RegexOptions.Multiline),
                "README.md should not contain the old standalone '- [File Header Comment Enforcement]' bullet.");
        }

        [TestMethod]
        public void ReadmeMd_DoesNotContainOldStandaloneCodingAssistanceBullet()
        {
            string content = ReadReadme("README.md");
            // Old format: "- [Coding Assistance](RULES.md#coding-assistance)" as a bullet start.
            Assert.IsFalse(
                Regex.IsMatch(content,
                    @"^\s*-\s*\[Coding Assistance\]",
                    RegexOptions.Multiline),
                "README.md should not contain the old standalone '- [Coding Assistance]' bullet.");
        }

        [TestMethod]
        public void ReadmeMd_ConsolidatedBulletAppearsPreciselyOnce()
        {
            string content = ReadReadme("README.md");
            int count = Regex.Matches(content, @"^\s*-\s*\[RULES\.md\]", RegexOptions.Multiline).Count;
            Assert.AreEqual(1, count,
                "README.md should contain exactly one '- [RULES.md]' bullet.");
        }

        [TestMethod]
        public void ReadmeMd_ConsolidatedBulletEndsWithDiagnosticRulesPhrase()
        {
            string content = ReadReadme("README.md");
            Assert.IsTrue(
                content.Contains("and all diagnostic rules."),
                "README.md consolidated bullet should end with 'and all diagnostic rules.'");
        }

        // ------------------------------------------------------------------ //
        //  README.ja.md (Japanese)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadmeJaMd_ContainsConsolidatedRulesMdBullet()
        {
            string content = ReadReadme("README.ja.md");
            Assert.IsTrue(
                content.Contains("[RULES.md](RULES.md):"),
                "README.ja.md should contain the consolidated '- [RULES.md](RULES.md):' bullet.");
        }

        [TestMethod]
        public void ReadmeJaMd_ConsolidatedBulletContainsFileHeaderCommentLink()
        {
            string content = ReadReadme("README.ja.md");
            Assert.IsTrue(
                content.Contains("[ファイルヘッダーコメントの強制](RULES.md#file-structure-analysis)"),
                "README.ja.md should contain an inline link to RULES.md#file-structure-analysis.");
        }

        [TestMethod]
        public void ReadmeJaMd_ConsolidatedBulletContainsCodingAssistanceLink()
        {
            string content = ReadReadme("README.ja.md");
            Assert.IsTrue(
                content.Contains("[コーディング支援](RULES.md#coding-assistance)"),
                "README.ja.md should contain an inline link to RULES.md#coding-assistance.");
        }

        [TestMethod]
        public void ReadmeJaMd_DoesNotContainOldStandaloneFileHeaderBullet()
        {
            string content = ReadReadme("README.ja.md");
            // Old format: "- [ファイルヘッダーコメントの強制](RULES.md#file-structure-analysis)"
            // as the sole content of a bullet line.
            Assert.IsFalse(
                Regex.IsMatch(content,
                    @"^\s*-\s*\[ファイルヘッダーコメントの強制\]",
                    RegexOptions.Multiline),
                "README.ja.md should not contain the old standalone '- [ファイルヘッダーコメントの強制]' bullet.");
        }

        [TestMethod]
        public void ReadmeJaMd_DoesNotContainOldStandaloneCodingAssistanceBullet()
        {
            string content = ReadReadme("README.ja.md");
            // Old format: "- [コーディング支援](RULES.md#coding-assistance)" as a bullet start.
            Assert.IsFalse(
                Regex.IsMatch(content,
                    @"^\s*-\s*\[コーディング支援\]",
                    RegexOptions.Multiline),
                "README.ja.md should not contain the old standalone '- [コーディング支援]' bullet.");
        }

        [TestMethod]
        public void ReadmeJaMd_ConsolidatedBulletAppearsPreciselyOnce()
        {
            string content = ReadReadme("README.ja.md");
            int count = Regex.Matches(content, @"^\s*-\s*\[RULES\.md\]", RegexOptions.Multiline).Count;
            Assert.AreEqual(1, count,
                "README.ja.md should contain exactly one '- [RULES.md]' bullet.");
        }

        [TestMethod]
        public void ReadmeJaMd_ConsolidatedBulletContainsJapaneseSuffix()
        {
            string content = ReadReadme("README.ja.md");
            // The new bullet ends with "全ての診断ルール（英語）" indicating all rules in English.
            Assert.IsTrue(
                content.Contains("全ての診断ルール（英語）"),
                "README.ja.md consolidated bullet should contain '全ての診断ルール（英語）'.");
        }

        // ------------------------------------------------------------------ //
        //  README.zh-CN.md (Simplified Chinese)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadmeZhCnMd_ContainsConsolidatedRulesMdBullet()
        {
            string content = ReadReadme("README.zh-CN.md");
            Assert.IsTrue(
                content.Contains("[RULES.md](RULES.md):"),
                "README.zh-CN.md should contain the consolidated '- [RULES.md](RULES.md):' bullet.");
        }

        [TestMethod]
        public void ReadmeZhCnMd_ConsolidatedBulletContainsFileHeaderCommentLink()
        {
            string content = ReadReadme("README.zh-CN.md");
            Assert.IsTrue(
                content.Contains("[文件头注释强制规则](RULES.md#file-structure-analysis)"),
                "README.zh-CN.md should contain an inline link to RULES.md#file-structure-analysis.");
        }

        [TestMethod]
        public void ReadmeZhCnMd_ConsolidatedBulletContainsCodingAssistanceLink()
        {
            string content = ReadReadme("README.zh-CN.md");
            Assert.IsTrue(
                content.Contains("[编码辅助](RULES.md#coding-assistance)"),
                "README.zh-CN.md should contain an inline link to RULES.md#coding-assistance.");
        }

        [TestMethod]
        public void ReadmeZhCnMd_DoesNotContainOldStandaloneFileHeaderBullet()
        {
            string content = ReadReadme("README.zh-CN.md");
            // Old format: "- [文件头注释强制规则](RULES.md#file-structure-analysis)" as a bullet start.
            Assert.IsFalse(
                Regex.IsMatch(content,
                    @"^\s*-\s*\[文件头注释强制规则\]",
                    RegexOptions.Multiline),
                "README.zh-CN.md should not contain the old standalone '- [文件头注释强制规则]' bullet.");
        }

        [TestMethod]
        public void ReadmeZhCnMd_DoesNotContainOldStandaloneCodingAssistanceBullet()
        {
            string content = ReadReadme("README.zh-CN.md");
            // Old format: "- [编码辅助](RULES.md#coding-assistance)" as a bullet start.
            Assert.IsFalse(
                Regex.IsMatch(content,
                    @"^\s*-\s*\[编码辅助\]",
                    RegexOptions.Multiline),
                "README.zh-CN.md should not contain the old standalone '- [编码辅助]' bullet.");
        }

        [TestMethod]
        public void ReadmeZhCnMd_ConsolidatedBulletAppearsPreciselyOnce()
        {
            string content = ReadReadme("README.zh-CN.md");
            int count = Regex.Matches(content, @"^\s*-\s*\[RULES\.md\]", RegexOptions.Multiline).Count;
            Assert.AreEqual(1, count,
                "README.zh-CN.md should contain exactly one '- [RULES.md]' bullet.");
        }

        [TestMethod]
        public void ReadmeZhCnMd_ConsolidatedBulletContainsChineseSuffix()
        {
            string content = ReadReadme("README.zh-CN.md");
            // The new bullet ends with "以及所有诊断规则（英文）".
            Assert.IsTrue(
                content.Contains("以及所有诊断规则（英文）"),
                "README.zh-CN.md consolidated bullet should contain '以及所有诊断规则（英文）'.");
        }

        // ------------------------------------------------------------------ //
        //  Cross-file consistency checks
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void AllThreeReadmes_UseIdenticalRulesMdLinkTarget()
        {
            // All three localizations should link to the same RULES.md target.
            string en = ReadReadme("README.md");
            string ja = ReadReadme("README.ja.md");
            string zh = ReadReadme("README.zh-CN.md");

            Assert.IsTrue(en.Contains("(RULES.md#file-structure-analysis)"),
                "README.md must link to RULES.md#file-structure-analysis.");
            Assert.IsTrue(ja.Contains("(RULES.md#file-structure-analysis)"),
                "README.ja.md must link to RULES.md#file-structure-analysis.");
            Assert.IsTrue(zh.Contains("(RULES.md#file-structure-analysis)"),
                "README.zh-CN.md must link to RULES.md#file-structure-analysis.");

            Assert.IsTrue(en.Contains("(RULES.md#coding-assistance)"),
                "README.md must link to RULES.md#coding-assistance.");
            Assert.IsTrue(ja.Contains("(RULES.md#coding-assistance)"),
                "README.ja.md must link to RULES.md#coding-assistance.");
            Assert.IsTrue(zh.Contains("(RULES.md#coding-assistance)"),
                "README.zh-CN.md must link to RULES.md#coding-assistance.");
        }

        [TestMethod]
        public void AllThreeReadmes_NewBulletStartsWithRulesMdLink()
        {
            // Regression: confirm the leading [RULES.md](RULES.md) link is the
            // first element of the bullet, not buried inside sentence prose.
            foreach (string fileName in new[] { "README.md", "README.ja.md", "README.zh-CN.md" })
            {
                string content = ReadReadme(fileName);
                bool hasLeadingLink = Regex.IsMatch(
                    content,
                    @"^\s*-\s*\[RULES\.md\]\(RULES\.md\):",
                    RegexOptions.Multiline);
                Assert.IsTrue(hasLeadingLink,
                    $"{fileName}: consolidated bullet must begin with '[RULES.md](RULES.md):'.");
            }
        }
    }
}
