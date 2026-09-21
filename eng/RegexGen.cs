// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#:property LangVersion=latest
#:property TargetFramework=net10.0
#:property PublishAot=false

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

const string OutputClassName = "GeneratedRegexPolyfill";

(string TargetNamespace, string Name, string Pattern, string Options)[] RegexPatterns = new[]
{
    ("SatorImaging.MeticulousAnalyzer.Analysis", "IsExcemptionNameForZeroComparison", @"Length|Count|Index|Remove|Search|Add|Exchange|Decrement|Increment", "RegexOptions.IgnoreCase"),
};

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    throw new ArgumentException("File path must be supplied.", nameof(args));
}

string outputPath = args[0];

string tempDirName = $"{OutputClassName}_{Guid.NewGuid():N}";
string tempDir = Path.Combine(Path.GetTempPath(), tempDirName);
Directory.CreateDirectory(tempDir);

try
{
    string csprojPath = Path.Combine(tempDir, $"{tempDirName}.csproj");
    string generatedCodeOutputPath = Path.Combine(tempDir, "obj", "GeneratedFiles");

    string csprojContent = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>{generatedCodeOutputPath}</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
</Project>";
    File.WriteAllText(csprojPath, csprojContent);

    var programSb = new StringBuilder();
    programSb.AppendLine("using System;");
    programSb.AppendLine("using System.Text.RegularExpressions;");
    programSb.AppendLine();
    foreach (var group in RegexPatterns.GroupBy(p => p.TargetNamespace))
    {
        programSb.AppendLine($"namespace {group.Key}");
        programSb.AppendLine("{");
        programSb.AppendLine($"    public static partial class {OutputClassName}");
        programSb.AppendLine("    {");
        foreach (var (_, name, pattern, options) in group)
        {
            programSb.AppendLine($"        [GeneratedRegex(@\"{pattern}\", {options})]");
            programSb.AppendLine($"        public static partial Regex {name}();");
            programSb.AppendLine();
        }
        programSb.AppendLine("    }");
        programSb.AppendLine("}");
        programSb.AppendLine();
    }
    programSb.AppendLine("class Program");
    programSb.AppendLine("{");
    programSb.AppendLine("    static void Main()");
    programSb.AppendLine("    {");
    foreach (var (targetNamespace, name, _, _) in RegexPatterns)
    {
        programSb.AppendLine($"        Console.WriteLine({targetNamespace}.{OutputClassName}.{name}().IsMatch(\"THIS IS A TEST\"));");
    }
    programSb.AppendLine("    }");
    programSb.AppendLine("}");

    File.WriteAllText(Path.Combine(tempDir, "Program.cs"), programSb.ToString());

    var psi = new ProcessStartInfo("dotnet", $"build -c Release \"{csprojPath}\"")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    using var proc = Process.Start(psi);
    proc?.WaitForExit();

    string[] generatedFiles = Directory.Exists(generatedCodeOutputPath)
        ? Directory.GetFiles(generatedCodeOutputPath, "*.cs", SearchOption.AllDirectories)
        : Array.Empty<string>();

    // Early exit
    if (generatedFiles.Length == 0)
    {
        Console.Error.WriteLine("Failed to locate generated files under obj/GeneratedFiles");
        return 1;
    }

    string generatedCode = string.Join("\n\n", generatedFiles.Select(f => File.ReadAllText(f)))
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

    // Strip "file " modifier as required
    generatedCode = Regex.Replace(generatedCode, @"\bfile\s+", "internal ");

    // Remove protected override from Scan(ReadOnlySpan<char> inputSpan)
    generatedCode = Regex.Replace(generatedCode, @"protected\s+override\s+void\s+Scan\(ReadOnlySpan<char>", "void Scan(ReadOnlySpan<char>");

    // Add missing abstract member implementations for RegexRunner
    string runnerOverrides = @"
    protected override void Go()
    {
        int start = runtextpos;

        if (!TryMatchAtCurrentPosition(runtext.AsSpan(0, runtextend)))
        {
            runtextpos = start;
        }
    }

    protected override bool FindFirstChar()
    {
        return TryFindNextPossibleStartingPosition(
            runtext.AsSpan(0, runtextend));
    }

    protected override void InitTrackCount()
    {
        runtrackcount = 0;
    }
";
    generatedCode = generatedCode.Replace("private sealed class Runner : RegexRunner\n            {", "private sealed class Runner : RegexRunner\n            {" + runnerOverrides);

    // Fix StartsWith for ReadOnlySpan<char> in netstandard2.0 / .NET 5.0
    generatedCode = Regex.Replace(generatedCode, @"!slice\.StartsWith\(("".*?""), StringComparison\.OrdinalIgnoreCase\)", "!slice.StartsWith($1.AsSpan(), StringComparison.OrdinalIgnoreCase)");

    var declSb = new StringBuilder();
    declSb.AppendLine("// <auto-generated>");
    declSb.AppendLine("//     Source generated files collected:");
    foreach (var file in generatedFiles)
    {
        declSb.AppendLine($"//     - {Path.GetFileName(file)}");
    }
    declSb.AppendLine("//     Patterns:");
    foreach (var (_, name, pattern, options) in RegexPatterns)
    {
        declSb.AppendLine($"//     - {name}: @\"{pattern}\", {options}");
    }
    declSb.AppendLine("// </auto-generated>");
    declSb.AppendLine();
    foreach (var group in RegexPatterns.GroupBy(p => p.TargetNamespace))
    {
        declSb.AppendLine($"namespace {group.Key}");
        declSb.AppendLine("{");
        declSb.AppendLine($"    public static partial class {OutputClassName}");
        declSb.AppendLine("    {");
        foreach (var (_, name, _, _) in group)
        {
            declSb.AppendLine($"        public static partial global::System.Text.RegularExpressions.Regex {name}();");
        }
        declSb.AppendLine("    }");
        declSb.AppendLine("}");
        declSb.AppendLine();
    }
    declSb.AppendLine(generatedCode);

    string finalCode = declSb.ToString();

    var dirPath = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath))
    {
        Directory.CreateDirectory(dirPath);
    }

    File.WriteAllText(outputPath, finalCode);
    Console.WriteLine($"Extracted and stripped source generator output at: {outputPath}");
    return 0;
}
finally
{
    try
    {
        Directory.Delete(tempDir, recursive: true);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine(ex.Message);
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.Error.WriteLine(ex.Message);
    }
}
