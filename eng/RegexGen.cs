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
                string programContent = @"
using System;
using System.Text.RegularExpressions;

namespace SatorImaging.MeticulousAnalyzer.Analysis.Analyzers;

public static partial class RegexHelper
{
    [GeneratedRegex(@""Length|Count|Index|Remove|Search|Add|Exchange|Decrement|Increment"", RegexOptions.IgnoreCase)]
    public static partial Regex IsExcemptionNameForZeroComparison();
}

class Program
{
    static void Main() => Console.WriteLine(RegexHelper.IsExcemptionNameForZeroComparison().IsMatch(""Length""));
}";
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

                string? generatedFile = Directory.GetFiles(tempDir, "*RegexGenerator.g.cs", SearchOption.AllDirectories).FirstOrDefault();
                // Early exit
                if (generatedFile == null)
                {
                    Console.Error.WriteLine("Failed to locate generated RegexGenerator.g.cs");
                    return 1;
                }

                string generatedCode = File.ReadAllText(generatedFile);

                // Strip "file " modifier as required
                generatedCode = Regex.Replace(generatedCode, @"\bfile\s+", "internal ");

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
