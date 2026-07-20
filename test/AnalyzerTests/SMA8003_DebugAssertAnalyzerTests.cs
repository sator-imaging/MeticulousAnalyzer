// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = SatorImaging.MeticulousAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.MeticulousAnalyzer.Analysis.Analyzers.DebugAssertAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;

namespace SatorImaging.MeticulousAnalyzer.Tests.AnalyzerTests
{
    [TestClass]
    public class SMA8003_DebugAssertAnalyzerTests
    {
        [TestMethod]
        public async Task SMA8003_Violation_PublicMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public void M()
    {
        {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicExpressionBodiedMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public void M() => {|#0:Debug.Assert(true)|};
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_InternalExpressionBodiedMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    internal void M() => Debug.Assert(true);
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicExpressionBodiedProperty()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public bool P => {|#0:Debug.Assert(true)|} == null;
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0),
                DiagnosticResult.CompilerError("CS0019").WithSpan(4, 22, 4, 48).WithArguments("==", "void", "<null>"));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_InternalExpressionBodiedProperty()
        {
            var test = @"using System.Diagnostics;
public class C
{
    internal bool P => Debug.Assert(true) == null;
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                DiagnosticResult.CompilerError("CS0019").WithSpan(4, 24, 4, 50).WithArguments("==", "void", "<null>"));
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicFieldInitializer()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public readonly object X = {|#0:Debug.Assert(true)|};
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0),
                DiagnosticResult.CompilerError("CS0029").WithSpan(4, 32, 4, 50).WithArguments("void", "object"));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_InternalFieldInitializer()
        {
            var test = @"using System.Diagnostics;
public class C
{
    internal readonly object X = Debug.Assert(true);
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                DiagnosticResult.CompilerError("CS0029").WithSpan(4, 34, 4, 52).WithArguments("void", "object"));
        }

        [TestMethod]
        public async Task SMA8003_Violation_ProtectedMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    protected void M()
    {
        {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_ProtectedInternalMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    protected internal void M()
    {
        {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_InternalMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    internal void M()
    {
        Debug.Assert(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8003_Compliant_PrivateMethod()
        {
            var test = @"using System.Diagnostics;
public class C
{
    private void M()
    {
        Debug.Assert(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicProperty()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public int P
    {
        get
        {
            {|#0:Debug.Assert(true)|};
            return 0;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicConstructor()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public C()
    {
        {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_ProtectedConstructor()
        {
            var test = @"using System.Diagnostics;
public class C
{
    protected C()
    {
        {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_InternalConstructor()
        {
            var test = @"using System.Diagnostics;
public class C
{
    internal C()
    {
        Debug.Assert(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8003_Compliant_PrivateConstructor()
        {
            var test = @"using System.Diagnostics;
public class C
{
    private C()
    {
        Debug.Assert(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicEventAccessor()
        {
            var test = @"using System.Diagnostics;
using System;
public class C
{
    public event Action E
    {
        add { {|#0:Debug.Assert(true)|}; }
        remove { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicMethod_Lambda()
        {
            var test = @"using System.Diagnostics;
using System;
public class C
{
    public void M()
    {
        Action a = () => {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicMethod_LocalFunction()
        {
            var test = @"using System.Diagnostics;
public class C
{
    public void M()
    {
        void Local() => {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_PublicInterfaceDefaultMethod()
        {
            var test = @"using System.Diagnostics;
public interface I
{
    void M()
    {
        {|#0:Debug.Assert(true)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_DefaultClassMethod()
        {
            var test = @"using System.Diagnostics;
class C
{
    void M()
    {
        Debug.Assert(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA8003_Violation_OtherAssertCall()
        {
            var test = @"
public class C
{
    private void Assert(bool b) {}
    public void M()
    {
        {|#0:Assert(true)|};
    }
}";
            // Analysis logic: If method call of Assert found (no argument check; no namespace check; just check the name)
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }

        [TestMethod]
        public async Task SMA8003_Violation_StartsWithAssertCall()
        {
            var test = @"
public class C
{
    private void AssertTrue(bool b) {}
    private void AssertEqual(int a, int b) {}
    public void M()
    {
        {|#0:AssertTrue(true)|};
        {|#1:AssertEqual(1, 1)|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0),
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(1));
        }

        [TestMethod]
        public async Task SMA8003_Compliant_ContainsAssertButNotStartsWith()
        {
            var test = @"
public class C
{
    private void MyAssert(bool b) {}
    private void CheckAssert(bool b) {}
    public void M()
    {
        MyAssert(true);
        CheckAssert(true);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
