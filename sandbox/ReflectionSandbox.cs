using System;
using System.Reflection;

namespace AnalyzerSandbox;

internal class ReflectionTests
{
    static void Tests(object foo)
    {
        // WARNING: return type is declared in System.Reflection
        foo.GetType().GetMembers();
        typeof(ReflectionTests).GetMembers();

        // WARNING: type references
        MethodInfo explicitLocal = null;
        _ = typeof(BindingFlags);

        // WARNING: `var` local holding reflection type
        var inferredLocal = typeof(ReflectionTests).GetMethod("Tests");

        // WARNING: member typed as System.Reflection type
        _ = typeof(ReflectionTests).Assembly;

        // WARNING: member reference on reflection-typed instance
        inferredLocal.Invoke(null, new[] { foo });
        _ = explicitLocal.Name;

        // OK: System.Type itself is not declared in System.Reflection
        var type = foo.GetType();
        _ = type.Name;
        _ = type.FullName;
    }

    // OK: attribute types from System.Reflection are exempt
    [Obfuscation(Exclude = true)]
    enum ExemptedAttributeUsage { }
}
