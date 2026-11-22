// Licensed under the MIT License
// https://github.com/sator-imaging/MeticulousAnalyzer

using System;
using System.Collections.Generic;
using System.Text;

namespace SatorImaging.MeticulousAnalyzer.CodeFixes.Providers
{
    internal sealed class LinqFusionCodeGenerator
    {
        private sealed class Step
        {
            internal Step(int nestLevel, string text)
            {
                NestLevel = nestLevel;
                Text = text;
            }

            internal int NestLevel { get; }
            internal string Text { get; }
        }

        private sealed class Steps
        {
            // Generation steps carry explicit nesting so loop structure remains visible instead of embedded newline-heavy strings.
            internal int LoopNestLevel { get; set; } = 1;
            internal string LoopVariableName { get; set; } = "item";
            internal bool IncrementIndex { get; set; }
            internal readonly List<Step> BeforeLoop = new List<Step>();
            internal readonly List<Step> InsideLoop = new List<Step>();
            internal readonly List<Step> AfterLoop = new List<Step>();
        }

        internal static string GenerateMethod(
            string innerName,
            string outerName,
            bool hasIndexedSelect,
            bool hasTerminalPredicate,
            string fusionName)
        {
            var signature = GenerateSignature(
                innerName,
                outerName,
                hasIndexedSelect,
                hasTerminalPredicate,
                fusionName);
            var steps = GenerateSteps(
                innerName,
                outerName,
                hasIndexedSelect,
                hasTerminalPredicate);

            var source = new StringBuilder();
            source.Append(signature);
            source.AppendLine();
            source.AppendLine("{");
            AppendSteps(source, 1, steps.BeforeLoop);

            source.Append("    foreach (var ");
            source.Append(steps.LoopVariableName);
            source.AppendLine(" in source)");
            source.AppendLine("    {");
            AppendSteps(source, 2, steps.InsideLoop);
            if (steps.IncrementIndex)
            {
                for (int level = 0; level < 2; level++)
                {
                    source.Append("    ");
                }
                source.AppendLine("index++;");
            }
            source.AppendLine("    }");

            AppendSteps(source, 1, steps.AfterLoop);
            source.AppendLine("}");
            return source.ToString();
        }

        private static string GenerateSignature(
            string innerName,
            string outerName,
            bool hasIndexedSelect,
            bool hasTerminalPredicate,
            string fusionName)
        {
            var resultType = "TSource";
            var parameters = new StringBuilder();

            if (innerName == "Where")
            {
                parameters.Append("this global::System.Collections.Generic.IEnumerable<TSource> source");
                parameters.Append(", global::System.Func<TSource, bool> predicate");
            }
            else if (innerName == "OfType")
            {
                resultType = "TResult";
                parameters.Append("this global::System.Collections.IEnumerable source");
            }
            else
            {
                resultType = "TResult";
                parameters.Append("this global::System.Collections.Generic.IEnumerable<TSource> source");
                if (hasIndexedSelect)
                {
                    parameters.Append(", global::System.Func<TSource, int, TResult> selector");
                }
                else
                {
                    parameters.Append(", global::System.Func<TSource, TResult> selector");
                }
            }

            if (hasTerminalPredicate)
            {
                if (innerName == "Where")
                {
                    parameters.Append(", global::System.Func<TSource, bool> terminalPredicate");
                }
                else
                {
                    parameters.Append(", global::System.Func<");
                    parameters.Append(resultType);
                    parameters.Append(", bool> predicate");
                }
            }

            var typeParameters = innerName == "Select"
                ? "<TSource, TResult>"
                : innerName == "OfType"
                    ? "<TResult>"
                    : "<TSource>";

            var returnType = outerName == "Any" || outerName == "All"
                ? "bool"
                : resultType;

            return "public static "
                + returnType
                + " "
                + fusionName
                + typeParameters
                + "("
                + parameters
                + ")";
        }

        private static Steps GenerateSteps(
            string innerName,
            string outerName,
            bool hasIndexedSelect,
            bool hasTerminalPredicate)
        {
            var steps = new Steps();
            if (innerName == "Select" && hasIndexedSelect)
            {
                steps.BeforeLoop.Add(new Step(1, "int index = 0;"));
                steps.IncrementIndex = true;
            }

            if (outerName == "Last" || outerName == "LastOrDefault"
                || outerName == "Single" || outerName == "SingleOrDefault")
            {
                var resultType = innerName == "Select" || innerName == "OfType"
                    ? "TResult"
                    : "TSource";
                steps.BeforeLoop.Add(new Step(1, "bool found = false;"));
                steps.BeforeLoop.Add(new Step(1, resultType + " result = default(" + resultType + ");"));
            }

            if (outerName == "Any")
            {
                AddAnySteps(steps, innerName, hasTerminalPredicate, hasIndexedSelect);
            }
            else if (outerName == "All")
            {
                AddAllSteps(steps, innerName, hasTerminalPredicate, hasIndexedSelect);
            }
            else if (outerName == "First" || outerName == "FirstOrDefault")
            {
                AddFirstSteps(steps, innerName, outerName, hasTerminalPredicate, hasIndexedSelect);
            }
            else if (outerName == "Last" || outerName == "LastOrDefault")
            {
                AddLastSteps(steps, innerName, outerName, hasTerminalPredicate, hasIndexedSelect);
            }
            else
            {
                AddSingleSteps(steps, innerName, outerName, hasTerminalPredicate, hasIndexedSelect);
            }

            return steps;
        }

        private static void AddAnySteps(Steps steps, string innerName, bool hasTerminalPredicate, bool hasIndexedSelect)
        {
            if (innerName == "Where")
            {
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (predicate(item) && terminalPredicate(item))"));
                }
                else
                {
                    steps.InsideLoop.Add(new Step(1, "if (predicate(item))"));
                }
            }
            else if (innerName == "OfType")
            {
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (item is TResult match && predicate(match))"));
                }
                else
                {
                    steps.InsideLoop.Add(new Step(1, "if (item is TResult)"));
                }
            }
            else
            {
                steps.InsideLoop.Add(new Step(1, "TResult selected = "
                    + (hasIndexedSelect ? "selector(item, index);" : "selector(item);")));
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (predicate(selected))"));
                }
                else
                {
                    steps.InsideLoop.Add(new Step(1, "return true;"));
                }
            }

            if (innerName != "Select" || hasTerminalPredicate)
            {
                steps.InsideLoop.Add(new Step(1, "{"));
                steps.InsideLoop.Add(new Step(2, "return true;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
            steps.AfterLoop.Add(new Step(1, "return false;"));
        }

        private static void AddAllSteps(Steps steps, string innerName, bool hasTerminalPredicate, bool hasIndexedSelect)
        {
            if (innerName == "Where")
            {
                steps.InsideLoop.Add(new Step(1, "if (!predicate(item))"));
                steps.InsideLoop.Add(new Step(1, "{"));
                steps.InsideLoop.Add(new Step(2, "continue;"));
                steps.InsideLoop.Add(new Step(1, "}"));
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (!terminalPredicate(item))"));
                }
            }
            else if (innerName == "OfType")
            {
                steps.InsideLoop.Add(new Step(1, "if (item is TResult match)"));
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "{"));
                    steps.InsideLoop.Add(new Step(2, "if (!predicate(match))"));
                    steps.InsideLoop.Add(new Step(2, "{"));
                    steps.InsideLoop.Add(new Step(3, "return false;"));
                    steps.InsideLoop.Add(new Step(2, "}"));
                    steps.InsideLoop.Add(new Step(1, "}"));
                    steps.AfterLoop.Add(new Step(1, "return true;"));
                    return;
                }
            }
            else
            {
                steps.InsideLoop.Add(new Step(1, "TResult selected = "
                    + (hasIndexedSelect ? "selector(item, index);" : "selector(item);")));
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (!predicate(selected))"));
                }
                else
                {
                    steps.AfterLoop.Add(new Step(1, "return true;"));
                    return;
                }
            }

            if (hasTerminalPredicate)
            {
                steps.InsideLoop.Add(new Step(1, "{"));
                steps.InsideLoop.Add(new Step(2, "return false;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
            steps.AfterLoop.Add(new Step(1, "return true;"));
        }

        private static void AddFirstSteps(Steps steps, string innerName, string outerName, bool hasTerminalPredicate, bool hasIndexedSelect)
        {
            if (innerName == "Where")
            {
                steps.InsideLoop.Add(new Step(1, hasTerminalPredicate
                    ? "if (predicate(item) && terminalPredicate(item))"
                    : "if (predicate(item))"));
                steps.InsideLoop.Add(new Step(1, "{"));
                steps.InsideLoop.Add(new Step(2, "return item;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
            else if (innerName == "OfType")
            {
                steps.InsideLoop.Add(new Step(1, hasTerminalPredicate
                    ? "if (item is TResult match && predicate(match))"
                    : "if (item is TResult match)"));
                steps.InsideLoop.Add(new Step(1, "{"));
                steps.InsideLoop.Add(new Step(2, "return match;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
            else
            {
                steps.InsideLoop.Add(new Step(1, "TResult selected = "
                    + (hasIndexedSelect ? "selector(item, index);" : "selector(item);")));
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (predicate(selected))"));
                    steps.InsideLoop.Add(new Step(1, "{"));
                    steps.InsideLoop.Add(new Step(2, "return selected;"));
                    steps.InsideLoop.Add(new Step(1, "}"));
                }
                else
                {
                    steps.InsideLoop.Add(new Step(1, "return selected;"));
                }
            }

            if (outerName == "First")
            {
                steps.AfterLoop.Add(new Step(1, "throw new global::System.InvalidOperationException(\"Sequence contains no elements\");"));
            }
            else
            {
                var resultType = innerName == "Select" || innerName == "OfType"
                    ? "TResult"
                    : "TSource";
                steps.AfterLoop.Add(new Step(1, "return default(" + resultType + ");"));
            }
        }

        private static void AddLastSteps(Steps steps, string innerName, string outerName, bool hasTerminalPredicate, bool hasIndexedSelect)
        {
            AddAccumulatorSteps(steps, innerName, hasTerminalPredicate, hasIndexedSelect, false);
            if (outerName == "Last")
            {
                steps.AfterLoop.Add(new Step(1, "if (found)"));
                steps.AfterLoop.Add(new Step(1, "{"));
                steps.AfterLoop.Add(new Step(2, "return result;"));
                steps.AfterLoop.Add(new Step(1, "}"));
                steps.AfterLoop.Add(new Step(1, "throw new global::System.InvalidOperationException(\"Sequence contains no elements\");"));
            }
            else
            {
                steps.AfterLoop.Add(new Step(1, "return found ? result : default;"));
            }
        }

        private static void AddSingleSteps(Steps steps, string innerName, string outerName, bool hasTerminalPredicate, bool hasIndexedSelect)
        {
            AddAccumulatorSteps(steps, innerName, hasTerminalPredicate, hasIndexedSelect, true);
            if (outerName == "Single")
            {
                steps.AfterLoop.Add(new Step(1, "if (found)"));
                steps.AfterLoop.Add(new Step(1, "{"));
                steps.AfterLoop.Add(new Step(2, "return result;"));
                steps.AfterLoop.Add(new Step(1, "}"));
                steps.AfterLoop.Add(new Step(1, "throw new global::System.InvalidOperationException(\"Sequence contains no elements\");"));
            }
            else
            {
                steps.AfterLoop.Add(new Step(1, "return found ? result : default;"));
            }
        }

        private static void AddAccumulatorSteps(Steps steps, string innerName, bool hasTerminalPredicate, bool hasIndexedSelect, bool single)
        {
            if (innerName == "Where")
            {
                steps.InsideLoop.Add(new Step(1, hasTerminalPredicate
                    ? "if (predicate(item) && terminalPredicate(item))"
                    : "if (predicate(item))"));
                steps.InsideLoop.Add(new Step(1, "{"));
                if (single)
                {
                    steps.InsideLoop.Add(new Step(2, "if (found)"));
                    steps.InsideLoop.Add(new Step(2, "{"));
                    steps.InsideLoop.Add(new Step(3, "throw new global::System.InvalidOperationException(\"Sequence contains more than one element\");"));
                    steps.InsideLoop.Add(new Step(2, "}"));
                }
                steps.InsideLoop.Add(new Step(2, "found = true;"));
                steps.InsideLoop.Add(new Step(2, "result = item;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
            else if (innerName == "OfType")
            {
                steps.InsideLoop.Add(new Step(1, hasTerminalPredicate
                    ? "if (item is TResult match && predicate(match))"
                    : "if (item is TResult match)"));
                steps.InsideLoop.Add(new Step(1, "{"));
                if (single)
                {
                    steps.InsideLoop.Add(new Step(2, "if (found)"));
                    steps.InsideLoop.Add(new Step(2, "{"));
                    steps.InsideLoop.Add(new Step(3, "throw new global::System.InvalidOperationException(\"Sequence contains more than one element\");"));
                    steps.InsideLoop.Add(new Step(2, "}"));
                }
                steps.InsideLoop.Add(new Step(2, "found = true;"));
                steps.InsideLoop.Add(new Step(2, "result = match;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
            else
            {
                steps.InsideLoop.Add(new Step(1, "TResult selected = "
                    + (hasIndexedSelect ? "selector(item, index);" : "selector(item);")));
                if (hasTerminalPredicate)
                {
                    steps.InsideLoop.Add(new Step(1, "if (predicate(selected))"));
                }
                else
                {
                    if (single)
                    {
                        steps.InsideLoop.Add(new Step(1, "if (found)"));
                        steps.InsideLoop.Add(new Step(1, "{"));
                        steps.InsideLoop.Add(new Step(2, "throw new global::System.InvalidOperationException(\"Sequence contains more than one element\");"));
                        steps.InsideLoop.Add(new Step(1, "}"));
                    }
                    steps.InsideLoop.Add(new Step(1, "found = true;"));
                    steps.InsideLoop.Add(new Step(1, "result = selected;"));
                    return;
                }
                steps.InsideLoop.Add(new Step(1, "{"));
                if (single)
                {
                    steps.InsideLoop.Add(new Step(2, "if (found)"));
                    steps.InsideLoop.Add(new Step(2, "{"));
                    steps.InsideLoop.Add(new Step(3, "throw new global::System.InvalidOperationException(\"Sequence contains more than one element\");"));
                    steps.InsideLoop.Add(new Step(2, "}"));
                }
                steps.InsideLoop.Add(new Step(2, "found = true;"));
                steps.InsideLoop.Add(new Step(2, "result = selected;"));
                steps.InsideLoop.Add(new Step(1, "}"));
            }
        }

        private static void AppendSteps(StringBuilder source, int baseIndent, List<Step> steps)
        {
            for (int i = 0, count = steps.Count; i < count; i++)
            {
                var step = steps[i];
                var indent = baseIndent + step.NestLevel;
                for (int level = 0; level < indent; level++)
                {
                    source.Append("    ");
                }
                source.AppendLine(step.Text);
            }
        }
    }
}
