// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    using VerifyCs = CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

    [TestClass]
    public class SMA0080_InternalNamespaceAccessAnalyzerTests_Generics
    {
        [TestMethod]
        public async Task SMA0080_Compliant_GenericTypeParameter()
        {
            var test = @"
namespace Foo
{
    public class G<T>
    {
        public void M()
        {
            T x = default;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
