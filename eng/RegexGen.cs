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

namespace SatorImaging.MeticulousAnalyzer.Eng
{
    public static class RegexGen
    {
        private static readonly (string Name, string Pattern)[] RegexPatterns = new[]
        {
            ("IsExcemptionNameForZeroComparison", @"Length|Count|Index|Remove|Search|Add|Exchange|Decrement|Increment"),
        };

        public static int Main(string[] args)
        {
            string outputPath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                ? args[0]
                : "src/analysis/RegexGen.g.cs";

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

                var programSb = new StringBuilder();
                programSb.AppendLine("using System;");
                programSb.AppendLine("using System.Text.RegularExpressions;");
                programSb.AppendLine();
                programSb.AppendLine("namespace SatorImaging.MeticulousAnalyzer.Analysis");
                programSb.AppendLine("{");
                programSb.AppendLine("    public static partial class RegexGen");
                programSb.AppendLine("    {");
                foreach (var (name, pattern) in RegexPatterns)
                {
                    programSb.AppendLine($"        [GeneratedRegex(@\"{pattern}\", RegexOptions.IgnoreCase)]");
                    programSb.AppendLine($"        public static partial Regex {name}();");
                    programSb.AppendLine();
                }
                programSb.AppendLine("    }");
                programSb.AppendLine("}");
                programSb.AppendLine();
                programSb.AppendLine("class Program");
                programSb.AppendLine("{");
                programSb.AppendLine("    static void Main()");
                programSb.AppendLine("    {");
                foreach (var (name, _) in RegexPatterns)
                {
                    programSb.AppendLine($"        Console.WriteLine(RegexGen.{name}().IsMatch(\"Length\"));");
                }
                programSb.AppendLine("    }");
                programSb.AppendLine("}");

                File.WriteAllText(Path.Combine(tempDir, "Program.cs"), programSb.ToString());

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

                var declSb = new StringBuilder();
                declSb.AppendLine("namespace SatorImaging.MeticulousAnalyzer.Analysis");
                declSb.AppendLine("{");
                declSb.AppendLine("    public static partial class RegexGen");
                declSb.AppendLine("    {");
                foreach (var (name, _) in RegexPatterns)
                {
                    declSb.AppendLine($"        public static partial global::System.Text.RegularExpressions.Regex {name}();");
                }
                declSb.AppendLine("    }");
                declSb.AppendLine("}");

                string fileListComment = "// <auto-generated>\n//     Source generated files collected:\n"
                    + string.Join("\n", generatedFiles.Select(f => $"//     - {Path.GetFileName(f)}"))
                    + "\n// </auto-generated>\n\n";

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

                // Insert header comment and partial method declarations
                generatedCode = fileListComment + declSb.ToString() + "\n\n" + generatedCode;

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
