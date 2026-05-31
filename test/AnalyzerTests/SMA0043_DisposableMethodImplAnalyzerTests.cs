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
    public class SMA0043_DisposableMethodImplAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0043_Violation_UndisposedField()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable {|#1:_field|} = new MyDisposable();
    public void Dispose()
    {
    }
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable TestClass._field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1);
        }

        [TestMethod]
        public async Task SMA0043_Compliant_DisposedField()
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
        public async Task SMA0043_Compliant_UndisposedProperty()
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
        public async Task SMA0043_Violation_FullDisposePattern()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable {|#1:_field|} = new MyDisposable();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable TestClass._field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1);
        }

        [TestMethod]
        public async Task SMA0043_Violation_ExplicitInterface()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable {|#1:_field|} = new MyDisposable();
    void IDisposable.Dispose()
    {
    }
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable TestClass._field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1);
        }

        [TestMethod]
        public async Task SMA0043_Compliant_ExplicitInterface_Disposed()
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
        public async Task SMA0043_Compliant_ExpressionBodiedProperty()
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
        public async Task SMA0043_Compliant_ExpressionBodiedProperty_Disposed()
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
        public async Task SMA0043_Compliant_DisposedField_NullConditional()
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
        public async Task SMA0043_Violation_MultipleUndisposedMembers()
        {
            var test = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

class TestClass : IDisposable
{
    private MyDisposable {|#1:_field1|} = new MyDisposable();
    private MyDisposable {|#2:_field2|} = new MyDisposable();
    public void Dispose()
    {
    }
}";
            var expected1 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable TestClass._field1");
            var expected2 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 2)
                .WithArguments("MyDisposable TestClass._field2");
            await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
        }

        [TestMethod]
        public async Task SMA0043_Violation_PartialType()
        {
            var test1 = @"
using System;

class MyDisposable : IDisposable { public void Dispose() {} }

partial class TestClass : IDisposable
{
    private MyDisposable _field1 = new MyDisposable();
}";
            var test2 = @"
using System;

partial class TestClass
{
    private MyDisposable {|#2:_field2|} = new MyDisposable();
    public void Dispose()
    {
        _field1.Dispose();
    }
}";
            var expected3 = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 2)
                .WithArguments("MyDisposable TestClass._field2");

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { test1, test2 }
                }
            };
            test.ExpectedDiagnostics.Add(expected3);
            await test.RunAsync();
        }

        [TestMethod]
        public async Task SMA0043_Compliant_SuppressionCommentOnField()
        {
            var test = @"
using System;

class MyDisposable : IDisposable
{
    // Don't dispose
    IDisposable _disposable;

    public void Dispose() { }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0043_Violation_UndisposedField_NoSuppression()
        {
            var test = @"
using System;

class MyDisposable : IDisposable
{
    IDisposable {|#0:_disposable|};

    public void Dispose() { }
}";
            var expected = VerifyCS.Diagnostic(DisposableMethodImplAnalyzer.RuleId_UndisposedMember)
                .WithLocation(markupKey: 0)
                .WithArguments("IDisposable MyDisposable._disposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

    }
}
