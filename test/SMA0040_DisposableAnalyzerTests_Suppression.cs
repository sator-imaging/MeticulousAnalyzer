// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_Suppression
    {
        [TestMethod]
        public async Task SMA0040_Compliant_AssemblyAttribute_SuppressedType()
        {
            var test = @"
using System;
using System.Diagnostics;

[assembly: DisposableAnalyzerSuppressor(typeof(Test.SuppressedDisposable))]

[Conditional(""DEBUG""), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute { public DisposableAnalyzerSuppressor(params Type[] _) { } }

namespace Test
{
    class SuppressedDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = new SuppressedDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_AssemblyAttribute_SuppressedDiscard()
        {
            var test = @"
using System;
using System.Diagnostics;

[assembly: DisposableAnalyzerSuppressor(typeof(Test.SuppressedDisposable))]

[Conditional(""DEBUG""), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute { public DisposableAnalyzerSuppressor(params Type[] _) { } }

namespace Test
{
    class SuppressedDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            _ = new SuppressedDisposable();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_AssemblyAttribute_UnsuppressedType()
        {
            var test = @"
using System;
using System.Diagnostics;

[assembly: DisposableAnalyzerSuppressor(typeof(Test.SuppressedDisposable))]

[Conditional(""DEBUG""), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute { public DisposableAnalyzerSuppressor(params Type[] _) { } }

namespace Test
{
    class SuppressedDisposable : IDisposable { public void Dispose() { } }
    class OtherDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = {|#0:new OtherDisposable()|};
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("OtherDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_AssemblyAttribute_MultipleTypes()
        {
            var test = @"
using System;
using System.Diagnostics;

[assembly: DisposableAnalyzerSuppressor(typeof(Test.SuppressedA), typeof(Test.SuppressedB))]

[Conditional(""DEBUG""), AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
sealed class DisposableAnalyzerSuppressor : Attribute { public DisposableAnalyzerSuppressor(params Type[] _) { } }

namespace Test
{
    class SuppressedA : IDisposable { public void Dispose() { } }
    class SuppressedB : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var a = new SuppressedA();
            var b = new SuppressedB();
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
