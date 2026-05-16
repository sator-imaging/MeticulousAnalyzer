namespace AnalyzerCheck;

public class MyClassInt { public MyClassInt(int i) { } public void M(int i) { } }
public class MyClassStr { public MyClassStr(string s) { } public void M(string s) { } }
public class MyClassChar { public MyClassChar(char c) { } public void M(char c) { } }

public class ArgumentAnalyzerDebug
{
    public void Test()
    {
        var mci = new MyClassInt(i: 0);
        var mcs = new MyClassStr("");
        var mcc = new MyClassChar(' ');

        // 1. string method (System.String)
        // int
        "".Substring(default);
        "".Substring(default(int));
        // string
        string.Intern(null);
        string.Intern(default);
        string.Intern(default(string));
        // char
        "".Trim((char)default);
        "".Trim(default(char));

        // 2. string constructor (System.String)
        // int (count)
        new string('a', default);
        new string('a', default(int));
        // string (value)
        new string((char[])null);
        new string(default(char[]));
        new string((char[])default);
        // char (c)
        new string(default, 1);
        new string(default(char), 1);

        // 3. MyClass method
        // int
        mci.M(default);
        mci.M(default(int));
        // string
        mcs.M(null);
        mcs.M(default);
        mcs.M(default(string));
        // char
        mcc.M(default);
        mcc.M(default(char));

        // 4. MyClass constructor
        // int
        new MyClassInt(default);
        new MyClassInt(default(int));
        // string
        new MyClassStr(null);
        new MyClassStr(default);
        new MyClassStr(default(string));
        // char
        new MyClassChar(default);
        new MyClassChar(default(char));
    }
}
