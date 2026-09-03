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
        public async Task SMA0094_Violation_ParameterLocalAndRefLocalCast()
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
        private MoveOnlyStruct _field;

        void Method(MoveOnlyStruct param)
        {
            var local = {|#0:param|};
            ref var refLocal = ref {|#1:_field|};

            object obj1 = {|#2:param|};
            object obj2 = {|#3:local|};
            object obj3 = {|#4:refLocal|};

            var explicit1 = {|#5:(object)param|};
            var explicit2 = {|#6:(object)local|};
            var explicit3 = {|#7:(object)refLocal|};
        }
    }
}
";
            var expected0 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 0)
                .WithArguments("MoveOnlyStruct");
            var expected1 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCopy)
                .WithLocation(markupKey: 1)
                .WithArguments("MoveOnlyStruct");
            var expected2 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 2)
                .WithArguments("MoveOnlyStruct", "object");
            var expected3 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 3)
                .WithArguments("MoveOnlyStruct", "object");
            var expected4 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 4)
                .WithArguments("MoveOnlyStruct", "object");
            var expected5 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 5)
                .WithArguments("MoveOnlyStruct", "object");
            var expected6 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 6)
                .WithArguments("MoveOnlyStruct", "object");
            var expected7 = VerifyCS.Diagnostic(MoveOnlyAnalyzer.RuleId_ProhibitedCast)
                .WithLocation(markupKey: 7)
                .WithArguments("MoveOnlyStruct", "object");

            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1, expected2, expected3, expected4, expected5, expected6, expected7);
        }
    }
}
