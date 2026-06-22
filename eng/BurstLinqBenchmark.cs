// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#:package BenchmarkDotNet@0.15.8
#:property LangVersion=latest
#:property PublishAot=false
#:property ImplicitUsings=false
#:property TargetFrameworks=net10.0;net5.0;

#:package FUnit.Directives@*
#warning funit include ../src/analysis/BurstLinq.cs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using SatorImaging.StaticMemberAnalyzer;


var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(Job.ShortRun
        .WithWarmupCount(1)
        .WithIterationCount(3)
        .WithToolchain(InProcessNoEmitToolchain.Instance))
    .WithOption(ConfigOptions.JoinSummary, true);

BenchmarkRunner.Run(typeof(BurstLinqBenchmarks).Assembly, config, args);


[MemoryDiagnoser]
[HideColumns(Column.Gen0, Column.Gen1, Column.Median, Column.Error, Column.RatioSD)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BurstLinqBenchmarks
{
    [Params(0, 10, 100)]
    public int Size;

    ImmutableArray<double> _immArray;
    IReadOnlyList<double> _roList = null!;
    IEnumerable<double> _enumerable = null!;
    IEnumerable<object> _objEnumerable = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = ImmutableArray.CreateBuilder<double>(Size);
        for (int i = 0; i < Size; i++)
            builder.Add((double)i);
        _immArray = builder.ToImmutable();
        _roList = (IReadOnlyList<double>)_immArray;
        _enumerable = (IEnumerable<double>)_immArray;

        var objBuilder = ImmutableArray.CreateBuilder<object>(Size);
        for (int i = 0; i < Size; i++)
            objBuilder.Add((double)i);
        _objEnumerable = (IEnumerable<object>)objBuilder.ToImmutable();
    }


    /*  Any (no predicate)  ================================================================ */

    [BenchmarkCategory("Any")]
    [Benchmark]
    public bool Any_BurstLinq()
    {
        return _immArray.Any();
    }

    [BenchmarkCategory("Any")]
    [Benchmark(Baseline = true)]
    public bool Any_SystemLinq()
    {
        return System.Linq.Enumerable.Any(_immArray);
    }


    /*  Any (with predicate)  ================================================================ */

    [BenchmarkCategory("AnyPredicate")]
    [Benchmark]
    public bool AnyPredicate_BurstLinq()
    {
        return _immArray.Any(static x => x > 50.0);
    }

    [BenchmarkCategory("AnyPredicate")]
    [Benchmark(Baseline = true)]
    public bool AnyPredicate_SystemLinq()
    {
        return System.Linq.Enumerable.Any(_immArray, static x => x > 50.0);
    }


    /*  FirstOrDefault (no predicate)  ================================================================ */

    [BenchmarkCategory("FirstOrDefault")]
    [Benchmark]
    public double FirstOrDefault_BurstLinq()
    {
        return _immArray.FirstOrDefault();
    }

    [BenchmarkCategory("FirstOrDefault")]
    [Benchmark(Baseline = true)]
    public double FirstOrDefault_SystemLinq()
    {
        return System.Linq.Enumerable.FirstOrDefault(_immArray);
    }


    /*  FirstOrDefault (with predicate)  ================================================================ */

    [BenchmarkCategory("FirstOrDefaultPredicate")]
    [Benchmark]
    public double FirstOrDefaultPredicate_BurstLinq()
    {
        return _immArray.FirstOrDefault(static x => x > 50.0);
    }

    [BenchmarkCategory("FirstOrDefaultPredicate")]
    [Benchmark(Baseline = true)]
    public double FirstOrDefaultPredicate_SystemLinq()
    {
        return System.Linq.Enumerable.FirstOrDefault(_immArray, static x => x > 50.0);
    }


    /*  Where (IReadOnlyList)  ================================================================ */

    [BenchmarkCategory("WhereCount")]
    [Benchmark]
    public int WhereCount_BurstLinq()
    {
        int count = 0;
        foreach (var _ in _roList.Where(static x => x % 2.0 == 0.0))
            count++;
        return count;
    }

    [BenchmarkCategory("WhereCount")]
    [Benchmark(Baseline = true)]
    public int WhereCount_SystemLinq()
    {
        return System.Linq.Enumerable.Count(
            System.Linq.Enumerable.Where(_roList, static x => x % 2.0 == 0.0));
    }


    /*  Where_Any (ImmutableArray)  ================================================================ */

    [BenchmarkCategory("WhereAny")]
    [Benchmark]
    public bool WhereAny_BurstLinq()
    {
        return _immArray.Where_Any(static x => x > 50.0);
    }

    [BenchmarkCategory("WhereAny")]
    [Benchmark(Baseline = true)]
    public bool WhereAny_SystemLinq()
    {
        return System.Linq.Enumerable.Any(
            System.Linq.Enumerable.Where(_immArray, static x => x > 50.0));
    }


    /*  Where.ToImmutableArray (ImmutableArray)  ================================================================ */

    [BenchmarkCategory("WhereToImmutableArray")]
    [Benchmark]
    public ImmutableArray<double> WhereToImmutableArray_BurstLinq()
    {
        return _immArray.Where(static x => x > 50.0).ToImmutableArray();
    }

    [BenchmarkCategory("WhereToImmutableArray")]
    [Benchmark(Baseline = true)]
    public ImmutableArray<double> WhereToImmutableArray_SystemLinq()
    {
        return System.Collections.Immutable.ImmutableArray.ToImmutableArray(
            System.Linq.Enumerable.Where(_immArray, static x => x > 50.0));
    }


    /*  Contains  ================================================================ */

    [BenchmarkCategory("Contains")]
    [Benchmark]
    public bool Contains_BurstLinq()
    {
        return _enumerable.Contains((double)(Size - 1));
    }

    [BenchmarkCategory("Contains")]
    [Benchmark(Baseline = true)]
    public bool Contains_SystemLinq()
    {
        return System.Linq.Enumerable.Contains(_enumerable, (double)(Size - 1));
    }


    /*  Contains (string array)  ================================================================ */

    string[] _stringArray = null!;

    [BenchmarkCategory("ContainsStringArray")]
    [Benchmark]
    public bool ContainsStringArray_BurstLinq()
    {
        return _stringArray.Contains("Target");
    }

    [BenchmarkCategory("ContainsStringArray")]
    [Benchmark(Baseline = true)]
    public bool ContainsStringArray_SystemLinq()
    {
        return System.Linq.Enumerable.Contains(_stringArray, "Target");
    }


    /*  Select + ToArray (ImmutableArray)  ================================================================ */

    [BenchmarkCategory("SelectToArray")]
    [Benchmark]
    public double[] SelectToArray_BurstLinq()
    {
        return _immArray.Select(static x => x * 2.0);
    }

    [BenchmarkCategory("SelectToArray")]
    [Benchmark(Baseline = true)]
    public double[] SelectToArray_SystemLinq()
    {
        return System.Linq.Enumerable.ToArray(
            System.Linq.Enumerable.Select(_immArray, static x => x * 2.0));
    }


    /*  ElementAtOrDefault  ================================================================ */

    [BenchmarkCategory("ElementAtOrDefault")]
    [Benchmark]
    public double ElementAtOrDefault_BurstLinq()
    {
        return _enumerable.ElementAtOrDefault(Size / 2);
    }

    [BenchmarkCategory("ElementAtOrDefault")]
    [Benchmark(Baseline = true)]
    public double ElementAtOrDefault_SystemLinq()
    {
        return System.Linq.Enumerable.ElementAtOrDefault(_enumerable, Size / 2);
    }


    /*  ToArray  ================================================================ */

    [BenchmarkCategory("ToArray")]
    [Benchmark]
    public double[] ToArray_BurstLinq()
    {
        return _enumerable.ToArray();
    }

    [BenchmarkCategory("ToArray")]
    [Benchmark(Baseline = true)]
    public double[] ToArray_SystemLinq()
    {
        return System.Linq.Enumerable.ToArray(_enumerable);
    }


    /*  OfType (struct iterator)  ================================================================ */

    [BenchmarkCategory("OfType")]
    [Benchmark]
    public int OfType_BurstLinq()
    {
        int count = 0;
        foreach (var _ in _objEnumerable.OfType<double>())
            count++;
        return count;
    }

    [BenchmarkCategory("OfType")]
    [Benchmark(Baseline = true)]
    public int OfType_SystemLinq()
    {
        return System.Linq.Enumerable.Count(System.Linq.Enumerable.OfType<double>(_objEnumerable));
    }


    /*  OfType_FirstOrDefault (fused)  ================================================================ */

    [BenchmarkCategory("OfType_FirstOrDefault")]
    [Benchmark]
    public double OfType_FirstOrDefault_BurstLinq()
    {
        return _objEnumerable.OfType_FirstOrDefault<double>();
    }

    [BenchmarkCategory("OfType_FirstOrDefault")]
    [Benchmark(Baseline = true)]
    public double OfType_FirstOrDefault_SystemLinq()
    {
        return System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<double>(_objEnumerable));
    }


    /*  OfType_Any (fused)  ================================================================ */

    [BenchmarkCategory("OfType_Any")]
    [Benchmark]
    public bool OfType_Any_BurstLinq()
    {
        return _objEnumerable.OfType_Any<double>();
    }

    [BenchmarkCategory("OfType_Any")]
    [Benchmark(Baseline = true)]
    public bool OfType_Any_SystemLinq()
    {
        return System.Linq.Enumerable.Any(System.Linq.Enumerable.OfType<double>(_objEnumerable));
    }


    /*  Where + FirstOrDefault (struct iterator)  ================================================================ */

    [BenchmarkCategory("WhereFirstOrDefault")]
    [Benchmark]
    public double WhereFirstOrDefault_BurstLinq()
    {
        foreach (var item in _roList.Where(static x => x > 50.0))
            return item;
        return default;
    }

    [BenchmarkCategory("WhereFirstOrDefault")]
    [Benchmark(Baseline = true)]
    public double WhereFirstOrDefault_SystemLinq()
    {
        return System.Linq.Enumerable.FirstOrDefault(
            System.Linq.Enumerable.Where(_roList, static x => x > 50.0));
    }
}
