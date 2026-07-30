// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using System.Threading.Tasks;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpAnalyzerVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.MethodImplAnalyzer>;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA7020_MethodImplAnalyzerTests
    {
        [TestMethod]
        public async Task SMA7020_Violation_Method_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
    public void MyMethod()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyMethod");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Violation_Constructor_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
    public TestClass()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("TestClass");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Violation_PropertyAccessors_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    private int _val;
    public int MyProp
    {
        [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        get => _val;
        [{|#1:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        set => _val = value;
    }
}
";
            var expectedGet = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyProp.get");
            var expectedSet = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 1)
                .WithArguments("MyProp.set");
            await VerifyCS.VerifyAnalyzerAsync(test, expectedGet, expectedSet);
        }

        [TestMethod]
        public async Task SMA7020_Violation_IndexerAccessors_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    public int this[int index]
    {
        [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        get => index;
        [{|#1:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        set { }
    }
}
";
            var expectedGet = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("this.get");
            var expectedSet = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 1)
                .WithArguments("this.set");
            await VerifyCS.VerifyAnalyzerAsync(test, expectedGet, expectedSet);
        }

        [TestMethod]
        public async Task SMA7020_Violation_EventAccessors_WithAggressiveInlining()
        {
            var test = @"
using System;
using System.Runtime.CompilerServices;

public class TestClass
{
    private EventHandler _myEvent;
    public event EventHandler MyEvent
    {
        [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        add => _myEvent += value;
        [{|#1:MethodImpl(MethodImplOptions.AggressiveInlining)|}]
        remove => _myEvent -= value;
    }
}
";
            var expectedAdd = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyEvent.add");
            var expectedRemove = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 1)
                .WithArguments("MyEvent.remove");
            await VerifyCS.VerifyAnalyzerAsync(test, expectedAdd, expectedRemove);
        }

        [TestMethod]
        public async Task SMA7020_Violation_Method_WithCombinedInliningFlags()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [{|#0:MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.NoOptimization)|}]
    public void MyMethod()
    {
    }
}
";
            var expected = VerifyCS.Diagnostic(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember)
                .WithLocation(markupKey: 0)
                .WithArguments("MyMethod");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA7020_Compliant_Method_WithoutAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void MyMethod()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7020_Compliant_InternalMethod_WithAggressiveInlining()
        {
            var test = @"
using System.Runtime.CompilerServices;

internal class TestClass
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MyMethod()
    {
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7020_Compliant_PublicProperty_WithInternalAccessor()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    public int MyProp
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal get => 42;
        set { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA7020_Compliant_PublicProperty_WithInternalSetter()
        {
            var test = @"
using System.Runtime.CompilerServices;

public class TestClass
{
    public int MyProp
    {
        get => 42;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal set { }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
