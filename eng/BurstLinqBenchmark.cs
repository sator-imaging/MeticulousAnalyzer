// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#:package BenchmarkDotNet@0.15.8
#:property LangVersion=latest
#:property PublishAot=false
#:property ImplicitUsings=false

#:package FUnit.Directives@*
#warning funit include ../src/analysis/BurstLinq.cs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using SatorImaging.StaticMemberAnalyzer;


var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(Job.Default.WithToolchain(InProcessNoEmitToolchain.Instance));

BenchmarkRunner.Run<BurstLinqBenchmarks>(config, args: args);


[MemoryDiagnoser]
public class BurstLinqBenchmarks
{
    [Params(100, 1000)]
    public int Size;

    ImmutableArray<int> _immArray;
    IReadOnlyList<int> _roList = null!;
    IEnumerable<int> _enumerable = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = ImmutableArray.CreateBuilder<int>(Size);
        for (int i = 0; i < Size; i++)
            builder.Add(i);
        _immArray = builder.ToImmutable();
        _roList = (IReadOnlyList<int>)_immArray;
        _enumerable = (IEnumerable<int>)_immArray;
    }


    /*  Any (no predicate)  ================================================================ */

    [Benchmark]
    public bool Any_BurstLinq()
    {
        return _immArray.Any();
    }

    [Benchmark]
    public bool Any_SystemLinq()
    {
        return System.Linq.Enumerable.Any(_immArray);
    }


    /*  Any (with predicate)  ================================================================ */

    [Benchmark]
    public bool AnyPredicate_BurstLinq()
    {
        return _immArray.Any(static x => x > 50);
    }

    [Benchmark]
    public bool AnyPredicate_SystemLinq()
    {
        return System.Linq.Enumerable.Any(_immArray, static x => x > 50);
    }


    /*  FirstOrDefault (no predicate)  ================================================================ */

    [Benchmark]
    public int FirstOrDefault_BurstLinq()
    {
        return _immArray.FirstOrDefault();
    }

    [Benchmark]
    public int FirstOrDefault_SystemLinq()
    {
        return System.Linq.Enumerable.FirstOrDefault(_immArray);
    }


    /*  FirstOrDefault (with predicate)  ================================================================ */

    [Benchmark]
    public int FirstOrDefaultPredicate_BurstLinq()
    {
        return _immArray.FirstOrDefault(static x => x > 50);
    }

    [Benchmark]
    public int FirstOrDefaultPredicate_SystemLinq()
    {
        return System.Linq.Enumerable.FirstOrDefault(_immArray, static x => x > 50);
    }


    /*  Where (IReadOnlyList)  ================================================================ */

    [Benchmark]
    public int WhereCount_BurstLinq()
    {
        int count = 0;
        foreach (var _ in _roList.Where(static x => x % 2 == 0))
            count++;
        return count;
    }

    [Benchmark]
    public int WhereCount_SystemLinq()
    {
        return System.Linq.Enumerable.Count(
            System.Linq.Enumerable.Where(_roList, static x => x % 2 == 0));
    }


    /*  Where_Any (ImmutableArray)  ================================================================ */

    [Benchmark]
    public bool WhereAny_BurstLinq()
    {
        return _immArray.Where_Any(static x => x > 50);
    }

    [Benchmark]
    public bool WhereAny_SystemLinq()
    {
        return System.Linq.Enumerable.Any(
            System.Linq.Enumerable.Where(_immArray, static x => x > 50));
    }


    /*  Contains  ================================================================ */

    [Benchmark]
    public bool Contains_BurstLinq()
    {
        return _enumerable.Contains(Size - 1);
    }

    [Benchmark]
    public bool Contains_SystemLinq()
    {
        return System.Linq.Enumerable.Contains(_enumerable, Size - 1);
    }


    /*  Select + ToArray (ImmutableArray)  ================================================================ */

    [Benchmark]
    public int[] SelectToArray_BurstLinq()
    {
        return _immArray.Select(static x => x * 2);
    }

    [Benchmark]
    public int[] SelectToArray_SystemLinq()
    {
        return System.Linq.Enumerable.ToArray(
            System.Linq.Enumerable.Select(_immArray, static x => x * 2));
    }


    /*  ElementAtOrDefault  ================================================================ */

    [Benchmark]
    public int ElementAtOrDefault_BurstLinq()
    {
        return _enumerable.ElementAtOrDefault(Size / 2);
    }

    [Benchmark]
    public int ElementAtOrDefault_SystemLinq()
    {
        return System.Linq.Enumerable.ElementAtOrDefault(_enumerable, Size / 2);
    }


    /*  ToArray  ================================================================ */

    [Benchmark]
    public int[] ToArray_BurstLinq()
    {
        return _enumerable.ToArray();
    }

    [Benchmark]
    public int[] ToArray_SystemLinq()
    {
        return System.Linq.Enumerable.ToArray(_enumerable);
    }
}
