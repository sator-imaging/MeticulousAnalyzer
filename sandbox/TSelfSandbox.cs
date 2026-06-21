using System;
using System.ComponentModel;

namespace AnalyzerSandbox
{
    [DescriptionAttribute]
    public class Value { }

    /// <summary>
    /// <see cref="ITest{TSelf, T}"/>
    /// <see cref="TSelfWeirdSyntaxTester"/>
    /// <inheritdoc cref="TSelfBase{T, TSelf}"/>
    public interface ITest<TSelf, T> { }
    public class TSelfBase<T, TSelf> { }

    abstract public class AbstractTSelf<TSelf>
            where TSelf : AbstractTSelf<TSelf>, new()
    { }
    class InstanceNest
    {
        class Nest : AbstractTSelf<Nest> { }
        class Nest2 : AbstractTSelf<Nest> { }
    }
    static class StaticNest
    {
        class Nest : AbstractTSelf<Nest> { }
        class Nest2 : AbstractTSelf<Nest> { }
    }

    // chaining TSelf
    class TSelfBase<TSelf> where TSelf : TSelfBase<TSelf> { }
    class TSelfDerived<TSelf, TOther> : TSelfBase<TSelf>  // TSelfBase<> can accept TOther but shows warning.
                                                          // only type parameter 'TSelf' is ignored to being warning
        where TSelf : TSelfDerived<TSelf, TOther>    // change this line will show warning (trivia is ignored)
        where TOther : TSelfDerived<TOther, TOther>
    { }

    public class ClassOK : TSelfBase<byte, ClassOK> { }
    public class ClassNG : TSelfBase<byte, ClassOK> { }
    public struct IFaceOK : ITest<IFaceOK, short> { }
    public struct IFaceNG : ITest<IFaceOK, short> { }
    public class BothOK : TSelfBase<int, BothOK>, ITest<BothOK, long> { }
    public class BothNG : TSelfBase<int, BothOK>, ITest<BothOK, long> { }
    public class IFaceIFaceOK : ITest<IFaceIFaceOK, int> { }
    public class IFaceIFaceNG : ITest<IFaceIFaceOK, int> { }

    // variance check
    interface ICovariance<out TSelf> { }
    interface IContravariance<in TSelf> { }
    class Something { }
    public class VariantBase { }
    // covariant
    public class CovariantOK : ICovariance<object> { }
    public class CovariantOK2 : ICovariance<Object> { }
    public class CovariantOK3 : ICovariance<System.Object> { }
    public class CovariantOK4 : ICovariance<CovariantOK4> { }
    public class CovariantOK5 : VariantBase, ICovariance<VariantBase> { }
    public class CovariantNG : ICovariance<VariantBase> { }    // not inherit
    public class CovariantNG2 : ICovariance<CovariantNG3> { }  // contravariant
    public class CovariantNG3 : CovariantNG2, ICovariance<Something> { }
    // contravariant
    public class ContravariantOK : IContravariance<ContravariantOK2> { }
    public class ContravariantOK2 : ContravariantOK, IContravariance<ContravariantOK2> { }
    public class ContravariantNG : IContravariance<ContravariantOK2> { }
    public class ContravariantNG2 : IContravariance<Something> { }
    public class ContravariantNG3 : IContravariance<object> { }
    // invariant
    public interface ITypeArgConstraint<TSelf> where TSelf : ITypeArgConstraint<TSelf> { }
    public class InvariantOK : ITypeArgConstraint<InvariantOK> { };
    public class InvariantNG : ITypeArgConstraint<InvariantOK> { };

    // generic type
    public interface IGenericTSelf<TSelf, TOther> where TSelf : IGenericTSelf<TSelf, TOther> { }
    public class GenericTSelfOK : IGenericTSelf<GenericTSelfOK, int> { }
    public class GenericTSelfOK2<T, U> : IGenericTSelf<GenericTSelfOK2<T /**/, /**/ U>, U> { }
    public class GenericTSelfNG<T, U> : IGenericTSelf<GenericTSelfNG</**/ T, /**/ int>, T> { }
    public class GenericTSelfNG2 : IGenericTSelf<GenericTSelfOK, int> { }

    // weird trivia
    public class TSelfWeirdSyntaxTester
        :
        TSelfBase<
            int,
            /**/ TSelfWeirdSyntaxTester /* <-- change this to 'int' will show warning */ >,
        ITest</**/ TSelfWeirdSyntaxTester                        /**/, long
            >

    {
    }
}
