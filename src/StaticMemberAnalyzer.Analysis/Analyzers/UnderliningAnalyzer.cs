// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

#if STMG_ENABLE_UNDERLINING_ANALYZER

#define STMG_USE_ATTRIBUTE_CACHE
#define STMG_USE_DESCRIPTION_CACHE

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;

#pragma warning disable RS1001 // Missing diagnostic analyzer attribute.

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnderliningAnalyzer : DiagnosticAnalyzer
    {
        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_SymbolDesc = "SMA9000";
        private static readonly DiagnosticDescriptor Rule_SymbolDesc = new(
            RuleId_SymbolDesc,
            new LocalizableResourceString(nameof(Resources.SMA9000_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9000_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9000_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_LocalVarDesc = "SMA9001";
        private static readonly DiagnosticDescriptor Rule_LocalVarDesc = new(
            RuleId_LocalVarDesc,
            new LocalizableResourceString(nameof(Resources.SMA9001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9001_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_ParameterDesc = "SMA9002";
        private static readonly DiagnosticDescriptor Rule_ParameterDesc = new(
            RuleId_ParameterDesc,
            new LocalizableResourceString(nameof(Resources.SMA9002_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9002_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9002_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_DeclarationDesc = "SMA9010";
        private static readonly DiagnosticDescriptor Rule_DeclarationDesc = new(
            RuleId_DeclarationDesc,
            new LocalizableResourceString(nameof(Resources.SMA9010_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9010_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9010_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_DesignatedTypeDesc = "SMA9015";
        private static readonly DiagnosticDescriptor Rule_DesignatedTypeDesc = new(
            RuleId_DesignatedTypeDesc,
            new LocalizableResourceString(nameof(Resources.SMA9015_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9015_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9015_Description), Resources.ResourceManager, typeof(Resources)));


        // line annotators
        public const string RuleId_LineHeadDesc = "SMA9020";
        private static readonly DiagnosticDescriptor Rule_LineHeadDesc = new(
            RuleId_LineHeadDesc,
            new LocalizableResourceString(nameof(Resources.SMA9020_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9020_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9020_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_LineLeadingDesc = "SMA9021";
        private static readonly DiagnosticDescriptor Rule_LineLeadingDesc = new(
            RuleId_LineLeadingDesc,
            new LocalizableResourceString(nameof(Resources.SMA9021_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9021_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9021_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_LineFillDesc = "SMA9022";
        private static readonly DiagnosticDescriptor Rule_LineFillDesc = new(
            RuleId_LineFillDesc,
            new LocalizableResourceString(nameof(Resources.SMA9022_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9022_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9022_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_LineEndDesc = "SMA9023";
        private static readonly DiagnosticDescriptor Rule_LineEndDesc = new(
            RuleId_LineEndDesc,
            new LocalizableResourceString(nameof(Resources.SMA9023_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9023_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9023_Description), Resources.ResourceManager, typeof(Resources)));


        //warning!!
        public const string RuleId_WarningDesc = "SMA9100";
        private static readonly DiagnosticDescriptor Rule_WarningDesc = new(
            RuleId_WarningDesc,
            new LocalizableResourceString(nameof(Resources.SMA9100_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA9100_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA9100_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_SymbolDesc,
            Rule_LocalVarDesc,
            Rule_ParameterDesc,
            Rule_DeclarationDesc,

            Rule_DesignatedTypeDesc,

            Rule_LineHeadDesc,
            Rule_LineLeadingDesc,
            Rule_LineFillDesc,
            Rule_LineEndDesc,

            Rule_WarningDesc
            );


        readonly static ImmutableArray<SyntaxKind> cache_descriptionTargetSyntaxes = ImmutableArray.Create(
            SyntaxKind.IdentifierName,

            //// NOTE: ImplicitObjectCreationExpression (`new()`) is not supported. use Operation instead.
            //SyntaxKind.ObjectCreationExpression,

            // class C : List<X>, INonGeneric, IList<X>
            //           ^^^^                  ^^^^^
            // for generic types in baseList, identifierName syntax doesn't contain these syntaxes
            SyntaxKind.SimpleBaseType,

            // ctor(...) : base(...) or this(...)
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ThisConstructorInitializer,

            // required for lambda parameter
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression
            );

        readonly static ImmutableArray<SymbolKind> cache_descriptionTargetSymbols = ImmutableArray.Create(
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.ArrayType,
            SymbolKind.Event,
            SymbolKind.Field,
            SymbolKind.Property,
            SymbolKind.Parameter,
            SymbolKind.TypeParameter
            );

        readonly static ImmutableArray<OperationKind> cache_descriptionTargetOperations = ImmutableArray.Create(
                //constructor
                OperationKind.ObjectCreation,
                OperationKind.AnonymousObjectCreation
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            context.RegisterSymbolAction(DrawUnderlineOnSymbols, cache_descriptionTargetSymbols);

            context.RegisterSyntaxNodeAction(DrawUnderlineOnSyntaxNodes, cache_descriptionTargetSyntaxes);

            context.RegisterOperationAction(DrawUnderlineOnOperators, cache_descriptionTargetOperations);

            context.RegisterCompilationStartAction(InitializeAndRegisterCallbacks);
        }


        /*  register  ================================================================ */

        [ThreadStatic, DescriptionAttribute] static Location[]? ts_singleLocationArray;
        [ThreadStatic, DescriptionAttribute] static Dictionary<ISymbol, AttributeSyntax?>? ts_symbolToDescription;
        [ThreadStatic, DescriptionAttribute] static Dictionary</*AttributeSyntax*/SyntaxNode, string?>? ts_descAttrToMessage;

        private static void InitializeAndRegisterCallbacks(CompilationStartAnalysisContext context)
        {
            /* =      clear cache      = */

#pragma warning disable RS1012  // better place to clear cache
            context.RegisterCodeBlockStartAction<SyntaxKind>(ctx =>
            {
                var descAttrToMessage = ts_descAttrToMessage;
                if (descAttrToMessage == null)
                    return;

                // NOTE: assume that contained attribute syntax is about to update
                foreach (var attr in ctx.CodeBlock.DescendantNodesAndSelf().OfType<AttributeSyntax>())
                {
                    if (descAttrToMessage.ContainsKey(attr))
                        descAttrToMessage.Remove(attr);
                }
            });
#pragma warning restore RS1012

            // clear every 16 iterations
            int iter = 0;
            context.RegisterCompilationEndAction(ctx =>
            {
                if (iter++ < 16)
                    return;

                iter = 0;

                var symbolToDescription = ts_symbolToDescription;
                if (symbolToDescription == null)
                    return;

                symbolToDescription.Clear();
            });
        }


        /*  entry  ================================================================ */

        //symbol
        private static void DrawUnderlineOnSymbols(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            if (symbol.IsImplicitlyDeclared)
                return;


            var compilation = context.Compilation;
            var token = context.CancellationToken;
            Action<Diagnostic> reportAction = context.ReportDiagnostic;


            //sp:CategoryAttribute
            if (symbol.Kind is SymbolKind.NamedType or SymbolKind.Method)
            {
                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax();

                    if (syntax is BaseTypeDeclarationSyntax typeDecl)
                    {
                        DrawCategoryAnnotation(compilation, symbol, typeDecl.Identifier.GetLocation(), reportAction, token);
                    }

                    //constructor?
                    else if (syntax is ConstructorDeclarationSyntax ctorDecl)
                    {
                        var ctorType = symbol.ContainingType;  /* containingType!! */
                        if (ctorType != null)
                        {
                            DrawCategoryAnnotation(compilation, ctorType, ctorDecl.Identifier.GetLocation(), reportAction, token);

                            // : base()?
                            var baseCtor = ctorDecl.Initializer.ThisOrBaseKeyword;
                            if (baseCtor.IsKind(SyntaxKind.BaseKeyword))
                            {
                                var baseSymbol = ctorType.BaseType;
                                if (baseSymbol != null)
                                    DrawCategoryAnnotation(compilation, baseSymbol, baseCtor.GetLocation(), reportAction, token);
                            }
                        }
                    }
                }
            }


            var symbolToDescription = (ts_symbolToDescription ??= new());
            var descAttrToMessage = (ts_descAttrToMessage ??= new());

            Underlining(symbol.Locations, symbol, Rule_DeclarationDesc,
                reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token, 0);


            // generic type/method
            switch (symbol)
            {
                case INamedTypeSymbol type:
                    foreach (var tp in type.TypeParameters)
                    {
                        Underlining(tp.Locations, tp, Rule_DeclarationDesc,
                            reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token, 0);
                    }
                    break;
                case IMethodSymbol method:
                    foreach (var tp in method.TypeParameters)
                    {
                        Underlining(tp.Locations, tp, Rule_DeclarationDesc,
                            reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token, 0);
                    }
                    break;
            }
        }


        //syntax
        private static void DrawUnderlineOnSyntaxNodes(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node;
            var tree = syntax.SyntaxTree;
            var compilation = context.Compilation;


            // NOTE: don't use cached one!
            //var filePathToModel = (ts_filePathToModel ??= new());
            //if (!filePathToModel.TryGetValue(tree.FilePath, out var model))
            //{
            var model = compilation.GetSemanticModel(tree);
            //    filePathToModel[tree.FilePath] = model;
            //}


            var symbolToDescription = (ts_symbolToDescription ??= new());
            Action<Diagnostic> reportAction = context.ReportDiagnostic;
            var token = context.CancellationToken;

            var descAttrToMessage = (ts_descAttrToMessage ??= new());

            /* =      lambda!!      = */
            if (syntax is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
            {
                ReportLambda(model, syntax,
                    reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token);

                return;
            }


            var rule = Rule_SymbolDesc;

            var singleLocation = (ts_singleLocationArray ??= new Location[1]);
            singleLocation[0] = syntax.GetLocation();

            var symbolCandidate = model.GetSymbolInfo(syntax, token);

        RERUN_SIMPLE_BASE_TYPE:
            var symbol = symbolCandidate.Symbol;
            if (symbol == null)
            {
                //ummmm.........
                if (syntax is SimpleBaseTypeSyntax baseType)
                {
                    symbolCandidate = model.GetSymbolInfo(baseType.Type, token);
                    goto RERUN_SIMPLE_BASE_TYPE;
                }

                symbol = symbolCandidate.CandidateSymbols.FirstOrDefault();
                if (symbol == null)
                    return;
            }
            else if (symbol is ILocalSymbol localSymbol)
            {
                symbol = localSymbol.Type;
                rule = Rule_LocalVarDesc;
            }
            else if (symbol is IParameterSymbol paramSymbol)
            {
                // NOTE: draw underline on methodParameter --> void Method([DescriptionAttribute] T methodParameter);
                Underlining(singleLocation, symbol, rule,
                    reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token,
                    0);

                // then, draw underline based on variable type
                symbol = paramSymbol.Type;
                rule = Rule_ParameterDesc;
            }


            if (symbol.IsImplicitlyDeclared)
                return;

            Underlining(singleLocation, symbol, rule,
                reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token,
                0);


            // draw line only when span length matches
            // - no underline --> var, localVar, etc
            // - underline --> TargetSymbolName (exact match)
            if ((symbol.Locations[0].SourceSpan.Length == singleLocation[0].SourceSpan.Length) && (symbol.Name == syntax.ToString()))
            {
                DrawCategoryAnnotation(compilation, symbol, singleLocation[0], context.ReportDiagnostic, token);
            }
        }


        private static void ReportLambda(SemanticModel model,
                                         SyntaxNode syntax,
                                         Action<Diagnostic> reportAction,
                                         IDictionary<ISymbol, AttributeSyntax?> symbolToDescription,
                                         // works only on small source code file --> IDictionary<string, SemanticModel> filePathToModel,
                                         IDictionary<SyntaxNode, string?> descAttrToMessage,
                                         Compilation compilation,
                                         CancellationToken token
            )
        {
            INamedTypeSymbol? lambdaType = null;

            // lambda parameter type is type-less. take actual type from variable declaration
            var par = syntax.Parent;
            if (par is EqualsValueClauseSyntax)
            {
                par = par.Parent;
                if (par is VariableDeclaratorSyntax && par.Parent is VariableDeclarationSyntax varDecl)
                {
                    // TODO: use GetTypeInfo
                    lambdaType = model.GetSymbolInfo(varDecl.Type, token).Symbol as INamedTypeSymbol;
                }
            }
            else if (par is AssignmentExpressionSyntax assign)
            {
                // TODO: support delegate parameter types --> delegate ReturnType MyDelegate(ParamType param);
                if (model.GetSymbolInfo(assign.Left, token).Symbol is ILocalSymbol localVar)
                {
                    lambdaType = (localVar.Type as INamedTypeSymbol);//?.TypeArguments.FirstOrDefault();
                }
            }

            if (lambdaType == null)
            {
                return;
            }


            var typeArgs = lambdaType.TypeArguments;
            var typeArgsCount = typeArgs.Length;
            if (typeArgsCount == 0)
                return;

            var singleLocation = (ts_singleLocationArray ??= new Location[1]);

            switch (syntax)
            {
                case SimpleLambdaExpressionSyntax lambda:
                    if (typeArgsCount == 1)
                    {
                        singleLocation[0] = lambda.Parameter.GetLocation();
                        Underlining(singleLocation, typeArgs[0], Rule_ParameterDesc,
                            reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token,
                            0);
                    }
                    return;

                case ParenthesizedLambdaExpressionSyntax multi:
                    var parameters = multi.ParameterList.Parameters;
                    if (typeArgsCount == parameters.Count)
                    {
                        int pos = -1;
                        foreach (var param in parameters)
                        {
                            pos++;
                            singleLocation[0] = param.GetLocation();
                            Underlining(singleLocation, typeArgs[pos], Rule_ParameterDesc,
                                reportAction, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token,
                                0);
                        }
                    }
                    return;
            }


            reportAction.Invoke(Diagnostic.Create(Rule_ParameterDesc, syntax.GetLocation(),
                Core.SpanConcat("[SMA UNSUPPORTED]: ".AsSpan(), syntax.Kind().ToString().AsSpan()),
                "TypeArguments.Count: " + typeArgsCount
                ));
        }


        const string ATTR_CATEGORY = nameof(CategoryAttribute);

        private static void DrawCategoryAnnotation(Compilation compilation,
                                                   ISymbol symbol,
                                                   Location loc,
                                                   Action<Diagnostic> reportAction,
                                                   CancellationToken token
            )
        {
            string? description = null;

            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is not BaseTypeDeclarationSyntax typeDecl)
                    goto EXIT;

                foreach (var attrList in typeDecl.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        if (attr.Name.Span.Length == ATTR_CATEGORY.Length && attr.Name.ToString() == ATTR_CATEGORY)
                        {
                            // NOTE: parameter-less attribute constructor has null argument list
                            if (attr.ArgumentList != null
                            && (attr.ArgumentList.Arguments).Count == 1)
                            {
                                var attrArgs0 = attr.ArgumentList.Arguments[0];
                                var attrTree = attrArgs0.SyntaxTree;
                                var attrModel = compilation.GetSemanticModel(attrTree);

                                var attrValue = attrModel.GetConstantValue((attrArgs0.Expression as ExpressionSyntax), token);
                                if (attrValue.HasValue)
                                {
                                    description ??= attrValue.ToString();

                                    //identifierToken = memberDecl.Identifier;//(memberDecl as BaseTypeDeclarationSyntax)?.Identifier;
                                    //identifierToken ??= (memberDecl as MethodDeclarationSyntax)?.Identifier;
                                    //identifierToken ??= (memberDecl as PropertyDeclarationSyntax)?.Identifier;

                                    goto EXIT;
                                }
                            }
                        }
                    }
                }
            EXIT:
                ;
            }

            if (description != null)
            {
                //var lineSpan = loc.GetLineSpan();

                __DrawUnderlinePerChar(
                    reportAction,
                    loc.SourceTree,
                    loc.SourceSpan.Start,// - lineSpan.StartLinePosition.Character,
                    loc.SourceSpan.End,// + lineSpan.EndLinePosition.Character,
                    Rule_DesignatedTypeDesc,
                    GetMessageFormatArgs(symbol, description)
                    );
            }
        }


        //operator
        private static void DrawUnderlineOnOperators(OperationAnalysisContext context)
        {
            var op = context.Operation;
            if (op.IsImplicit)
                return;

            Action<Diagnostic> reportAction = context.ReportDiagnostic;
            var compilation = context.Compilation;
            var token = context.CancellationToken;

            var symbolToDescription = (ts_symbolToDescription ??= new());
            var descAttrToMessage = (ts_descAttrToMessage ??= new());

            var singleLocation = (ts_singleLocationArray ??= new Location[1]);


            INamedTypeSymbol? foundSymbol = null;
            SyntaxNode? foundSyntax = null;
            ImmutableArray<IArgumentOperation> foundArgs;
            if (op is IObjectCreationOperation ctorOp)
            {
                if (ctorOp.Type is INamedTypeSymbol namedSymbol)
                {
                    foundSymbol = namedSymbol;
                    foundSyntax = ctorOp.Syntax;
                    foundArgs = ctorOp.Arguments;
                }
            }
            else if (op is IAnonymousObjectCreationOperation anonyOp)
            {
                if (anonyOp.Type is INamedTypeSymbol namedSymbol)
                {
                    foundSymbol = namedSymbol;
                    foundSyntax = anonyOp.Syntax;
                    foundArgs = anonyOp.Children.OfType<IArgumentOperation>().ToImmutableArray();
                }
            }
            //else if (op is IMemberReferenceOperation memberOp)
            //{
            //    // NOTE: Class.Member consists of FieldReference and ClassReference
            //    var par = memberOp.Parent;
            //    for (; ; )
            //    {
            //        if (par is not IMemberReferenceOperation parOp)
            //            break;
            //        memberOp = parOp;
            //        par = par.Parent;
            //    }
            //    member = memberOp.Member;
            //}
            //else if (op is ITypeOfOperation typeofOp)
            //{
            //    member = typeofOp.TypeOperand;
            //}


            if (foundSymbol != null && !foundSymbol.IsImplicitlyDeclared
             && foundSyntax != null
            )
            {
                //var ctorOpSyntax = ctorOp.Syntax;
                singleLocation[0] = foundSyntax.GetLocation();

                // NOTE: when optional parameter is omitted, Operation will return count INCLUDING omitted ones.
                //       ex) ctor(int value, int other = 0)
                //           _ = new(310);  // <-- this operation will return Length == 2, not 1.
                var argsCount = foundArgs.Length;
                foreach (var ctor in foundSymbol.Constructors)
                {
                    if (!ctor.IsStatic)
                    {
                        var ctorParams = ctor.Parameters;

                        // count will match even if optional parameters are omitted
                        if (ctorParams.Length != argsCount)
                            continue;

                        for (var i = 0; i < argsCount; i++)
                        {
                            if (!SymbolEqualityComparer.Default.Equals(foundArgs[i].Parameter.Type, ctorParams[i].Type))
                                goto NEXT;
                        }
                    }

                    Underlining(singleLocation, ctor, Rule_SymbolDesc,
                        reportAction, symbolToDescription, descAttrToMessage, compilation, token, 0);

                NEXT:
                    ;
                }
            }
        }


        /*  helper  ================================================================ */

        private static void Underlining<T>(T locations,
                                           ISymbol targetSymbol,
                                           DiagnosticDescriptor rule,
                                           Action<Diagnostic> reportAction,
                                           IDictionary<ISymbol, AttributeSyntax?> symbolToDescription,
                                           // works only on small source code file --> IDictionary<string, SemanticModel> filePathToModel,
                                           IDictionary<SyntaxNode, string?> descAttrToMessage,
                                           Compilation compilation,
                                           CancellationToken token,
                                           int dotTokenCount
            )
            where T : IEnumerable<Location>
        {
#if STMG_DEBUG_MESSAGE
            try
#endif
            {
                int depth = 0;
                string? description;
                AttributeSyntax? attr;

                do
                {
                    description = null;

                    if (symbolToDescription.TryGetValue(targetSymbol, out attr))
                    {
                        if (attr != null && !descAttrToMessage.TryGetValue(attr, out description))
                        {
                            description = GetDescriptionMessage(attr, targetSymbol, /*filePathToModel,*/ descAttrToMessage, compilation, token);
                        }
                    }
                    else
                    {
                        description = GetAndUpdateDescriptionCache(targetSymbol, symbolToDescription, /*filePathToModel,*/ descAttrToMessage, compilation, token);
                    }

                    if (description == null)
                    {
                        goto NEXT;
                    }


                    var args = GetMessageFormatArgs(targetSymbol, description);
                    foreach (var location in locations)
                    {
                        ReportDiagnosticPerChar(reportAction, location, rule, args);
                    }

                NEXT:
                    if (depth++ >= dotTokenCount)
                        break;

                    targetSymbol = targetSymbol.ContainingType;
                }
                while (targetSymbol != null);
            }

#if STMG_DEBUG_MESSAGE
            catch (Exception ex)
            {
                Core.ReportDebugMessage(reportAction,
                    "=== ERROR OCCURED ===",
                    ex.ToString(),
                    locations);
            }
#endif
        }


        private static object[] GetMessageFormatArgs(ISymbol targetSymbol, string description)
        {
            var assem = targetSymbol.ContainingAssembly?.Name;
            if (assem == null)
            {
                if (targetSymbol.ContainingNamespace != null)
                {
                    assem = string.Join(".", targetSymbol.ContainingNamespace.ConstituentNamespaces);
                }
                else
                {
                    assem = "UNKNOWN ASSEMBLY";
                }
            }
            //TODO
            const string PREFIX_SYMBOL = " in ";
            Span<char> span = stackalloc char[assem.Length + PREFIX_SYMBOL.Length];
            PREFIX_SYMBOL.AsSpan().CopyTo(span);
            assem.AsSpan().CopyTo(span.Slice(PREFIX_SYMBOL.Length));

            var targetName = targetSymbol.Name;
            Span<char> quoted = stackalloc char[targetName.Length + 2];
            quoted[0] = '\'';
            targetName.AsSpan().CopyTo(quoted.Slice(1));
            quoted[quoted.Length - 1] = '\'';

            var args = new object[] { Core.SpanConcat(quoted, span), description };
            return args;
        }


        // NOTE: don't collect "[Description]". collect only "[DescriptionAttribute]" to prevent
        //       underlining by original Description attribute usage.
        //       - [Description("Description for VS Visual Designer")] int NoUnderlineOnMe;
        //       - [DescriptionAttribute("Draw underline in VS source code editor")] int GetUnderline;
        const string ATTR_DESCRIPTION = nameof(DescriptionAttribute);

        // NOTE: to apply attribute message parameter changes quickly,
        //       don't cache description text string, instead cache symbol-to-AttributeSyntax mapping.
        private static string? GetAndUpdateDescriptionCache(ISymbol symbol,
                                                            IDictionary<ISymbol, AttributeSyntax?> symbolToDescription,
                                                            // works only on small source code file --> IDictionary<string, SemanticModel> filePathToModel,
                                                            IDictionary<SyntaxNode, string?> descAttrToMessage,
                                                            Compilation compilation,
                                                            CancellationToken token
            )
        {
            // NOTE: cache existence check is done by caller
            AttributeSyntax? attr = null;
            string? description;

            SyntaxNode declareSyntax;
            var declareSyntaxRefs = symbol.DeclaringSyntaxReferences;

            // NOTE: don't reuse symbol var, dictionary key must be supplied symbol, not base/overridden symbol
            var recursiveTarget = symbol;
            uint recursiveCount = 0;
            //TODO
            const uint MAX_TRAVERSE_DEPTH = 3;

        RESTART_FOREACH:

            //// .ctor cannot be referenced!!!
            //if (!recursiveTarget.CanBeReferencedByName)
            //{
            //    goto SKIP_FOREACH;
            //}

            if (recursiveTarget.ContainingNamespace?.ConstituentNamespaces.FirstOrDefault()?.Name == nameof(System))
            {
                goto NOT_FOUND;
            }

            // get attributes
            foreach (var declare in declareSyntaxRefs)
            {
                declareSyntax = declare.GetSyntax(token);

            RESTART_SWITCH:
                switch (declareSyntax)
                {
                    case MemberDeclarationSyntax memberDecl:
                        attr = memberDecl.AttributeLists.SelectMany(static x => x.Attributes).FirstOrDefault(static x =>
                        {
                            return x.Name.Span.Length == ATTR_DESCRIPTION.Length
                                && x.Name.ToString() == ATTR_DESCRIPTION;
                        });
                        break;

                    case AccessorDeclarationSyntax accessorDecl:
                        attr = accessorDecl.AttributeLists.SelectMany(static x => x.Attributes).FirstOrDefault(static x =>
                        {
                            return x.Name.Span.Length == ATTR_DESCRIPTION.Length
                                && x.Name.ToString() == ATTR_DESCRIPTION;
                        });
                        break;

                    case ParameterSyntax paramDecl:
                        attr = paramDecl.AttributeLists.SelectMany(static x => x.Attributes).FirstOrDefault(static x =>
                        {
                            return x.Name.Span.Length == ATTR_DESCRIPTION.Length
                                && x.Name.ToString() == ATTR_DESCRIPTION;
                        });
                        break;

                    case TypeParameterSyntax typeParam:
                        attr = typeParam.AttributeLists.SelectMany(static x => x.Attributes).FirstOrDefault(static x =>
                        {
                            return x.Name.Span.Length == ATTR_DESCRIPTION.Length
                                && x.Name.ToString() == ATTR_DESCRIPTION;
                        });
                        break;

                    case LocalFunctionStatementSyntax localFunc:
                        attr = localFunc.ChildNodes().OfType<AttributeListSyntax>().FirstOrDefault()?.Attributes.FirstOrDefault(static x =>
                        {
                            return x.Name.Span.Length == ATTR_DESCRIPTION.Length
                                && x.Name.ToString() == ATTR_DESCRIPTION;
                        });
                        break;


                    // label doesn't require underline
                    case LabeledStatementSyntax:
                        break;

                    // public int Property => 100;
                    //                        ^^^
                    // 100 get underline but not required
                    case ArrowExpressionClauseSyntax:
                        break;

                    // tuple should not get underline
                    case TupleTypeSyntax:
                    case TupleElementSyntax:
                        break;

                    // syntax used when code has error
                    // ex) root level method declaration --> void Test() { }
                    case CompilationUnitSyntax:
                        break;

                    // xml doc comment `cref="GenericType{T1, T2}"` can have this syntax
                    //                                    ^^  ^^
                    case IdentifierNameSyntax:
                        {
                            var par = declareSyntax.Parent;

                            //cref??
                            if (par is TypeArgumentListSyntax)
                            {
                                par = par.Parent;
                                if (par is not GenericNameSyntax)
                                    goto default;

                                par = par.Parent;
                                if (par is not CrefSyntax)
                                    goto default;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                        break;

                    // var anonymous = new { member = something };
                    // ^^^                   ^^^^^^
                    // creationExpr          memberDeclarator
                    case AnonymousObjectCreationExpressionSyntax:
                    case AnonymousObjectMemberDeclaratorSyntax:
                        break;


                    // NOTE: check at last!!!!
                    case VariableDeclaratorSyntax:
                        {
                            var par = declareSyntax.Parent;
                            if (par is VariableDeclarationSyntax)
                            {
                                if (par.Parent is MemberDeclarationSyntax)//FieldDeclarationSyntax or EventFieldDeclarationSyntax)
                                {
                                    declareSyntax = par.Parent;
                                    goto RESTART_SWITCH;  //tricky!!
                                }
                            }
                        }
                        break;


                    //unsupported
                    default:
                        return Core.SpanConcat("[SMA UNSUPPORTED]: ".AsSpan(), (declareSyntax?.Kind().ToString() ?? string.Empty).AsSpan());
                }


                if (attr == null)
                {
                    continue;
                }


#if STMG_USE_ATTRIBUTE_CACHE
                // NOTE: existence check is done by caller
                symbolToDescription[symbol] = attr;
#endif

                if (descAttrToMessage.TryGetValue(attr, out description))
                    return description;

                return GetDescriptionMessage(attr, symbol, /*filePathToModel,*/ descAttrToMessage, compilation, token);
            }


            // NOTE: when searching base/overridden symbol perfectly, it makes analyzing EXTREMELY slow!!
            if (recursiveCount++ < MAX_TRAVERSE_DEPTH)
            {
                if (symbol.IsOverride)
                {
                    switch (symbol)
                    {
                        case IMethodSymbol M when !M.OverriddenMethod.IsImplicitlyDeclared:
                            {
                                declareSyntaxRefs = M.OverriddenMethod.DeclaringSyntaxReferences;
                            }
                            goto RESTART_FOREACH;

                        case IPropertySymbol P when !P.OverriddenProperty.IsImplicitlyDeclared:
                            {
                                declareSyntaxRefs = P.OverriddenProperty.DeclaringSyntaxReferences;
                            }
                            goto RESTART_FOREACH;

                        case IEventSymbol E when !E.OverriddenEvent.IsImplicitlyDeclared:
                            {
                                declareSyntaxRefs = E.OverriddenEvent.DeclaringSyntaxReferences;
                            }
                            goto RESTART_FOREACH;
                    }
                }

                // base type
                else
                {
                    if (recursiveTarget is INamedTypeSymbol T && T.BaseType is INamedTypeSymbol B && !B.IsImplicitlyDeclared)
                    {
                        recursiveTarget = B;
                        declareSyntaxRefs = B.DeclaringSyntaxReferences;
                        goto RESTART_FOREACH;
                    }
                }
            }


        NOT_FOUND:

#if STMG_USE_ATTRIBUTE_CACHE
            // NOTE: existence check is done by caller
            symbolToDescription[symbol] = null;
#endif

            return null;
        }


        private static string? GetDescriptionMessage(AttributeSyntax attr,
                                                     ISymbol symbol,
                                                     // works only on small source code file --> IDictionary<string, SemanticModel> filePathToModel,
                                                     IDictionary<SyntaxNode, string?> descAttrToMessage,
                                                     Compilation compilation,
                                                     CancellationToken token
            )
        {
            // NOTE: cache existence check is done by caller
            string? description = null;

            SeparatedSyntaxList<AttributeArgumentSyntax> attrArgs;
            SyntaxTree attrTree;
            Optional<object> attrValue;

            // NOTE: parameter-less attribute constructor has null argument list
            if (attr.ArgumentList != null
            && (attrArgs = attr.ArgumentList.Arguments).Count == 1)
            {
                attrTree = attrArgs[0].SyntaxTree;
                // NOTE: DON'T USE CACHE!!!!
                //       THROWS EXCEPTION!!!!!!
                /*
                                if (!filePathToModel.TryGetValue(attrTree.FilePath, out var model))
                                {
                                    model = compilation.GetSemanticModel(attrTree);
                                    filePathToModel.Add(attrTree.FilePath, model);
                                }
                */
                var model = compilation.GetSemanticModel(attrTree);

                attrValue = model.GetConstantValue((attrArgs[0].Expression as ExpressionSyntax), token);
                if (attrValue.HasValue)
                {
                    description = attrValue.ToString();
                }
                else
                {
                    // NOTE: when constant value cannot be retrieved, assume that user is typing source code in IDE
                    //       so that don't try to update cache at this moment.
                    return null;
                }
            }


            if (string.IsNullOrWhiteSpace(description))
            {
                //var name = symbol.Name;

                //Span<char> span = stackalloc char[name.Length + 2];
                //span[0] = '\'';
                //name.AsSpan().CopyTo(span.Slice(1));
                //span[span.Length - 1] = '\'';

                description = "Take care to use (no details provided)";  //SpanConcat(span, " has Description attribute w/o args".AsSpan());
            }


#if STMG_USE_DESCRIPTION_CACHE
            // NOTE: existence checked is done in caller!!
            descAttrToMessage[attr] = description;
#endif

            return description;
        }


        private static void ReportDiagnosticPerChar(Action<Diagnostic> reportAction,
                                                    Location location,
                                                    DiagnosticDescriptor rule,
                                                    object[] messageArgs
            )
        {
            //warning??
            if (messageArgs[1] is string s && s.StartsWith("!", StringComparison.Ordinal))
            {
                reportAction.Invoke(Diagnostic.Create(Rule_WarningDesc, location, messageArgs));
                return;
            }


            int locStart = location.SourceSpan.Start;
            int locEnd = location.SourceSpan.End;
            var tree = location.SourceTree;

            __DrawUnderlinePerChar(reportAction, tree, locStart, locEnd, rule, messageArgs);


            // add line annotation
            //TODO
            const int length = 2;
            const int offset = length + 1;

            var spanInLine = location.GetLineSpan();
            if (spanInLine.IsValid)
            {
                var treeText = tree.GetText();

                int startInLine = spanInLine.StartLinePosition.Character;
                if (startInLine > offset)
                {
                    int start = locStart - startInLine;

                    //head!!
                    int linehead = start + 1;
                    reportAction.Invoke(Diagnostic.Create(
                        Rule_LineHeadDesc, tree.GetLocation(new(linehead, length)), messageArgs));

                    // find non-whitespace
                    var lineheadText = treeText.GetSubText(start);
                    for (int i = 0; i < startInLine; i++)
                    {
                        if (!char.IsWhiteSpace(lineheadText[i]))
                            break;
                        start++;
                    }

                    //lead!!
                    start -= offset;
                    if (start > linehead)
                    {
                        if (start < locStart)
                        {
                            reportAction.Invoke(Diagnostic.Create(
                                Rule_LineLeadingDesc, tree.GetLocation(new(start, length)), messageArgs));
                        }
                    }
                }

                //lineEnd!!
                int lineEnd = locEnd;
                var lineEndText = treeText.GetSubText(lineEnd);
                for (int i = 0; i < lineEndText.Length; i++)
                {
                    if (lineEndText[i] is '\n' or '\r')
                        break;
                    lineEnd++;
                }


                reportAction.Invoke(Diagnostic.Create(
                    Rule_LineEndDesc, tree.GetLocation(new(lineEnd, 0)), messageArgs));  // TextSpan end is inclusive. 0 is allowed
            }
        }


        /// <summary>
        /// When report `Info` severity diagnostic on full span, VisualStudio draws underline only on first a few chars, not on whole span.
        /// To workaround, split report into multiple reports for no overlapping/connected span.
        /// </summary>
        private static void __DrawUnderlinePerChar(Action<Diagnostic> reportAction,
                                                   SyntaxTree tree,
                                                   int start,
                                                   int end,
                                                   DiagnosticDescriptor rule,
                                                   object[] messageArgs
            )
        {
            int lineLength = 2;
            int increment = lineLength + 1;

            Location loc;
            int remain;
            for (int i = start; i < end; i += increment)  // NOTE: connected underline cannot be drawn... need a gap
            {
                remain = end - i;
                if (remain == increment)
                {
                    if (increment > 2)
                    {
                        lineLength -= 1;
                        increment -= 1;
                    }
                    else
                    {
                        lineLength = 2;
                    }
                }

                loc = tree.GetLocation(new(i, Math.Min(lineLength, remain)));
                reportAction.Invoke(Diagnostic.Create(rule, loc, messageArgs));
            }
        }

    }
}

#endif
