namespace AnalyzerSandbox
{
    class Demo
    {
        readonly struct ReadOnlyS { }
        struct MutableS { }

        static object Create() => new object();
        static void UseRefType(object value) { }
        static void UseIn(in MutableS value) { }
        static void UseReadOnly(ReadOnlyS value) { }
        public int this[string key] => 0;

        void Test(
            int param,
            int mut_param,
            MutableS s,
            ReadOnlyS rs,
            ref int refValue,
            out int result
        )
        {
            result = 0;  // Allowed: assignment to `out` parameter

            param += 1;      // Reported: parameter assignment
            mut_param += 1;  // Allowed: `mut_` prefix on parameter

            int foo = 0;
            foo = 1;     // Reported: local assignment
            foo++;       // Reported: local increment

            var (x, y) = (42, 310);  // Allowed: var (...) is allowed
            (x, y) = (42, 310);      // Reported: deconstruction assignment
            (x, var z) = (42, 310);  // Reported: mixed deconstruction causes error
                                     //           For Unity compatibility, `var z` also get error

            // Allowed: assignment in for-header
            int i;
            for (i = 0; i < 10; i++)
            {
                i += 0;  // Reported: not in for-header
            }

            int mut_counter = 0;
            mut_counter = 1;  // Allowed: `mut_` prefix

            int.TryParse("1", out var parsed);  // Allowed: out declaration at call site
            int.TryParse("1", out parsed);      // Reported: out overwrites variable

            int.TryParse("1", out var mut_parsed);
            int.TryParse("1", out mut_parsed);  // Allowed: `mut_` prefix

            string key = "A";
            var indexer = new Demo();
            _ = indexer[key];  // Allowed: string is treated readonly-struct
            indexer = new();   // Reported: local assignment (reference type)

            UseIn(s);                  // Allowed: callee parameter is `in`
            UseReadOnly(rs);           // Allowed: readonly struct with no modifier
            UseRefType(Create());      // Allowed: argument value is invocation
            UseRefType(new object());  // Allowed: argument value is object creation
        }
    }
}
