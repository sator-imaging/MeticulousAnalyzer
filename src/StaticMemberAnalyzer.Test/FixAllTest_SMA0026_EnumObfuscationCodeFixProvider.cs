/*
 * FixAllTest_SMA0026_EnumObfuscationCodeFixProvider.cs
 *
 * Copyright (c) 2024 Sator Imaging
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SatorImaging.StaticMemberAnalyzer.Test;
using StaticMemberAnalyzer.Test;
using System.Threading.Tasks;
using VerifyCS = StaticMemberAnalyzer.Test.CSharpCodeFixVerifier<
    SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers.EnumAnalyzer,
    SatorImaging.StaticMemberAnalyzer.CodeFixes.Providers.EnumObfuscationCodeFixProvider>;

namespace SatorImaging.StaticMemberAnalyzer.Test
{
    [TestClass]
    public class FixAllTest_SMA0026_EnumObfuscationCodeFixProvider
    {
        [TestMethod]
        public async Task FixAll_SMA0026()
        {
            var test = new VerifyCS.Test
            {
                CodeActionEquivalenceKey = "Exclude Enum Member from Obfuscation",
                FixAllProvider = CodeFixHelpers.BatchFixAllProvider,
            };

            test.CodeActionValidationMode = CodeActionValidationMode.None;

            for (int p = 0; p < 3; p++)
            {
                var projectName = "Project" + p.ToString();
                var project = p == 0 ? test.TestState : new ProjectState(name: projectName, language: LanguageNames.CSharp, defaultPrefix: projectName, defaultExtension: "cs");
                if (p > 0) test.TestState.AdditionalProjects.Add(projectName, project);

                var batchFixedProject = p == 0 ? test.BatchFixedState : new ProjectState(name: projectName, language: LanguageNames.CSharp, defaultPrefix: projectName, defaultExtension: "cs");
                if (p > 0) test.BatchFixedState.AdditionalProjects.Add(projectName, batchFixedProject);

                for (int f = 0; f < 3; f++)
                {
                    var fileName = (f == 0 && p == 0) ? "Test0.cs" : ("File" + p.ToString() + "_" + f.ToString() + ".cs");
                    project.Sources.Add((fileName, GetSource(p: p, f: f)));
                    batchFixedProject.Sources.Add((fileName, GetFixedSource(p: p, f: f)));

                    for (int i = 0; i < 3; i++)
                    {
                        var diag = VerifyCS.Diagnostic().WithLocation(path: fileName, line: 3 + i, column: 17).WithArguments(arguments: new[] { "Enum0", "Enum1", "Enum2" }[i]);
                        test.ExpectedDiagnostics.Add(diag);
                    }
                }
            }
            test.TestState.Sources.Add(("Empty.cs", ""));
            test.BatchFixedState.Sources.Add(("Empty.cs", ""));

            test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            test.FixedState.Sources.Add(("Test0.cs", GetFixedSource(p: 0, f: 0)));

            // FixAllProvider test cannot be done with current Roslyn version (3.8.0).
            //   e.g., `FixAllProvider = CodeFixHelpers.BatchFixAllProvider`
            // It's available in Roslyn version (4.4.0 or later).
            await test.VerifyAsync().ConfigureAwait(continueOnCapturedContext: false);
        }

        private static string GetSource(int p, int f)
        {
            return string.Format(@"namespace Project{0}.Namespace{1}
{{
    public enum Enum0 {{ A }}
    public enum Enum1 {{ B }}
    public enum Enum2 {{ C }}
}}", (object)p.ToString(), (object)f.ToString());
        }

        private static string GetFixedSource(int p, int f)
        {
            return string.Format(@"using System.Reflection;

namespace Project{0}.Namespace{1}
{{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum Enum0 {{ A }}
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum Enum1 {{ B }}
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public enum Enum2 {{ C }}
}}", (object)p.ToString(), (object)f.ToString());
        }
    }
}
