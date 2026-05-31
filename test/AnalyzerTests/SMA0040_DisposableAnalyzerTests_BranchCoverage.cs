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
    public class SMA0040_DisposableAnalyzerTests_BranchCoverage
    {
        // ===================================================================
        // ISwitchExpressionArmOperation path (IsSyntaxIgnorable)
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_SwitchExpressionArm_MultipleDisposableArms()
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
            var d = {|#0:value switch
            {
                1 => new MyDisposable(),
                2 => new MyDisposable(),
                _ => null,
            }|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected0);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_SwitchExpressionArm_WithUsing()
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
                2 => new MyDisposable(),
                _ => null,
            };
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // SuppressNullableWarningExpression unwrap (IsSyntaxIgnorable while loop)
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_NullSuppression_WithoutUsing()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable()!|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_NullSuppression_WithUsing()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable()!;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ParenthesizedNullSuppression_WithoutUsing()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = {|#0:(new MyDisposable())!|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // ===================================================================
        // TryUnwrapSafeConversion - chained cast (do-while loop iterates)
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_ChainedSafeCast_ObjectCreationWithCast()
        {
            // Tests TryUnwrapSafeConversion: when a new disposable is cast to a
            // disposable interface, the creation itself still triggers a violation
            // if the using detection cannot reach through the cast in syntax.
            var test = @"
using System;

namespace Test
{
    class DualDisposable : IDisposable, IAsyncDisposable
    {
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() => default;
    }

    class Program
    {
        void Method()
        {
            using var d = (IDisposable){|#0:new DualDisposable()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("DualDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ChainedCast_IntermediateNonDisposable()
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
        void Method()
        {
            var d = {|#0:(IDisposable){|#1:(object){|#2:new MyDisposable()|}|}|};
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
            var expected2 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 2)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2);
        }

        // ===================================================================
        // IMemberReferenceOperation - non-disposable type (returns true)
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_MemberReference_NonDisposableProperty()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public string Name { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            var s = d.Name;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // IMemberReferenceOperation - disposable type + IInvocationOperation parent
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_MemberReference_DisposableChainedNonDisposableMethod()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable
    {
        public MyDisposable Self => this;
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            var s = d.Self.ToString();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // IMemberReferenceOperation - disposable type + IMemberReferenceOperation parent
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_MemberReference_DisposableChainedToMemberRef()
        {
            var test = @"
using System;

namespace Test
{
    class InnerDisposable : IDisposable
    {
        public string Name { get; set; }
        public void Dispose() { }
    }

    class MyDisposable : IDisposable
    {
        public InnerDisposable Inner { get; set; }
        public void Dispose() { }
    }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            var n = d.Inner.Name;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // IArgumentOperation - !isCreationOp path
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_NonCreation_PassedAsArgument()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Consume(IDisposable d) { }

        void Method()
        {
            using var d = new MyDisposable();
            Consume(d);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // IForEachLoopOperation path - method invocation returning disposable
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_ForEachLoop_DisposableEnumerator()
        {
            var test = @"
using System;
using System.Collections;
using System.Collections.Generic;

namespace Test
{
    class DisposableEnumerable : IEnumerable<int>, IDisposable
    {
        public void Dispose() { }
        public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    class Program
    {
        DisposableEnumerable GetEnumerable() => new DisposableEnumerable();

        void Method()
        {
            foreach (var item in GetEnumerable())
            {
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // AccessorDeclarationSyntax path in IsLocalVariableReturned
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyGetter_ReturnsLocal()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable MyProp
        {
            get
            {
                var d = new MyDisposable();
                return d;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // expressionBody with ThrowExpressionSyntax in IsLocalVariableReturned
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_Method_ExpressionBody_ThrowExpression()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable Method() => throw new NotImplementedException();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // body with ThrowStatementSyntax in IsLocalVariableReturned
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_Method_BodyWithThrow_LocalNotReturned()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable Method(bool condition)
        {
            var d = {|#0:new MyDisposable()|};
            if (condition) throw new InvalidOperationException();
            return d;
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // ===================================================================
        // IPropertyReferenceOperation IsIndexer=true in AnalyzeOtherSyntax
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyIndexer_AssignToFieldIndexer()
        {
            var test = @"
using System;
using System.Collections.Generic;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        Dictionary<int, MyDisposable> _dict = new Dictionary<int, MyDisposable>();

        void Method()
        {
            _dict[0] = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_PropertyIndexer_LocalVariable()
        {
            var test = @"
using System;
using System.Collections.Generic;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var dict = new Dictionary<int, MyDisposable>();
            {|#0:dict[0]|} = {|#1:new MyDisposable()|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        // ===================================================================
        // IArrayElementReferenceOperation - field array vs local array
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_ArrayElement_AssignToFieldArray()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable[] _arr = new MyDisposable[10];

        void Method()
        {
            _arr[0] = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_ArrayElement_AssignToLocalArray()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var arr = new MyDisposable[10];
            {|#0:arr[0]|} = {|#1:new MyDisposable()|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            var expected1 = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 1)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        // ===================================================================
        // Arrow return detection in using/foreach block
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_ArrowReturn_MethodReturnsDisposable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable Method() => new MyDisposable();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Compliant_ArrowReturn_InvocationReturnsDisposable()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable Create() => new MyDisposable();
        MyDisposable Method() => Create();
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // controlFlow.EndPointIsReachable / ReturnStatements.IsEmpty
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_ControlFlowReturnStatementsEmpty()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            var d = {|#0:new MyDisposable()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // ===================================================================
        // expressionBody returning identifier in AccessorDeclarationSyntax
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyGetter_ExpressionBody()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable _field = new MyDisposable();
        MyDisposable MyProp => _field;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // returnStatements.Length != allReturnStatements.Length (yield return)
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_MethodWithMultipleReturns_SomeNotReturningLocal()
        {
            var test = @"
using System;

#nullable enable

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable? Method(int value)
        {
            var {|#0:d = new MyDisposable()|};
            if (value == 1)
            {
                return d;
            }
            return null;
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_NotAllCodePathsReturn)
                .WithLocation(markupKey: 0)
                .WithArguments("d");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // ===================================================================
        // IDiscardOperation - suppressed by comment
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_Discard_SuppressedByComment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            // Don't dispose
            _ = new MyDisposable();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0040_Violation_Discard_NotSuppressedByComment()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            _ = {|#0:new MyDisposable()|};
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // ===================================================================
        // Interlocked.CompareExchange path
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_InterlockedCompareExchange_NewCreation()
        {
            var test = @"
using System;
using System.Threading;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        MyDisposable _field;

        void Method()
        {
            Interlocked.CompareExchange(ref _field, new MyDisposable(), null);
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // PropertyGetter with body - returns on all paths
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_PropertyGetter_BodyReturnsOnAllPaths()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        bool _flag;

        MyDisposable MyProp
        {
            get
            {
                var d = new MyDisposable();
                if (_flag)
                {
                    return d;
                }
                return d;
            }
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // Cast in binary operation (IBinaryOperation) after unsafe cast
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Compliant_UnsafeCast_InBinaryOperation()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            bool check = (object)d == null;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // ===================================================================
        // Cast in IsPattern operation after unsafe cast - expects violation
        // ===================================================================

        [TestMethod]
        public async Task SMA0040_Violation_UnsafeCast_InIsPattern()
        {
            var test = @"
using System;

namespace Test
{
    class MyDisposable : IDisposable { public void Dispose() { } }

    class Program
    {
        void Method()
        {
            using var d = new MyDisposable();
            bool check = {|#0:(object)d|} is string;
        }
    }
}
";
            var expected = VerifyCS.Diagnostic(DisposableAnalyzer.RuleId_MissingUsing)
                .WithLocation(markupKey: 0)
                .WithArguments("MyDisposable");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
