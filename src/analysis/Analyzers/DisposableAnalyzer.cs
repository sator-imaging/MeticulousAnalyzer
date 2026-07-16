// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DisposableAnalyzer : DiagnosticAnalyzer
    {
        public const string SuppressionComment = "// Don't dispose";

        private const string UnknownLocalName = "<unknown>";

        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_MissingUsing = "SMA0040";
        private static readonly DiagnosticDescriptor Rule_MissingUsing = new(
            RuleId_MissingUsing,
            new LocalizableResourceString(nameof(Resources.SMA0040_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0040_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0040_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_NullAssignmentToDisposable = "SMA0041";
        private static readonly DiagnosticDescriptor Rule_NullAssignmentToDisposable = new(
            RuleId_NullAssignmentToDisposable,
            new LocalizableResourceString(nameof(Resources.SMA0041_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0041_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0041_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_NotAllCodePathsReturn = "SMA0042";
        private static readonly DiagnosticDescriptor Rule_NotAllCodePathsReturn = new(
            RuleId_NotAllCodePathsReturn,
            new LocalizableResourceString(nameof(Resources.SMA0042_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0042_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0042_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_MissingUsing,
            Rule_NullAssignmentToDisposable,
            Rule_NotAllCodePathsReturn
            );


        private static bool IsDuckTypingEnabled = false;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            // TODO: As roslyn triggers compilation start only on file is saved (Ctrl+S is pressed).
            //       Registering action in compilation start action is **correct but not ideal** because
            //       the analyzer feedback is not reported until Ctrl+S is pressed.
            //       For now, basic best-effort configuration support is sufficient.
            context.RegisterCompilationStartAction(ctx =>
            {
                IsDuckTypingEnabled = Core.GetGlobalConfigurationBoolean(ctx, Core.Config_EnableDuckTypingRecognition);
            });

            context.RegisterOperationAction(AnalyzeCast, OperationKind.Conversion);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzeUsualCreation, OperationKind.ObjectCreation);
            context.RegisterOperationAction(AnalyzeAnonymousCreation, OperationKind.AnonymousObjectCreation);
            context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(AnalyzeArrayElementReference, OperationKind.ArrayElementReference);
            context.RegisterOperationAction(AnalyzeNullAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(AnalyzeAwait, OperationKind.Await);
        }


        /*  entry  ================================================================ */

        private static void AnalyzeAwait(OperationAnalysisContext context)
        {
            if (context.Operation is not IAwaitOperation op)
            {
                return;
            }

            if (!IsDisposable(context, op.Type))
            {
                return;
            }

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }

        private static void AnalyzeCast(OperationAnalysisContext context)
        {
            if (context.Operation is not IConversionOperation op)
            {
                return;
            }

            // Ignore conversions from null, as this is handled by AnalyzeSimpleAssignment.
            if (op.Operand.ConstantValue.HasValue && op.Operand.ConstantValue.Value == null)
            {
                return;
            }

            bool isResultDisposable = IsDisposable(context, op.Type);
            bool isSourceDisposable = IsDisposable(context, op.Operand.Type);

            // both are disposable OR both are not disposable
            if (isResultDisposable == isSourceDisposable)
            {
                return;
            }

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }


        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation op)
            {
                return;
            }

            var interlockedType = context.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName: "System.Threading.Interlocked");
            if (interlockedType != null && SymbolEqualityComparer.Default.Equals(op.TargetMethod.ContainingType, interlockedType))
            {
                return;
            }

            var returnSymbol = op.TargetMethod.ReturnType;
            if (!IsDisposable(context, returnSymbol))
            {
                return;
            }

            CheckAssignmentAndUsingStatementExistence(context, op, returnSymbol);
        }


        private static void AnalyzeUsualCreation(OperationAnalysisContext context)
        {
            if (context.Operation is not IObjectCreationOperation op)
            {
                return;
            }

            if (!IsDisposable(context, op.Type))
            {
                return;
            }

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }

        private static void AnalyzeAnonymousCreation(OperationAnalysisContext context)
        {
            if (context.Operation is not IAnonymousObjectCreationOperation op)
            {
                return;
            }

            if (!IsDisposable(context, op.Type))
            {
                return;
            }

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }


        private static void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IPropertyReferenceOperation op)
            {
                return;
            }

            if (!IsDisposable(context, op.Type))
            {
                return;
            }

            // ignore right hand
            if (op.Parent is IAssignmentOperation assignOp)
            {
                if (op == assignOp.Value)
                {
                    return;
                }
            }
            else
            {
                if (op.Syntax.Parent is EqualsValueClauseSyntax equalsStx
                 && op.Syntax == equalsStx.Value
                )
                {
                    return;
                }
            }

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }

        private static void AnalyzeArrayElementReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IArrayElementReferenceOperation op)
            {
                return;
            }

            if (!IsDisposable(context, op.Type))
            {
                return;
            }

            // ignore right hand
            if (op.Parent is IAssignmentOperation assignOp)
            {
                if (op == assignOp.Value)
                {
                    return;
                }
            }
            else
            {
                if (op.Syntax.Parent is EqualsValueClauseSyntax equalsStx
                 && op.Syntax == equalsStx.Value
                )
                {
                    return;
                }
            }

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }

        private static void AnalyzeNullAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not IAssignmentOperation assignmentOp)
            {
                return;
            }

            // Check if the assigned value is null
            if (assignmentOp.Value.ConstantValue.HasValue && assignmentOp.Value.ConstantValue.Value == null)
            {
                // Check if the target is a disposable type
                if (IsDisposable(context, assignmentOp.Target.Type))
                {
                    var semanticModel = assignmentOp.SemanticModel ?? context.Compilation.GetSemanticModel(assignmentOp.Syntax.SyntaxTree);
                    var targetSymbolInfo = semanticModel.GetSymbolInfo(assignmentOp.Target.Syntax);
                    if (targetSymbolInfo.Symbol == null)
                    {
                        return;
                    }

                    if (assignmentOp.Syntax.Parent is not ExpressionStatementSyntax assignmentStatement)
                    {
                        return;
                    }

                    if (assignmentStatement.Parent is not BlockSyntax block)
                    {
                        return;
                    }

                    var statements = block.Statements;
                    int assignmentIndex = statements.IndexOf(assignmentStatement);

                    if (assignmentIndex > 0)
                    {
                        var precedingStatement = statements[assignmentIndex - 1];

                        if (precedingStatement is ExpressionStatementSyntax expressionStatement)
                        {
                            ExpressionSyntax? invocationTargetExpression = null;
                            SimpleNameSyntax? disposeMethodName = null;

                            // d.Dispose()
                            if (expressionStatement.Expression is InvocationExpressionSyntax invocation &&
                                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                            {
                                disposeMethodName = memberAccess.Name;
                                invocationTargetExpression = memberAccess.Expression;
                            }
                            // d?.Dispose()
                            else if (expressionStatement.Expression is ConditionalAccessExpressionSyntax conditionalAccess &&
                                     conditionalAccess.WhenNotNull is InvocationExpressionSyntax invocationOnNotNull &&
                                     invocationOnNotNull.Expression is MemberBindingExpressionSyntax memberBinding)
                            {
                                disposeMethodName = memberBinding.Name;
                                invocationTargetExpression = conditionalAccess.Expression;
                            }

                            if (disposeMethodName != null && disposeMethodName.Identifier.Text == "Dispose" && invocationTargetExpression != null)
                            {
                                var disposeTargetSymbolInfo = semanticModel.GetSymbolInfo(invocationTargetExpression);
                                if (disposeTargetSymbolInfo.Symbol != null && SymbolEqualityComparer.Default.Equals(targetSymbolInfo.Symbol, disposeTargetSymbolInfo.Symbol))
                                {
                                    // The dispose call is on the same variable. We're good.
                                    return;
                                }
                            }
                        }
                    }

                    // If we get here, no preceding dispose call was found. Report the diagnostic.
                    context.ReportDiagnostic(Diagnostic.Create(Rule_NullAssignmentToDisposable, assignmentOp.Syntax.GetLocation(), assignmentOp.Target.Type.ToDiagnosticMessageName()));
                }
            }
        }


        /*  internal  ================================================================ */

#pragma warning disable RS1008  // Avoid storing per-compilation data into the fields of a diagnostic analyzer
        // NOTE: This is required to skip null check on every visit on local var declaration.
        readonly static Func<INamedTypeSymbol, bool> cache_HasDisposableImplemented = static x => HasDisposableImplemented(x);
#pragma warning restore RS1008

        private static bool HasDisposableImplemented(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.SpecialType is SpecialType.System_IDisposable)
            {
                return true;
            }

            // TODO: SpecialType enum item for 'IAsyncDisposable'
            return typeSymbol.Name is "IAsyncDisposable"
                && typeSymbol.ContainingNamespace is INamespaceSymbol
                {
                    Name: "System", ContainingNamespace: INamespaceSymbol
                    {
                        IsGlobalNamespace: true,
                    }
                };
        }

        private static bool IsDisposable(OperationAnalysisContext context, ITypeSymbol? disposableSymbol)
        {
            if (disposableSymbol is null)
            {
                return false;
            }

#if STMG_ENABLE_DISPOSABLE_ANALYZER_ATTRIBUTE
            if (IsTypeIgnoredByAssemblyAttribute(context, disposableSymbol))
            {
                return false;
            }
#endif

            // Task implements IDisposable...!!
            if (disposableSymbol.Name is nameof(Task) &&
                disposableSymbol.ContainingNamespace is INamespaceSymbol
                {
                    Name: nameof(System.Threading.Tasks), ContainingNamespace: INamespaceSymbol
                    {
                        Name: nameof(System.Threading), ContainingNamespace: INamespaceSymbol
                        {
                            Name: nameof(System), ContainingNamespace: INamespaceSymbol
                            {
                                IsGlobalNamespace: true,
                            }
                        }
                    }
                }
            )
            {
                return false;
            }

            if (HasDisposableImplemented(disposableSymbol) ||
                disposableSymbol.AllInterfaces.Any(cache_HasDisposableImplemented)
            )
            {
                return true;
            }

            if (!IsDuckTypingEnabled)
            {
                return false;
            }

            return detect_duck_typing(disposableSymbol);
            static bool detect_duck_typing(ITypeSymbol disposableSymbol)
            {
                var candidateMethods = disposableSymbol.GetMembers()
                    .OfType_Where<IMethodSymbol>(static x => x.Parameters.Length == 0
                                                          && x.DeclaredAccessibility >= Accessibility.Internal);

                var isDisposable = candidateMethods
                    .Where_Any(static x => x.Name == nameof(IDisposable.Dispose)
                                        && x.ReturnType.SpecialType is SpecialType.System_Void);

                if (isDisposable)
                {
                    return true;
                }

                return candidateMethods
                    .Where_Any(static x
                        // TODO: SpecialType enum item for 'ValueTask'
                        => x.Name == "DisposeAsync"
                        && x.ReturnType.Name is nameof(ValueTask)
                        && x.ReturnType.ContainingNamespace is INamespaceSymbol
                        {
                            Name: nameof(System.Threading.Tasks), ContainingNamespace: INamespaceSymbol
                            {
                                Name: nameof(System.Threading), ContainingNamespace: INamespaceSymbol
                                {
                                    Name: nameof(System), ContainingNamespace: INamespaceSymbol
                                    {
                                        IsGlobalNamespace: true,
                                    }
                                }
                            }
                        });
            }
        }


        private static bool IsTypeIgnoredByAssemblyAttribute(OperationAnalysisContext context, ITypeSymbol disposableSymbol)
        {
            const string ATTR_NAME = "DisposableAnalyzerSuppressor";

            foreach (var attr in context.Compilation.Assembly.GetAttributes())
            {
                if (attr.AttributeClass?.Name == ATTR_NAME)
                {
                    foreach (var ctorArg in attr.ConstructorArguments)
                    {
                        if (ctorArg.Kind != TypedConstantKind.Array)
                        {
                            if (ctorArg.Value is ITypeSymbol typeSymbol && SymbolEqualityComparer.Default.Equals(disposableSymbol, typeSymbol))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            foreach (var argument in ctorArg.Values)
                            {
                                //Core.ReportDebugMessage(context.ReportDiagnostic, context.Operation);

                                if (argument.Value is ITypeSymbol typeSymbol && SymbolEqualityComparer.Default.Equals(disposableSymbol, typeSymbol))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        private static bool TryUnwrapSafeConversion(
            OperationAnalysisContext ctx,
            IConversionOperation castOp,
            out IOperation operation,
            out ITypeSymbol symbol)
        {
            var safeCastOnly = true;

            var safeRootOp = castOp;
            do
            {
                if (!IsDisposable(ctx, castOp.Type))
                {
                    safeCastOnly = false;
                    break;
                }

                safeRootOp = castOp;
            }
            while ((castOp = (((castOp.Parent as IConversionOperation)))!) != null);

            operation = safeRootOp;
            symbol = safeRootOp.Type;

            return safeCastOnly;
        }


        /*  check  ================================================================ */

        private static void CheckAssignmentAndUsingStatementExistence(
            OperationAnalysisContext context,
            IOperation operation,
            ITypeSymbol disposableSymbol
        )
        {
            var focusedSymbol = disposableSymbol;
            var focusedOp = operation;

            // MUST check before unpacking conversion operation.
            bool isCreationOp = focusedOp.Kind is OperationKind.ObjectCreation
                                               or OperationKind.AnonymousObjectCreation
                                               or OperationKind.TypeParameterObjectCreation
                                               or OperationKind.DefaultValue
                                               ;

            // NOTE: Unpack conversion operation.
            //       --> Method(new Disposable())
            //                  ^^^^^^^^^^^^^^^^ Cast may happen implicitly
            ITypeSymbol? untrackedCastOperandType = null;
            {
                if (focusedOp is IConversionOperation castOp)
                {
                    if (!TryUnwrapSafeConversion(context, castOp, out focusedOp, out focusedSymbol))
                    {
                        // Don't consider '+' or other binary operation.
                        // It may create new IDisposable instance? haha.
                        if (focusedOp.Parent is not IBinaryOperation
                                            and not IIsPatternOperation)
                        {
                            // NOTE: Don't exit here.
                            //       Need to check parent operation for:
                            //       - using var ...
                            //       - foreach (var item in ...
                            untrackedCastOperandType = castOp.Operand.Type;
                        }
                    }
                }
            }

            // NOTE: Unpack ternary or coalesce operation.
            //       --> condition ? Method() : new Disposable()
            //       --> Method() ?? throw new Exception()
            if (focusedOp.Parent is IConditionalOperation or ICoalesceOperation)
            {
                focusedOp = focusedOp.Parent;
            }

            // Unwrap '?.' operation chain.
            // --> a?.b?.c?.member
            //     ~~~~~~~~~ Unpack all chained access
            focusedOp = Core.UnwrapAllNullCoalesceOperation(focusedOp);

            // 'using' or 'foreach' statement?
            // --> using (new Disposable()) { ... }
            // --> using var x = new...
            // --> foreach (var foo in some.MethodOrPropertyReturnsDisposable())
            {
                // Unwrap only once.
                var parentOp = focusedOp.Parent;
                if (parentOp is IConversionOperation castOp)
                {
                    parentOp = castOp.Parent;
                }

                if (parentOp is IUsingOperation
                             or IUsingDeclarationOperation
                             or IForEachLoopOperation
                    // Arrow return
                    // --> Foo() => disposable;
                    // --> Foo() => bar.MethodReturnsDisposable();
                    || (
                        parentOp is IReturnOperation ret &&
                        ret.Parent is IBlockOperation block &&
                        block.Parent is IMethodBodyBaseOperation method &&
                        method.ExpressionBody == block
                    )
                )
                {
                    goto NO_WARN;
                }
            }


            // No 'using' and 'foreach' found.
            // Report untracked cast operation here.
            if (untrackedCastOperandType is not null)
            {
                if (!Core.IsSuppressedByComment(focusedOp, SuppressionComment))
                {
                    var reportType = IsDisposable(context, disposableSymbol)
                        ? disposableSymbol
                        : untrackedCastOperandType;

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_MissingUsing, operation.Syntax.GetLocation(), reportType.ToDiagnosticMessageName()));
                }

                return;
            }


            if (IsOperationIgnorable(context, isCreationOp, ref focusedOp))
            {
                goto NO_WARN;
            }

            static bool IsOperationIgnorable(
                OperationAnalysisContext context,
                bool isCreationOp,
                ref IOperation focusedOp
            )
            {
                // Method argument?
                {
                    if (focusedOp.Parent is IArgumentOperation argumentOp)
                    {
                        if (!isCreationOp)
                        {
                            return true;
                        }

                        if (argumentOp.Parent is IInvocationOperation invocationOp)
                        {
                            // Interlocked methods are intentionally allowed.
                            if (invocationOp.TargetMethod.ContainingType is ITypeSymbol
                                {
                                    Name: nameof(Interlocked), ContainingNamespace: INamespaceSymbol
                                    {
                                        Name: nameof(System.Threading), ContainingNamespace: INamespaceSymbol
                                        {
                                            Name: nameof(System), ContainingNamespace: INamespaceSymbol
                                            {
                                                IsGlobalNamespace: true,
                                            },
                                        },
                                    },
                                })
                            {
                                return true;
                            }
                        }
                    }
                }

                if (!isCreationOp)
                {
                    // Comparison?
                    // --> foo == null
                    // --> foo != other
                    // --> is null
                    // --> is not null
                    if (focusedOp.Parent is IBinaryOperation
                                         or IIsPatternOperation)
                    {
                        return true;
                    }

                    // Member reference!!
                    // --> disposable.Property;
                    // --> disposable?.Property;
                    // --> disposable.Return();
                    // --> disposable?.Return();
                    if (focusedOp is IMemberReferenceOperation memberRefOp)
                    {
                        if (!IsDisposable(context, memberRefOp.Type))
                        {
                            return true;
                        }
                        else
                        {
                            var parentOp = Core.UnwrapAllNullCoalesceOperation(focusedOp.Parent);
                            if (parentOp is IMemberReferenceOperation or ILocalReferenceOperation)
                            {
                                return true;
                            }
                            else
                            {
                                // NOTE: Need to check subsequent method chain
                                //       --> ...Prop.ToString();
                                //                   ^^^^^^^^^^
                                if (parentOp is IInvocationOperation invokeOp)
                                {
                                    if (!IsDisposable(context, invokeOp.TargetMethod.ReturnType))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                return false;
            }


            // NOTE: In the following, cannot use I***Operation to analyze usage
            //       because unity doesn't allow using latest roslyn analyzer
            var syntax = focusedOp.Syntax;

            if (IsSyntaxIgnorable(context, isCreationOp, ref syntax, ref focusedOp, ref focusedSymbol))
            {
                goto NO_WARN;
            }

            static bool IsSyntaxIgnorable(
                OperationAnalysisContext context,
                bool isCreationOp,
                ref SyntaxNode syntax,
                ref IOperation focusedOp,
                ref ITypeSymbol focusedSymbol
            )
            {
                // NOTE: Remove parenthesizes and null warning suppressor!!
                //       --> (((new Disposable()))) --> new Disposable()
                //       --> (new Disposable())! --> new Disposable()
                while (syntax.Parent?.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.SuppressNullableWarningExpression)
                {
                    syntax = syntax.Parent;
                }


                // NOTE: If switch arm expression found, move focus to parent expression
                //       > var x = value switch { ... };
                //                              ~~~~~~~ current focus
                //       > var x = value switch { ... };
                //                 ~~~~~~~~~~~~~~~~~~~~ moving to here
                {
                    if (focusedOp.Parent is ISwitchExpressionArmOperation switchArmOp &&
                        switchArmOp.Parent is ISwitchExpressionOperation switchOp
                    )
                    {
                        focusedOp = switchOp;
                        focusedSymbol = switchOp.Type;

                        syntax = switchOp.Syntax;
                    }
                }


                // Return statement?
                // --> Method() => new Disposable();
                // --> Method() { return new Disposable(); }
                {
                    if (syntax.Parent is ArrowExpressionClauseSyntax or ReturnStatementSyntax or YieldStatementSyntax)
                    {
                        return true;
                    }
                }

                // NOTE: IUsingOperation is not pointing to block-less using syntax --> using var x = ...
                if (syntax.Parent is EqualsValueClauseSyntax equalsStx)
                {
                    return AnalyzeEqualsSyntax(context, equalsStx);
                    static bool AnalyzeEqualsSyntax(
                        OperationAnalysisContext context,
                        EqualsValueClauseSyntax equalsStx
                    )
                    {
                        // 'using' statement w/o block scope?
                        // --> using var x = new Disposable();
                        // --> using(var x = new Disposable()) { ... }
                        if (equalsStx.Parent is VariableDeclaratorSyntax declaratorStx &&
                            declaratorStx.Parent is VariableDeclarationSyntax varDeclStx
                        )
                        {
                            var parStx = varDeclStx.Parent;
                            if (parStx is UsingStatementSyntax or MemberDeclarationSyntax)
                            {
                                return true;
                            }
                            else if (parStx is LocalDeclarationStatementSyntax localVarStx)
                            {
                                // DON'T check localVarStx variable type is disposable or not.
                                // Just check using keyword existence.
                                if (localVarStx.UsingKeyword != default)
                                {
                                    return true;
                                }

                                if (Core.IsSuppressedByComment(localVarStx, SuppressionComment))
                                {
                                    return true;
                                }

                                if (localVarStx.Declaration.Variables.Count == 1)
                                {
                                    var localVarDeclaratorStx = localVarStx.Declaration.Variables[0];
                                    if (IsLocalVariableReturned(context, localVarDeclaratorStx, out var inAllCodePaths))
                                    {
                                        if (!inAllCodePaths)
                                        {
                                            // NOTE: Workaround for Roslyn bug
                                            //       Even through 'localVarDeclaratorStx.Identifier.ToString()' returns 'd',
                                            //       'localVarDeclaratorStx.Identifier.GetLocation()' may NOT be pointing 'd' location.
                                            //       As a result, although a violation is detected correctly, a warning is not reported at all.
                                            //       --> var d = new MyDisposable();
                                            //               ~~~~~~~~~~~~~~~~~~~~~~  fixed location (declarator syntax; formerly 'd' only)

                                            // Reporting detailed diagnostic instead of generic one.
                                            var localName = context.Operation.SemanticModel
                                                .GetDeclaredSymbol(localVarDeclaratorStx)?.ToDiagnosticMessageName()
                                                ?? UnknownLocalName;
                                            context.ReportDiagnostic(Diagnostic.Create(
                                                Rule_NotAllCodePathsReturn, localVarDeclaratorStx.GetLocation(), localName));
                                        }

                                        // Then, just go to NO_WARN to avoid additionally reporting SMA0040.
                                        return true;
                                    }
                                }
                            }
                        }

                        return false;
                    }
                }

                return AnalyzeOtherSyntax(context, isCreationOp, ref syntax, ref focusedOp);
                static bool AnalyzeOtherSyntax(
                    OperationAnalysisContext context,
                    bool isCreationOp,
                    ref SyntaxNode syntax,
                    ref IOperation focusedOp
                )
                {
                    // NOTE: Ignore field/property assignment even if field/property type is disposable
                    //       --> Field = new Disposable();
                    //       --> Property = new Disposable();
                    if (syntax.Parent is AssignmentExpressionSyntax assignStx)
                    {
                        var leftStx = assignStx.Left;

                        var model = focusedOp.SemanticModel ?? context.Compilation.GetSemanticModel(syntax.SyntaxTree);
                        var leftOp = model.GetOperation(leftStx);

                        // Discarding?
                        if (leftOp is IDiscardOperation)
                        {
                            // Won't allow silent suppression
                            if (Core.IsSuppressedByComment(assignStx, SuppressionComment, isDiscardOperation: true))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // Left hand is indexer?
                            if (leftOp is IArrayElementReferenceOperation elementOp)
                            {
                                leftOp = elementOp.ArrayReference;
                            }
                            else if (leftOp is IPropertyReferenceOperation indexerOp && indexerOp.Property.IsIndexer)
                            {
                                leftOp = indexerOp.Instance;
                            }

                            // Ignore field/property
                            if (leftOp?.Kind is OperationKind.FieldReference or OperationKind.PropertyReference)
                            {
                                return true;
                            }
                        }
                    }
                    // --> if (disposable == ...)
                    // --> while (disposable == ...)
                    else if (focusedOp.Parent is IBinaryOperation)
                    {
                        // don't allow creation operation pass the warning
                        if (!isCreationOp)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }


            // !! REPORT !!
            context.ReportDiagnostic(Diagnostic.Create(
                Rule_MissingUsing, syntax.GetLocation(), disposableSymbol.ToDiagnosticMessageName()));

            return;


        NO_WARN:

            return;
        }

        private static bool IsLocalVariableReturned(OperationAnalysisContext context, VariableDeclaratorSyntax variableDeclarator, out bool inAllCodePaths)
        {
            inAllCodePaths = false;

            var enclosingMember = variableDeclarator.Ancestors().FirstOrDefault(static x => x is MethodDeclarationSyntax or AccessorDeclarationSyntax);
            if (enclosingMember == null)
            {
                return false;
            }

            var semanticModel = context.Operation.SemanticModel;
            if (semanticModel == null)
            {
                return false;
            }

            var declaredSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
            if (declaredSymbol == null)
            {
                return false;
            }

            SyntaxNode? body = null;
            ArrowExpressionClauseSyntax? expressionBody = null;

            if (enclosingMember is MethodDeclarationSyntax method)
            {
                body = method.Body;
                expressionBody = method.ExpressionBody;
            }
            else if (enclosingMember is AccessorDeclarationSyntax accessor)
            {
                body = accessor.Body;
                expressionBody = accessor.ExpressionBody;
            }

            if (expressionBody != null)
            {
                if (expressionBody.Expression is ThrowExpressionSyntax)
                {
                    // NOTE: keep consistent with statement syntax.
                    return false;
                }

                if (expressionBody.Expression is IdentifierNameSyntax identifierName)
                {
                    var returnedSymbol = semanticModel.GetSymbolInfo(identifierName).Symbol;
                    var isReturned = SymbolEqualityComparer.Default.Equals(returnedSymbol, declaredSymbol);

                    inAllCodePaths = isReturned;
                    return isReturned;
                }
            }

            if (body != null)
            {
                if (body.DescendantNodes().Any(static x => x is ThrowStatementSyntax or ThrowExpressionSyntax))
                {
                    // NOTE: keep consistent with '=> ...' syntax.
                    return false;  // assumes that some paths throw (reports generic diagnostic)
                }

                var controlFlow = semanticModel.AnalyzeControlFlow(body);
                if (!controlFlow.Succeeded || controlFlow.EndPointIsReachable || controlFlow.ReturnStatements.IsEmpty)
                {
                    return false;
                }

                var allReturnStatements = controlFlow.ReturnStatements;
                var returnStatements = allReturnStatements.OfType<ReturnStatementSyntax>().ToArray();

                if (returnStatements.Length == 0 ||
                    returnStatements.Length != allReturnStatements.Length)
                {
                    // If not all return statements can be cast to ReturnStatementSyntax,
                    // we can't be sure about the variable's lifecycle.
                    return false;
                }

                var isVariableEverReturned = false;
                int handledPaths = 0;

                foreach (var returnSyntax in returnStatements)
                {
                    if (returnSyntax.Expression is IdentifierNameSyntax identifierName)
                    {
                        var returnedSymbol = semanticModel.GetSymbolInfo(identifierName).Symbol;
                        if (SymbolEqualityComparer.Default.Equals(returnedSymbol, declaredSymbol))
                        {
                            isVariableEverReturned = true;
                            handledPaths++;
                        }
                    }

                    // NOTE: The following code is preserved to prevent relaxing the restriction unexpectedly.
                    //       * This disposable analyzer is designed in restrict-first and we accept false positive.

                    /*
                    else if (returnSyntax.Expression is null)
                    {
                        // e.g. return;
                        // This path does not return the variable, but it's a valid exit.
                        // We don't increment handledPaths here because the variable is not returned.
                    }
                    // // NOTE: Disallow returning 'null' if local is declared.
                    // //       * Analyzer already allows returning a new instance on 'return' statement.
                    // //         So instead, create 2 paths one returns new instance, another one returns null.
                    // else if (returnSyntax.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                    // {
                    //     handledPaths++;
                    // }
                    else
                    {
                        // Another variable or a new object is returned.
                        // This path is handled, but doesn't return our variable.
                    }
                    */
                }

                inAllCodePaths = (handledPaths == returnStatements.Length);
                return isVariableEverReturned;
            }

            return false;
        }
    }
}
