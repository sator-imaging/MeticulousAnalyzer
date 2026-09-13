// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

#:property LangVersion=latest
#:property TargetFramework=net10.0
#:property PublishAot=false

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SatorImaging.MeticulousAnalyzer.Eng
{
    public static class RegexGen
    {
        private const string Pattern = @"Length|Count|Index|Remove|Search|Add|Exchange|Decrement|Increment";

        private const string Declaration = @"namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{
    public static partial class RegexHelper
    {
        public static partial global::System.Text.RegularExpressions.Regex IsExcemptionNameForZeroComparison();
    }
}";

        public static int Main(string[] args)
        {
            string outputPath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                ? args[0]
                : "src/analysis/Analyzers/LiteralBranchAnalyzer.GeneratedRegex.cs";

            string tempDir = Path.Combine(Path.GetTempPath(), "MeticulousAnalyzer_RegexGen_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string csprojPath = Path.Combine(tempDir, "GenApp.csproj");
                string csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)GeneratedFiles</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
</Project>";
                File.WriteAllText(csprojPath, csprojContent);

                string programPath = Path.Combine(tempDir, "Program.cs");
                string programContent = $@"
using System;
using System.Text.RegularExpressions;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers
{{
    public static partial class RegexHelper
    {{
        [GeneratedRegex(@""{Pattern}"", RegexOptions.IgnoreCase)]
        public static partial Regex IsExcemptionNameForZeroComparison();
    }}
}}

class Program
{{
    static void Main() => Console.WriteLine(RegexHelper.IsExcemptionNameForZeroComparison().IsMatch(""Length""));
}}";
                File.WriteAllText(programPath, programContent);

                var psi = new ProcessStartInfo("dotnet", $"build \"{csprojPath}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit();
                }

                string generatedFilesDir = Path.Combine(tempDir, "obj", "GeneratedFiles");
                string[] generatedFiles = Directory.Exists(generatedFilesDir)
                    ? Directory.GetFiles(generatedFilesDir, "*.cs", SearchOption.AllDirectories)
                    : Directory.GetFiles(tempDir, "*.cs", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith("Program.cs") && !f.EndsWith("GlobalUsings.g.cs") && !f.EndsWith("AssemblyInfo.cs") && !f.EndsWith("AssemblyAttributes.cs"))
                        .ToArray();

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

                // Remove protected from Scan(ReadOnlySpan<char> inputSpan)
                generatedCode = generatedCode.Replace("protected override void Scan(ReadOnlySpan<char> inputSpan)", "void Scan(ReadOnlySpan<char> inputSpan)");

                // Add missing abstract member implementations for RegexRunner
                string runnerOverrides = @"
                protected override void Go() => throw new NotImplementedException();
                protected override bool FindFirstChar() => throw new NotImplementedException();
                protected override void InitTrackCount() { }
";
                generatedCode = generatedCode.Replace("private sealed class Runner : RegexRunner\n            {", "private sealed class Runner : RegexRunner\n            {" + runnerOverrides);

                // Fix StartsWith for ReadOnlySpan<char> in netstandard2.0
                generatedCode = Regex.Replace(generatedCode, @"!slice\.StartsWith\(("".*?""), StringComparison\.OrdinalIgnoreCase\)", "!slice.StartsWith($1.AsSpan(), StringComparison.OrdinalIgnoreCase)");

                // Insert RegexHelper.IsExcemptionNameForZeroComparison() declaration to generated code to solve the compile error.
                generatedCode = Declaration + "\n\n" + generatedCode;

                var dirPath = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                File.WriteAllText(outputPath, generatedCode);
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
        }
    }
}
