// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DisposableAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0042_DisposableAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0042_Violation_AllCodePathsReturn()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable Method(bool condition)
        {
            var {|#0:d = new MyDisposable()|};
            var {|#1:other = new MyDisposable()|};

            if (condition)
            {
                return d;
            }
            else
            {
                return other;
            }
        }
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test, new[]
            {
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                    .WithLocation(markupKey: 0)
                    .WithArguments("d"),
                VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                    .WithLocation(markupKey: 1)
                    .WithArguments("other")
            });
        }

        [TestMethod]
        public async Task SMA0042_Compliant_SwitchExpression_ReturnedOnAllPaths()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }
    class Program
    {
        MyDisposable Method(int value)
        {
            return value switch
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
        public async Task SMA0042_Violation_ReturnedOnSomePaths()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable? Method(bool condition)
        {
            var {|#0:d = new MyDisposable()|};
            if (condition)
            {
                return d;
            }
            else
            {
                return null;
            }
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0042_Violation_ReturnedOnSomePaths_WithDefault()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable? Method(bool condition)
        {
            var {|#0:d = new MyDisposable()|};
            if (condition)
            {
                return d;
            }
            else
            {
                return default;
            }
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0042_Violation_AllCodePathsReturn_ObjectCreation()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    class Program
    {
        MyDisposable? Method()
        {
            var {|#0:d = new MyDisposable()|};
            if (DateTime.Now.Year > 3000)
            {
                return new MyDisposable();
            }
            return d;
        }
    }
}
";

            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
