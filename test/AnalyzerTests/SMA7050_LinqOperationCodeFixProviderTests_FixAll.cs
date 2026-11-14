// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA7050_LinqOperationCodeFixProviderTests_FixAll
    {
        [TestMethod]
        public void SMA7050_CodeFix_FixAllIsNotSupported()
        {
            Assert.IsNull(new LinqOperationCodeFixProvider().GetFixAllProvider());
        }
    }
}
