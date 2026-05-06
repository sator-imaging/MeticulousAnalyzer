// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DisposableMethodImplAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class DisposableMethodImplAnalyzerUnitTests
    {
        [TestMethod]
        public async Task SMA0043_UndisposedField()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|} : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    public void Dispose()
    {
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("_field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0043_DisposedField()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    public void Dispose()
    {
        _field.Dispose();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0043_UndisposedProperty()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    public MyDisposable Prop { get; } = new MyDisposable();
    public void Dispose()
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0044_MissingDispose()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|}
{
    private MyDisposable _field = new MyDisposable();
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(0)
                .WithArguments("TestClass");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0043_FullDisposePattern()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|} : IDisposable
{
    private MyDisposable _field = new MyDisposable();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("_field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0043_ExplicitInterface()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|} : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    void IDisposable.Dispose()
    {
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("_field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0043_ExplicitInterface_Disposed()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    void IDisposable.Dispose()
    {
        ((IDisposable)_field).Dispose();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }


        [TestMethod]
        public async Task SMA0043_ExpressionBodiedProperty_Detected()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    public MyDisposable Prop => null;
    public void Dispose()
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0043_ExpressionBodiedProperty_Disposed()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    public MyDisposable Prop => null;
    public void Dispose()
    {
        Prop?.Dispose();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0043_DisposedField_NullConditional()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    public void Dispose()
    {
        _field?.Dispose();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0043_MultipleUndisposedMembers()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|} : IDisposable
{
    private MyDisposable _field1 = new MyDisposable();
    private MyDisposable _field2 = new MyDisposable();
    public void Dispose()
    {
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("_field1, _field2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0043_PartialType()
        {
            var test1 = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

partial class {|#0:TestClass|} : IDisposable
{
    private MyDisposable _field1 = new MyDisposable();
}";
            var test2 = @"
using System;

partial class {|#1:TestClass|}
{
    private MyDisposable _field2 = new MyDisposable();
    public void Dispose()
    {
        _field1.Dispose();
    }
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("_field2");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(1)
                .WithArguments("_field2");

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { test1, test2 }
                }
            };
            test.ExpectedDiagnostics.Add(expected1);
            test.ExpectedDiagnostics.Add(expected2);
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA0045_MissingIDisposableInterface()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class {|#0:TestClass|}
{
    private MyDisposable _field = new MyDisposable();
    public void Dispose()
    {
        _field.Dispose();
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface)
                .WithLocation(0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
