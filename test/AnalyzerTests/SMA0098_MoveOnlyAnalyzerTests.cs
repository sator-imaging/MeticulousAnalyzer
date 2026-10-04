// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MoveOnlyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA0098_MoveOnlyAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0098_Compliant_UsingAndAwaitUsingAtRootLevel()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace Test
{
    struct MoveOnlyIDisposable : IDisposable, IAsyncDisposable
    {
        public MoveOnlyIDisposable Move() => this;
        public void Dispose() { }
        public ValueTask DisposeAsync() => default;
    }

    class Program
    {
        void SyncMethod(MoveOnlyIDisposable arg)
        {
            using var u = arg;
        }

        void SyncMethodMove(MoveOnlyIDisposable arg)
        {
            using var u = arg.Move();
        }

        void SyncMethodStatement(MoveOnlyIDisposable arg)
        {
            using (arg)
            {
            }
        }

        async Task AsyncMethod(MoveOnlyIDisposable arg)
        {
            await using var u = arg;
        }

        async Task AsyncMethodMove(MoveOnlyIDisposable arg)
        {
            await using var u = arg.Move();
        }

        async Task AsyncMethodStatement(MoveOnlyIDisposable arg)
        {
            await using (arg)
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0098_Compliant_WithRefModifiersOrNonDisposable()
        {
            var test = @"
using System;

namespace Test
{
    struct MoveOnlyStruct
    {
        public MoveOnlyStruct Move() => this;
    }

    struct MoveOnlyIDisposable : IDisposable
    {
        public MoveOnlyIDisposable Move() => this;
        public void Dispose() { }
    }

    class Program
    {
        void NonDisposable(MoveOnlyStruct arg)
        {
        }

        void WithIn(in MoveOnlyIDisposable arg)
        {
        }

        void WithRef(ref MoveOnlyIDisposable arg)
        {
        }

    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0098_Violation_MissingUsingOrNestedUsing()
        {
            var test = @"
using System;

namespace Test
{
    struct MoveOnlyIDisposable : IDisposable
    {
        public MoveOnlyIDisposable Move() => this;
        public void Dispose() { }
    }

    class Program
    {
        void MissingUsing(MoveOnlyIDisposable {|#0:arg|})
        {
            _ = arg.Move();
        }

        void NestedUsingInIf(MoveOnlyIDisposable {|#1:arg|}, bool flag)
        {
            if (flag)
            {
                using var u = arg;
            }
        }

        void NestedUsingInBlock(MoveOnlyIDisposable {|#2:arg|})
        {
            {
                using var u = arg;
            }
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_DisposableParameterMissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("arg");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_DisposableParameterMissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("arg");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_DisposableParameterMissingUsing)
                .WithLocation(markupKey: 2)
                .WithArguments("arg");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }
    }
}
