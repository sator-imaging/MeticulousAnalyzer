// Licensed under the MIT License
// https://github.com/sator-imaging/CSharp-StaticFieldAnalyzer

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
using System.Linq;
using System.Threading.Tasks;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DisposableAnalyzer : DiagnosticAnalyzer
    {
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

        public const string RuleId_NullAssignment = "SMA0041";
        private static readonly DiagnosticDescriptor Rule_NullAssignment = new(
            RuleId_NullAssignment,
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
            Rule_NullAssignment,
            Rule_NotAllCodePathsReturn
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            context.RegisterOperationAction(AnalyzeCast, OperationKind.Conversion);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzeUsualCreation, OperationKind.ObjectCreation);
            context.RegisterOperationAction(AnalyzeAnonymousCreation, OperationKind.AnonymousObjectCreation);
            context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(AnalyzeArrayElementReference, OperationKind.ArrayElementReference);
            context.RegisterOperationAction(AnalyzeSimpleAssignment, OperationKind.SimpleAssignment);
        }


        /*  entry  ================================================================ */

        private static void AnalyzeCast(OperationAnalysisContext context)
        {
            if (context.Operation is not IConversionOperation op)
                return;

            // Ignore conversions from null, as this is handled by AnalyzeSimpleAssignment.
            if (op.Operand.ConstantValue.HasValue && op.Operand.ConstantValue.Value == null)
            {
                return;
            }

            bool isResultDisposable = op.Type is INamedTypeSymbol resultSymbol && IsDisposable(context, resultSymbol);
            bool isSourceDisposable = op.Operand.Type is INamedTypeSymbol sourceSymbol && IsDisposable(context, sourceSymbol);

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
                return;

            var interlockedType = context.Compilation.GetTypeByMetadataName("System.Threading.Interlocked");
            if (interlockedType != null && SymbolEqualityComparer.Default.Equals(op.TargetMethod.ContainingType, interlockedType))
            {
                return;
            }

            var returnSymbol = op.TargetMethod.ReturnType;
            if (returnSymbol is not INamedTypeSymbol named || !IsDisposable(context, named))
                return;

            CheckAssignmentAndUsingStatementExistence(context, op, returnSymbol);
        }


        private static void AnalyzeUsualCreation(OperationAnalysisContext context)
        {
            if (context.Operation is not IObjectCreationOperation op)
                return;

            if (op.Type is not INamedTypeSymbol named || !IsDisposable(context, named))
                return;

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }

        private static void AnalyzeAnonymousCreation(OperationAnalysisContext context)
        {
            if (context.Operation is not IAnonymousObjectCreationOperation op)
                return;

            if (op.Type is not INamedTypeSymbol named || !IsDisposable(context, named))
                return;

            CheckAssignmentAndUsingStatementExistence(context, op, op.Type);
        }


        private static void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            if (context.Operation is not IPropertyReferenceOperation op)
                return;

            if (op.Type is not INamedTypeSymbol named || !IsDisposable(context, named))
                return;

            // ignore right hand
            if (op.Parent is IAssignmentOperation assignOp)
            {
                if (op == assignOp.Value)
                    return;
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
                return;

            if (op.Type is not INamedTypeSymbol named || !IsDisposable(context, named))
                return;

            // ignore right hand
            if (op.Parent is IAssignmentOperation assignOp)
            {
                if (op == assignOp.Value)
                    return;
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

        private static void AnalyzeSimpleAssignment(OperationAnalysisContext context)
        {
            if (context.Operation is not IAssignmentOperation assignmentOp)
                return;

            // Check if the assigned value is null
            if (assignmentOp.Value.ConstantValue.HasValue && assignmentOp.Value.ConstantValue.Value == null)
            {
                // Check if the target is a disposable type
                if (assignmentOp.Target.Type is INamedTypeSymbol targetTypeSymbol && IsDisposable(context, targetTypeSymbol))
                {
                    var semanticModel = context.Compilation.GetSemanticModel(assignmentOp.Syntax.SyntaxTree);
                    var targetSymbolInfo = semanticModel.GetSymbolInfo(assignmentOp.Target.Syntax);
                    if (targetSymbolInfo.Symbol == null)
                        return;

                    if (assignmentOp.Syntax.Parent is not ExpressionStatementSyntax assignmentStatement) return;
                    if (assignmentStatement.Parent is not BlockSyntax block) return;

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
                    context.ReportDiagnostic(Diagnostic.Create(Rule_NullAssignment, assignmentOp.Syntax.GetLocation(), targetTypeSymbol.Name));
                }
            }
        }


        /*  internal  ================================================================ */

        private static bool IsSuppressedByComment(SyntaxNode? node)
        {
            const string SuppressionComment = "// Don't dispose";

            if (node == null) return false;

            var comment = node
                .GetFirstToken()
                .LeadingTrivia
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));

            return comment != default
                && comment.ToString().StartsWith(SuppressionComment, StringComparison.OrdinalIgnoreCase);
        }


#pragma warning disable RS1008

        readonly static Func<INamedTypeSymbol, bool> HasDisposableImplemented = static x =>
        {
            if (x.SpecialType is SpecialType.System_IDisposable)
            {
                return true;
            }
            // TODO: SpecialType enum item for 'IAsyncDisposable'
            else if (x.Name == "IAsyncDisposable")
            {
                var ns = x.ContainingNamespace;
                if (ns.Name == nameof(System) && ns.ContainingNamespace.IsGlobalNamespace)
                {
                    return true;
                }
            }
            return false;
        };

#pragma warning restore RS1008


        private static bool IsDisposable(OperationAnalysisContext context,
                                         INamedTypeSymbol disposableSymbol
            )
        {
#if STMG_ENABLE_DISPOSABLE_ANALYZER_ATTRIBUTE
            if (IsTypeIgnoredByAssemblyAttribute(context, disposableSymbol))
            {
                return false;
            }
#endif

            if (!disposableSymbol.IsRefLikeType)
            {
                // Task or Task<T> in System.Threading.Tasks
                if (disposableSymbol.Name.StartsWith(nameof(Task), StringComparison.Ordinal)
                 && disposableSymbol.ContainingNamespace.Name == nameof(System.Threading.Tasks)
                 && disposableSymbol.ContainingNamespace.ContainingNamespace.Name == nameof(System.Threading)
                 && disposableSymbol.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name == nameof(System)
                 && disposableSymbol.ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace
                )
                {
                    return false;
                }

                if (HasDisposableImplemented.Invoke(disposableSymbol)
                 || disposableSymbol.Interfaces.Any(HasDisposableImplemented)
                 || disposableSymbol.AllInterfaces.Any(HasDisposableImplemented)
                )
                {
                    return true;
                }

                return false;
            }


            const Accessibility ACCESS_HIDDEN = Accessibility.Protected | Accessibility.Private | Accessibility.NotApplicable;

            var candidateMethods = disposableSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(static x => x.Parameters.Length == 0 && (x.DeclaredAccessibility & ~ACCESS_HIDDEN) != 0)
                ;

            var isDisposable = candidateMethods
                .Where(static x => x.Name == nameof(IDisposable.Dispose))
                .Any(static x => x.ReturnType.SpecialType == SpecialType.System_Void)
                ;
            if (!isDisposable)
            {
                var isAsyncDisposable = candidateMethods
                    .Where(static x => x.Name == "DisposeAsync")
                    .Any(static x =>
                    {
                        // TODO: SpecialType enum item for 'ValueTask'
                        if (x.ReturnType.Name != nameof(ValueTask))
                            return false;

                        var ns = x.ReturnType.ContainingNamespace;
                        if (ns.Name != nameof(System.Threading.Tasks)
                         || ns.ContainingNamespace.Name != nameof(System.Threading)
                         || ns.ContainingNamespace.ContainingNamespace.Name != nameof(System)
                         || !ns.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace
                        )
                        {
                            return false;
                        }

                        return true;
                    })
                    ;

                if (!isAsyncDisposable)
                    return false;
            }

            return true;
        }


        private static bool IsTypeIgnoredByAssemblyAttribute(OperationAnalysisContext context, INamedTypeSymbol disposableSymbol)
        {
            const string ATTR_NAME = "DisposableAnalyzerSuppressor";

            foreach (var attr in context.Compilation.Assembly.GetAttributes())
            {
                if (attr.AttributeClass.Name == ATTR_NAME)
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


        /*  check  ================================================================ */

        private static void CheckAssignmentAndUsingStatementExistence(OperationAnalysisContext context,
                                                                      IOperation op,
                                                                      ITypeSymbol? disposableSymbol
            )
        {
            if (disposableSymbol == null)
                return;

            // MUST check before unpacking implicit cast
            bool isCreationOp = op is IObjectCreationOperation
                                   or IAnonymousObjectCreationOperation
                                   or ITypeParameterObjectCreationOperation
                                   or IDefaultValueOperation
                                   ;

            // NOTE: unpack implicit cast operation
            //       --> Method(new Disposable())
            //                  ^^^^^^^^^^^^^^^^ implicit cast may happen
            {
                IConversionOperation? castOp = op.Parent as IConversionOperation;
                while (castOp != null)
                {
                    if (castOp.IsImplicit && castOp.Type is INamedTypeSymbol named && IsDisposable(context, named))
                    {
                        op = castOp;
                        disposableSymbol = op.Type;
                    }
                    castOp = castOp.Parent as IConversionOperation;
                }
            }


            // NOTE: this code can check using 'block' statement only
            //       --> using (new Disposable()) { ... }
            // NOTE: block-less using statement cannot be checked! it checked later!!
            //       --> using var x = new...
            {
                if (op.Parent is IUsingOperation)
                {
                    goto NO_WARN;
                }
            }


            // method argument?
            {
                if (op.Parent is IArgumentOperation argumentOp)
                {
                    if (argumentOp.Parent is IInvocationOperation invocationOp)
                    {
                        var interlockedType = context.Compilation.GetTypeByMetadataName("System.Threading.Interlocked");
                        if (interlockedType != null && SymbolEqualityComparer.Default.Equals(invocationOp.TargetMethod.ContainingType, interlockedType))
                        {
                            goto NO_WARN;
                        }
                    }

                    if (!isCreationOp)
                    {
                        goto NO_WARN;
                    }
                }
            }

            // `is null` or `is not null` pattern
            {
                if (op.Parent is IIsPatternOperation isPatternOp)
                {
                    if (isPatternOp.Pattern is IConstantPatternOperation constantPattern)
                    {
                        if (constantPattern.Value.ConstantValue.HasValue && constantPattern.Value.ConstantValue.Value == null)
                        {
                            if (!isCreationOp)
                            {
                                goto NO_WARN;
                            }
                        }
                    }
                }
            }


            var memberRefOrInvokeOp = Core.UnwrapNullCoalesceOperation(op);

            // member reference!!
            // --> disposable.Property;
            // --> disposable?.Property;
            {
                if (memberRefOrInvokeOp is IMemberReferenceOperation memberRefOp)
                {
                    if (!isCreationOp)
                    {
                        if (memberRefOp.Type is INamedTypeSymbol named && !IsDisposable(context, named))
                        {
                            goto NO_WARN;
                        }
                        else
                        {
                            var parentOp = Core.UnwrapNullCoalesceOperation(memberRefOrInvokeOp.Parent);
                            if (parentOp != null)
                            {
                                if (parentOp is IMemberReferenceOperation or ILocalReferenceOperation)
                                {
                                    goto NO_WARN;
                                }
                                else
                                {
                                    // NOTE: need to check subsequent method chain
                                    //       --> ...Prop.ToString();
                                    //                   ^^^^^^^^^^
                                    memberRefOrInvokeOp = parentOp;
                                }
                            }
                        }
                    }
                }
            }

            // method receiver!!
            // --> disposable.Return();
            // --> disposable?.Return();
            {
                if (memberRefOrInvokeOp is IInvocationOperation invokeOp)
                {
                    if (!isCreationOp)
                    {
                        if (invokeOp.TargetMethod.ReturnType is INamedTypeSymbol named && !IsDisposable(context, named))
                        {
                            goto NO_WARN;
                        }
                        else
                        {
                            //Core.ReportDebugMessage(context.ReportDiagnostic, op);

                            // check original operation type to determine code path is redirect from member ref or not
                            if (op is IMemberReferenceOperation)
                            {
                                goto NO_WARN;
                            }
                        }
                    }
                }
            }


            // NOTE: in the following, cannot use I***Operation to analyze usage
            //       because unity doesn't allow using latest roslyn analyzer
            var syntax = op.Syntax;


            // NOTE: if switch arm expression found, move focus to parent expression
            //       > var x = value switch { ... };
            //                              ~~~~~~~ current focus
            //       > var x = value switch { ... };
            //                 ~~~~~~~~~~~~~~~~~~~~ moving to here
            {
                if (op.Parent is ISwitchExpressionArmOperation switchArmOp
                 && switchArmOp.Parent is ISwitchExpressionOperation switchOp
                )
                {
                    syntax = switchOp.Syntax;
                }
            }


            // NOTE: remove parenthesizes and null warning suppressor!!
            //       --> (((new Disposable()))) --> new Disposable()
            //       --> (new Disposable())! --> new Disposable()
            syntax = Core.UnwrapParenthesizeAndNullSuppressorNodes(syntax);


            // return statement?
            // --> Method() => new Disposable();
            // --> Method() { return new Disposable(); }
            {
                if (syntax.Parent is ArrowExpressionClauseSyntax or ReturnStatementSyntax)
                {
                    goto NO_WARN;
                }
            }


            // NOTE: IUsingOperation is not pointing to block-less using syntax --> using var x = ...
            if (syntax.Parent is EqualsValueClauseSyntax equalsStx)
            {
                // using statement w/o block scope?
                // --> using var x = new Disposable();
                // --> using(var x = new Disposable()) { ... }
                if (equalsStx.Parent is VariableDeclaratorSyntax declaratorStx
                 && declaratorStx.Parent is VariableDeclarationSyntax varDeclStx
                )
                {
                    var parStx = varDeclStx.Parent;
                    if (parStx is UsingStatementSyntax or MemberDeclarationSyntax)
                    {
                        goto NO_WARN;
                    }
                    else if (parStx is LocalDeclarationStatementSyntax localVarStx)
                    {
                        // DON'T check localVarStx variable type is disposable or not
                        // just check using keyword existence
                        if (localVarStx.UsingKeyword != default)
                        {
                            goto NO_WARN;
                        }

                        if (IsSuppressedByComment(localVarStx))
                        {
                            goto NO_WARN;
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

                                    // reporting detailed diagnostic instead of generic one.
                                    context.ReportDiagnostic(Diagnostic.Create(
                                        Rule_NotAllCodePathsReturn, localVarDeclaratorStx.GetLocation(), localVarDeclaratorStx.Identifier));
                                }

                                // then, just go to NO_WARN to avoid additionally reporting SMA0040.
                                goto NO_WARN;
                            }
                        }
                    }
                }
            }
            else
            {
                // NOTE: ignore field/property assignment even if field/property type is disposable
                //       --> Field = new Disposable();
                //       --> Property = new Disposable();
                if (syntax.Parent is AssignmentExpressionSyntax assignStx)
                {
                    var leftStx = assignStx.Left;

                    // Discarding?
                    if (leftStx.IsKind(SyntaxKind.DiscardDesignation))
                    {
                        // Won't allow silent discard
                        if (IsSuppressedByComment(assignStx))
                        {
                            goto NO_WARN;
                        }
                    }
                    else
                    {
                        var model = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
                        var leftSymbol = model.GetSymbolInfo(leftStx).Symbol;

                        // left hand is indexer?
                        if (leftStx is ElementAccessExpressionSyntax elementAccessStx)
                        {
                            // array[0] returns null, list[0] returns non-null
                            var nonArraySymbol = model.GetSymbolInfo(elementAccessStx.Expression).Symbol;
                            if (nonArraySymbol != null)
                            {
                                leftSymbol = nonArraySymbol;
                            }
                        }

                        // ignore field/property
                        if (leftSymbol != null && (leftSymbol.Kind is SymbolKind.Field or SymbolKind.Property))
                        {
                            // don't allow cast and forget
                            // NG --> m_objectField = (new Disposable()) as object;
                            if (!isCreationOp
                            || op.Parent is not IConversionOperation castOp
                            || (castOp.Type is INamedTypeSymbol named && IsDisposable(context, named))
                            )
                            {
                                goto NO_WARN;
                            }
                        }
                    }
                }
                // --> if (disposable == ...)
                // --> while (disposable == ...)
                else if (op.Parent is IBinaryOperation)
                {
                    // don't allow creation operation pass the warning
                    if (!isCreationOp)
                    {
                        goto NO_WARN;
                    }
                }
            }


            // !! REPORT !!
            context.ReportDiagnostic(Diagnostic.Create(
                Rule_MissingUsing, syntax.GetLocation(), disposableSymbol.Name));

            //Core.ReportDebugMessage(context.ReportDiagnostic,
            //    "WARN",
            //    op.Syntax.GetLocation(),
            //    "target: " + op.Kind,
            //    "parent: " + op.Parent.Kind,
            //    "symbol: " + disposableSymbol.Name
            //    );

            return;

        NO_WARN:
            //Core.ReportDebugMessage(context.ReportDiagnostic,
            //    "NO WARN",
            //    op.Syntax.GetLocation(),
            //    "target: " + op.Kind,
            //    "parent: " + op.Parent.Kind,
            //    "symbol: " + disposableSymbol.Name
            //    );

            return;
        }

        private static bool IsLocalVariableReturned(OperationAnalysisContext context, VariableDeclaratorSyntax variableDeclarator, out bool inAllCodePaths)
        {
            inAllCodePaths = false;

            var enclosingMember = variableDeclarator.Ancestors().FirstOrDefault(x => x is MethodDeclarationSyntax or AccessorDeclarationSyntax);
            if (enclosingMember == null) return false;

            var semanticModel = context.Operation.SemanticModel;
            if (semanticModel == null) return false;

            var declaredSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
            if (declaredSymbol == null) return false;

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
                if (body.DescendantNodes().Any(x => x is ThrowStatementSyntax or ThrowExpressionSyntax))
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
                var handledPaths = 0;

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
