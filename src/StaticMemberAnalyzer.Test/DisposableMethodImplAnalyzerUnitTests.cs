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

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    public void {|#0:Dispose|}()
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
    public void {|#0:Dispose|}()
    {
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("Prop");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
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
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation)
                .WithLocation(0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0043_FullDisposePattern()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void {|#0:Dispose|}(bool disposing)
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

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    void IDisposable.{|#0:Dispose|}()
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
        public async Task SMA0043_DuckTyping()
        {
            var test = @"
using System;

class MyDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable _field = new MyDisposable();
    public void {|#0:Dispose|}()
    {
    }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(0)
                .WithArguments("_field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0043_ExpressionBodiedProperty_Ignored()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass
{
    public MyDisposable Prop => null;
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
