// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaskAnalyzer : DiagnosticAnalyzer
    {
        private const string SuppressionComment = "// Don't await";

        public const string RuleId_MissingAwait = "SMA0070";
        private static readonly DiagnosticDescriptor Rule_MissingAwait = new(
            RuleId_MissingAwait,
            new LocalizableResourceString(nameof(Resources.SMA0070_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0070_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0070_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_NotAllCodePathsAwait = "SMA0071";
        private static readonly DiagnosticDescriptor Rule_NotAllCodePathsAwait = new(
            RuleId_NotAllCodePathsAwait,
            new LocalizableResourceString(nameof(Resources.SMA0071_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0071_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0071_Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rule_MissingAwait,
            Rule_NotAllCodePathsAwait
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzeSimpleAssignment, OperationKind.SimpleAssignment);
        }

        private static void AnalyzeSimpleAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not ISimpleAssignmentOperation assignment)
            {
                return;
            }

            if (assignment.Target is not IDiscardOperation)
            {
                return;
            }

            if (!IsTask(assignment.Value.Type))
            {
                return;
            }

            if (Core.IsSuppressedByComment(assignment.Syntax, SuppressionComment, isDiscardOperation: true))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_MissingAwait, assignment.Value.Syntax.GetLocation(), assignment.Target.Syntax.ToString()));
        }

        private static void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            if (context.Operation is not IVariableDeclaratorOperation declarator)
            {
                return;
            }

            var local = declarator.Symbol;
            if (!IsTask(local.Type))
            {
                return;
            }

            if (declarator.Syntax is not VariableDeclaratorSyntax syntax)
            {
                return;
            }

            if (syntax.Initializer == null)
            {
                return;
            }

            // NOTE: Won't support supressing with discard. e.g. `_ = MyTask();`
            //       --> Declarator -> Declaration -> LocalDeclarationStatement
            if (Core.IsSuppressedByComment(declarator.Parent.Parent.Syntax, SuppressionComment))
            {
                return;
            }

            if (IsTaskAwaitedOrReturned(context, syntax, out var inAllCodePaths))
            {
                if (!inAllCodePaths)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule_NotAllCodePathsAwait, syntax.Identifier.GetLocation(), local.Name));
                }
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule_MissingAwait, syntax.Identifier.GetLocation(), local.Name));
        }

        private static bool IsTask(ITypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            if (type is INamedTypeSymbol { Name: "Task" or "ValueTask", ContainingNamespace: { Name: "Tasks", ContainingNamespace: { Name: "Threading", ContainingNamespace: { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } } } } })
            {
                return true;
            }

            return IsTask(type.BaseType);
        }


        private static bool IsTaskAwaitedOrReturned(OperationAnalysisContext context, VariableDeclaratorSyntax variableDeclarator, out bool inAllCodePaths)
        {
            inAllCodePaths = false;

            var enclosingMember = variableDeclarator.Ancestors().FirstOrDefault(static x => x is MethodDeclarationSyntax or AccessorDeclarationSyntax or AnonymousFunctionExpressionSyntax);
            if (enclosingMember == null)
            {
                return false;
            }

            var semanticModel = context.Operation.SemanticModel;
            if (semanticModel == null)
            {
                return false;
            }

            var localSymbol = (ILocalSymbol?)semanticModel.GetDeclaredSymbol(variableDeclarator);
            if (localSymbol == null)
            {
                return false;
            }

            ControlFlowGraph cfg;
            try
            {
                cfg = ControlFlowGraph.Create(enclosingMember, semanticModel);
            }
            catch
            {
                return false;
            }

            var handledBlocks = new HashSet<int>();
            int declarationBlock = -1;
            var allBlocks = cfg.Blocks;

            for (int i = 0; i < allBlocks.Length; i++)
            {
                var block = allBlocks[i];
                bool isHandled = false;

                var operations = new List<IOperation>(block.Operations.Length + 1);
                foreach (var op in block.Operations)
                {
                    operations.Add(op);
                }

                if (block.BranchValue != null)
                {
                    operations.Add(block.BranchValue);
                }

                foreach (var op in operations)
                {
                    if (declarationBlock == -1 && op.Syntax.AncestorsAndSelf().Contains(variableDeclarator))
                    {
                        declarationBlock = i;
                    }

                    foreach (var desc in op.DescendantsAndSelf())
                    {
                        if (desc is IAwaitOperation awaitOp)
                        {
                            var operand = awaitOp.Operation;
                            while (operand is IConversionOperation conv)
                            {
                                operand = conv.Operand;
                            }

                            if (operand is ILocalReferenceOperation lr && SymbolEqualityComparer.Default.Equals(lr.Local, localSymbol))
                            {
                                isHandled = true;
                                break;
                            }
                        }
                        else if (desc is IReturnOperation returnOp && returnOp.ReturnedValue != null)
                        {
                            var val = returnOp.ReturnedValue;
                            while (val is IConversionOperation conv)
                            {
                                val = conv.Operand;
                            }

                            if (val is ILocalReferenceOperation lr && SymbolEqualityComparer.Default.Equals(lr.Local, localSymbol))
                            {
                                isHandled = true;
                                break;
                            }
                        }
                        else if (desc is ILocalReferenceOperation lr && SymbolEqualityComparer.Default.Equals(lr.Local, localSymbol))
                        {
                            if (op == block.BranchValue && block.FallThroughSuccessor?.Destination.Kind == BasicBlockKind.Exit)
                            {
                                isHandled = true;
                                break;
                            }
                        }
                    }
                    if (isHandled)
                    {
                        break;
                    }
                }

                if (isHandled)
                {
                    handledBlocks.Add(i);
                }
            }

            if (handledBlocks.Count == 0)
            {
                return false;
            }

            if (declarationBlock == -1)
            {
                return false;
            }

            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(declarationBlock);

            while (stack.Count > 0)
            {
                int currentOrdinal = stack.Pop();
                if (visited.Contains(currentOrdinal))
                {
                    continue;
                }

                if (handledBlocks.Contains(currentOrdinal))
                {
                    continue;
                }

                visited.Add(currentOrdinal);
                var currentBlock = allBlocks[currentOrdinal];

                if (currentBlock.Kind == BasicBlockKind.Exit)
                {
                    inAllCodePaths = false;
                    return true;
                }

                if (currentBlock.FallThroughSuccessor != null)
                {
                    stack.Push(currentBlock.FallThroughSuccessor.Destination.Ordinal);
                }

                if (currentBlock.ConditionalSuccessor != null)
                {
                    stack.Push(currentBlock.ConditionalSuccessor.Destination.Ordinal);
                }
            }

            inAllCodePaths = true;
            return true;
        }
    }
}
