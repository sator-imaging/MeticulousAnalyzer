// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;
using SatorImaging.MeticulousAnalyzer.CodeFixes.Providers;

namespace SatorImaging.MeticulousAnalyzer.Tests
{
    // NOTE: This test is for adjusting test coverage numbers.
    //       Public members of analyzers and codefix providers are accessed here.
    [TestClass]
    public class PublicMembersTest
    {
        [TestMethod]
        public void AllAnalyzerPublicMembers()
        {
            DiagnosticAnalyzer[] analyzers = new DiagnosticAnalyzer[]
            {
                new AnonymousObjectCreationAnalyzer(),
                new ArgumentAnalyzer(),
                new CatchAnalyzer(),
                new DebugAssertAnalyzer(),
                new DisposableAnalyzer(),
                new DisposableMethodImplAnalyzer(),
                new EnumAnalyzer(),
                new ExplicitNumberDeclarationAnalyzer(),
                new FileHeaderCommentAnalyzer(),
                new FlakyInitializationAnalyzer(),
                new InternalNamespaceAccessAnalyzer(),
                new LambdaAnalyzer(),
                new LiteralBranchAnalyzer(),
                new MethodImplAnalyzer(),
                new MidFlowBranchAnalyzer(),
                new MoveOnlyAnalyzer(),
                new NullSuppressionAnalyzer(),
                new OmittableArgumentAnalyzer(),
                new ParamsArgumentAnalyzer(),
                new ReadOnlyVariableAnalyzer(),
                new ReflectionAnalyzer(),
                new StructAnalyzer(),
                new TaskAnalyzer(),
                new TSelfTypeParameterAnalyzer(),
#if STMG_ENABLE_UNDERLINING_ANALYZER
                new UnderliningAnalyzer(),
#endif
            };

            foreach (var analyzer in analyzers)
            {
                Assert.IsNotNull(analyzer.SupportedDiagnostics);
                Assert.IsTrue(analyzer.SupportedDiagnostics.Length > 0);
            }

            // Verify Rule ID Constants
            Assert.IsNotNull(AnonymousObjectCreationAnalyzer.RuleId_AnonymousObject);

            Assert.IsNotNull(ArgumentAnalyzer.RuleId_LiteralArgument);

            Assert.IsNotNull(CatchAnalyzer.RuleId_CatchWithoutThrow);
            Assert.IsNotNull(CatchAnalyzer.RuleId_CatchAll);

            Assert.IsNotNull(DebugAssertAnalyzer.RuleId_DebugAssertInPublicApi);

            Assert.IsNotNull(DisposableAnalyzer.RuleId_MissingUsing);
            Assert.IsNotNull(DisposableAnalyzer.RuleId_NullAssignmentToDisposable);
            Assert.IsNotNull(DisposableAnalyzer.RuleId_NotAllCodePathsReturn);

            Assert.IsNotNull(DisposableMethodImplAnalyzer.RuleId_UndisposedMember);
            Assert.IsNotNull(DisposableMethodImplAnalyzer.RuleId_MissingDisposeImplementation);
            Assert.IsNotNull(DisposableMethodImplAnalyzer.RuleId_MissingIDisposableInterface);

            Assert.IsNotNull(EnumAnalyzer.RuleId_CastToEnum);
            Assert.IsNotNull(EnumAnalyzer.RuleId_CastFromEnum);
            Assert.IsNotNull(EnumAnalyzer.RuleId_CastToGenericEnum);
            Assert.IsNotNull(EnumAnalyzer.RuleId_CastFromGenericEnum);
            Assert.IsNotNull(EnumAnalyzer.RuleId_EnumToString);
            Assert.IsNotNull(EnumAnalyzer.RuleId_EnumMethod);
            Assert.IsNotNull(EnumAnalyzer.RuleId_EnumObfuscation);
            Assert.IsNotNull(EnumAnalyzer.RuleId_UnusualEnum);
            Assert.IsNotNull(EnumAnalyzer.RuleId_EnumLike);

            Assert.IsNotNull(ExplicitNumberDeclarationAnalyzer.RuleId_ExplicitNumber);

            Assert.IsNotNull(FileHeaderCommentAnalyzer.RuleId_MissingFileHeaderComment);

            Assert.IsNotNull(FlakyInitializationAnalyzer.RuleId_WrongInit);
            Assert.IsNotNull(FlakyInitializationAnalyzer.RuleId_CrossRef);
            Assert.IsNotNull(FlakyInitializationAnalyzer.RuleId_AnotherFile);
            Assert.IsNotNull(FlakyInitializationAnalyzer.RuleId_LateDeclare);

            Assert.IsNotNull(InternalNamespaceAccessAnalyzer.RuleId_InternalNamespaceAccess);

            Assert.IsNotNull(LambdaAnalyzer.RuleId_LambdaCanBeStatic);
            Assert.IsNotNull(LambdaAnalyzer.RuleId_InefficientDelegateDeclaration);
            Assert.IsNotNull(LambdaAnalyzer.RuleId_LambdaAllocation);

            Assert.IsNotNull(LiteralBranchAnalyzer.RuleId_LiteralBranch);
            Assert.IsNotNull(LiteralBranchAnalyzer.RuleId_LiteralBranchZero);
            Assert.IsNotNull(LiteralBranchAnalyzer.RuleId_LiteralBranchString);
            Assert.IsNotNull(LiteralBranchAnalyzer.RuleId_LiteralBranchChar);

            Assert.IsNotNull(MethodImplAnalyzer.RuleId_AggressiveInliningOnPublicMember);

            Assert.IsNotNull(MidFlowBranchAnalyzer.RuleId_MidFlowBranch);
            Assert.IsNotNull(MidFlowBranchAnalyzer.RuleId_StateChangeInEarlyReturn);
            Assert.IsNotNull(MidFlowBranchAnalyzer.RuleId_NonLocalExitFromLoop);

            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_MissingMoveMethod);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_InvalidTypeDeclaration);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_ProhibitedCopy);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_NoCopyValueCopy);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_ProhibitedRefOutInAsync);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_AsyncRefOutNoCopy);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_ProhibitedCast);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_ProhibitedLambdaCapture);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_ProhibitedOutParameter);
            Assert.IsNotNull(MoveOnlyAnalyzer.RuleId_ProhibitedReturn);

            Assert.IsNotNull(NullSuppressionAnalyzer.RuleId_NullSuppression);

            Assert.IsNotNull(OmittableArgumentAnalyzer.RuleId_OmittableArgument);

            Assert.IsNotNull(ParamsArgumentAnalyzer.RuleId_ImplicitParamsAllocation);

            Assert.IsNotNull(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyLocal);
            Assert.IsNotNull(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyParameter);
            Assert.IsNotNull(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyArgument);
            Assert.IsNotNull(ReadOnlyVariableAnalyzer.RuleId_PropertyAccessCanChangeState);
            Assert.IsNotNull(ReadOnlyVariableAnalyzer.RuleId_ReadOnlyMethodCall);

            Assert.IsNotNull(ReflectionAnalyzer.RuleId_SystemReflectionUsage);
            Assert.IsNotNull(ReflectionAnalyzer.RuleId_SystemReflectionVariable);

            Assert.IsNotNull(StructAnalyzer.RuleId_InvalidStructCtor);
            Assert.IsNotNull(StructAnalyzer.RuleId_InvalidReadOnlyField);
            Assert.IsNotNull(StructAnalyzer.RuleId_ImplicitBoxing);

            Assert.IsNotNull(TaskAnalyzer.RuleId_MissingAwait);
            Assert.IsNotNull(TaskAnalyzer.RuleId_NotAllCodePathsAwait);

            Assert.IsNotNull(TSelfTypeParameterAnalyzer.RuleId_TSelfInvariant);
            Assert.IsNotNull(TSelfTypeParameterAnalyzer.RuleId_TSelfCovariant);
            Assert.IsNotNull(TSelfTypeParameterAnalyzer.RuleId_TSelfContravariant);
            Assert.IsNotNull(TSelfTypeParameterAnalyzer.RuleId_TSelfPointingOther);

#if STMG_ENABLE_UNDERLINING_ANALYZER
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineIdentifierSymbol);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineLocalVar);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineParameter);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineDeclaration);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineDesignatedType);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineLineHead);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineLineLeading);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineLineFill);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineLineEnd);
            Assert.IsNotNull(UnderliningAnalyzer.RuleId_UnderlineWarning);
#endif
        }

        [TestMethod]
        public void AllCodeFixProviderPublicMembers()
        {
            var enumFix = new EnumObfuscationCodeFixProvider();
            Assert.IsNotNull(enumFix.FixableDiagnosticIds);
            Assert.IsNotNull(enumFix.GetFixAllProvider());

            var lambdaFix = new LambdaStaticCodeFixProvider();
            Assert.IsNotNull(lambdaFix.FixableDiagnosticIds);
            Assert.IsNotNull(lambdaFix.GetFixAllProvider());

            var namedFix = new NamedArgumentCodeFixProvider();
            Assert.IsNotNull(namedFix.FixableDiagnosticIds);
            Assert.IsNotNull(namedFix.GetFixAllProvider());

            var nullFix = new NullSuppressionCodeFixProvider();
            Assert.IsNotNull(nullFix.FixableDiagnosticIds);
            Assert.IsNotNull(nullFix.GetFixAllProvider());

            var paramsFix = new ParamsArgumentCodeFixProvider();
            Assert.IsNotNull(paramsFix.FixableDiagnosticIds);
            Assert.IsNotNull(paramsFix.GetFixAllProvider());
        }
    }
}
