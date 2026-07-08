using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = StaticMemberAnalyzer.Tests.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.DebugAssertAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers;

namespace SatorImaging.StaticMemberAnalyzer.Tests.AnalyzerTests
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
        public async Task SMA8003_Violation_PublicFieldInitializer()
        {
            var test = @"
public class C
{
    public static object Assert(bool b) => null;
    public object X = {|#0:Assert(true)|};
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
    public void Assert(bool b) {}
    public void M()
    {
        {|#0:Assert(true)|};
    }
}";
            // Analysis logic: If method call of Debug.Assert found (no argument check; no namespace check; just check the name)
            await VerifyCS.VerifyAnalyzerAsync(test,
                VerifyCS.Diagnostic(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi).WithLocation(0));
        }
    }
}
