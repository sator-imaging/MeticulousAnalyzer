// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#:property LangVersion=latest

#:package BenchmarkDotNet@0.15.8

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;


BenchmarkRunner.Run<BurstLinqBenchmarks>(args: args);


[MemoryDiagnoser]
[ShortRunJob]
public class BurstLinqBenchmarks
{
    [Params(100, 1000)]
    public int Size;

    int[] _array = null!;

    [GlobalSetup]
    public void Setup()
    {
        _array = new int[Size];
        for (int i = 0; i < Size; i++)
            _array[i] = i;
    }


    /*  Any (with predicate)  ================================================================ */

    [Benchmark]
    public bool Any_BurstLinq()
    {
        var source = _array;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] > Size / 2)
                return true;
        }
        return false;
    }

    [Benchmark]
    public bool Any_SystemLinq()
    {
        return _array.Any(x => x > Size / 2);
    }


    /*  FirstOrDefault (with predicate)  ================================================================ */

    [Benchmark]
    public int FirstOrDefault_BurstLinq()
    {
        var source = _array;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] > Size / 2)
                return source[i];
        }
        return default;
    }

    [Benchmark]
    public int FirstOrDefault_SystemLinq()
    {
        return _array.FirstOrDefault(x => x > Size / 2);
    }


    /*  Where + Count  ================================================================ */

    [Benchmark]
    public int WhereCount_BurstLinq()
    {
        var source = _array;
        int count = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] % 2 == 0)
                count++;
        }
        return count;
    }

    [Benchmark]
    public int WhereCount_SystemLinq()
    {
        return _array.Where(x => x % 2 == 0).Count();
    }


    /*  Contains  ================================================================ */

    [Benchmark]
    public bool Contains_BurstLinq()
    {
        var source = _array;
        int value = Size - 1;
        for (int i = 0; i < source.Length; i++)
        {
            if (EqualityComparer<int>.Default.Equals(source[i], value))
                return true;
        }
        return false;
    }

    [Benchmark]
    public bool Contains_SystemLinq()
    {
        return _array.Contains(Size - 1);
    }


    /*  Select + ToArray  ================================================================ */

    [Benchmark]
    public int[] SelectToArray_BurstLinq()
    {
        var source = _array;
        var result = new int[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = source[i] * 2;
        }
        return result;
    }

    [Benchmark]
    public int[] SelectToArray_SystemLinq()
    {
        return _array.Select(x => x * 2).ToArray();
    }
}
