// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
{
    using VerifyCS = global::StaticMemberAnalyzer.Test.CSharpAnalyzerVerifier<InternalNamespaceAccessAnalyzer>;

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
