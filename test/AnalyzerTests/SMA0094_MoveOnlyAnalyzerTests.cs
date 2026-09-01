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
    public class SMA0094_MoveOnlyAnalyzerTests
    {
        [TestMethod]
        public async Task SMA0094_Violation_CastToObjectOrInterfaceWithoutMove()
        {
            var test = @"
namespace Test
{
    public interface ICustomInterface { }

    struct MoveOnlyStruct : ICustomInterface
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Method(MoveOnlyStruct moveOnly)
        {
            object obj = {|#0:moveOnly|};
            ICustomInterface iface = {|#1:moveOnly|};
            object explicitObj = {|#2:(object)moveOnly|};
            ICustomInterface explicitIface = {|#3:(ICustomInterface)moveOnly|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct", "object");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct", "ICustomInterface");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct", "object");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct", "ICustomInterface");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }

        [TestMethod]
        public async Task SMA0094_Compliant_CastToObjectOrInterfaceWithMove()
        {
            var test = @"
namespace Test
{
    public interface ICustomInterface { }

    struct MoveOnlyStruct : ICustomInterface
    {
        public MoveOnlyStruct Move() => this;
    }

    class Program
    {
        void Method(MoveOnlyStruct moveOnly)
        {
            object obj = moveOnly.Move();
            ICustomInterface iface = moveOnly.Move();
            object explicitObj = (object)moveOnly.Move();
            ICustomInterface explicitIface = (ICustomInterface)moveOnly.Move();
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0094_Compliant_PublicMoveMethodExemptedFromChecks()
        {
            var test = @"
namespace Test
{
    public interface ICustomInterface { }

    struct MoveOnlyStruct : ICustomInterface
    {
        public MoveOnlyStruct Move()
        {
            object obj = this;
            ICustomInterface iface = (ICustomInterface)this;
            return this;
        }
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SMA0094_RefLocalAndArcGetRef_CastToObjectOrInterface()
        {
            var test = @"
namespace Test
{
    public interface ICustomInterface { }

    struct MoveOnlyStruct : ICustomInterface
    {
        public MoveOnlyStruct Move() => this;
    }

    class Arc
    {
        private MoveOnlyStruct _value;
        public ref MoveOnlyStruct GetRef() => ref _value;
    }

    class Program
    {
        void Method(Arc arc)
        {
            ref var refLocal = ref arc.GetRef();

            object obj1 = {|#0:arc.GetRef()|};
            ICustomInterface iface1 = {|#1:arc.GetRef()|};
            object obj2 = {|#2:refLocal|};
            ICustomInterface iface2 = {|#3:refLocal|};

            object obj3 = arc.GetRef().Move();
            ICustomInterface iface3 = arc.GetRef().Move();
            object obj4 = refLocal.Move();
            ICustomInterface iface4 = refLocal.Move();
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast).WithLocation(markupKey: 0).WithArguments("MoveOnlyStruct", "object");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast).WithLocation(markupKey: 1).WithArguments("MoveOnlyStruct", "ICustomInterface");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast).WithLocation(markupKey: 2).WithArguments("MoveOnlyStruct", "object");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast).WithLocation(markupKey: 3).WithArguments("MoveOnlyStruct", "ICustomInterface");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3);
        }
    }
}
