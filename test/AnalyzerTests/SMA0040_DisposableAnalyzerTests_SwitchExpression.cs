// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0040_DisposableAnalyzerTests_SwitchExpression
    {
        [TestMethod]
        public async Task SMA0040_Violation_SwitchExpression_AssignToNonDisposableField()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static object _field;

        void Method(int value)
        {
            _field = {|#2:{|#0:{|#1:value switch
            {
                1 => new MyDisposable(),
                _ => throw new Exception(),
            }|}|}
            as object|}
            ;
        }
    }
}
";

            // The analyzer reports multiple diagnostics:
            // - switch expression arm creating disposable (reported twice - for switch and for assignment)
            // - the cast to object also triggers
            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            var expected2 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 2)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_SwitchExpression_AssignToIDisposableField()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        static IDisposable _field;

        void Method(int value)
        {
            _field = value switch
            {
                1 => new MyDisposable(),
                _ => throw new Exception(),
            };
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_SwitchExpression_AssignToFieldArrayElement()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable[] _arr = new MyDisposable[2];

        void Method(int value)
        {
            _arr[0] = value switch
            {
                1 => new MyDisposable(),
                _ => throw new Exception(),
            };
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_SwitchExpression_WithUsing()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method(int value)
        {
            using var d = value switch
            {
                1 => new MyDisposable(),
                _ => throw new Exception(),
            };
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_SwitchExpression_LocalArrayElement()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method(int value)
        {
            var arr = new IDisposable[1];
            {|#0:arr[0]|} = {|#1:value switch
            {
                1 => new MyDisposable(),
                _ => throw new Exception(),
            }|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected1);
        }

        [TestMethod]
        public async Task SMA0040_Violation_SwitchExpression_LocalListElement()
        {
            var test = @"
using System;
using System.Collections.Generic;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method(int value)
        {
            var list = new List<IDisposable>();
            {|#0:list[0]|} = {|#1:value switch
            {
                1 => new MyDisposable(),
                _ => throw new Exception(),
            }|};
        }
    }
}
";

            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected1);
        }
    }
}
