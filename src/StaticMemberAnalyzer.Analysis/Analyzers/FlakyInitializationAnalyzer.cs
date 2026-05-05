// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FlakyInitializationAnalyzer : DiagnosticAnalyzer
    {
        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_WrongInit = "SMA0001";
        private static readonly DiagnosticDescriptor Rule_WrongInit = new(
            RuleId_WrongInit,
            new LocalizableResourceString(nameof(Resources.SMA0001_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0001_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_CrossRef = "SMA0002";
        private static readonly DiagnosticDescriptor Rule_CrossRef = new(
            RuleId_CrossRef,
            new LocalizableResourceString(nameof(Resources.SMA0002_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0002_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0002_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_AnotherFile = "SMA0003";
        private static readonly DiagnosticDescriptor Rule_AnotherFile = new(
            RuleId_AnotherFile,
            new LocalizableResourceString(nameof(Resources.SMA0003_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0003_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0003_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_LateDeclare = "SMA0004";
        private static readonly DiagnosticDescriptor Rule_LateDeclare = new(
            RuleId_LateDeclare,
            new LocalizableResourceString(nameof(Resources.SMA0004_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0004_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0004_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_WrongInit,
            Rule_CrossRef,
            Rule_AnotherFile,
            Rule_LateDeclare
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            // called per source document
            context.RegisterSemanticModelAction(AnalyzeStaticFields);
        }


        /*  impl  ================================================================ */

        const int DEFAULT_LIST_CAPACITY = 4;

#pragma warning disable RS1008  // OK: always clear on method startup
        [ThreadStatic, DescriptionAttribute] static HashSet<ISymbol>? ts_declaringOrderCheckSymbolSet;
        [ThreadStatic, DescriptionAttribute] static HashSet<ISymbol>? ts_declaredSymbolSet;
        [ThreadStatic, DescriptionAttribute] static List<ISymbol>? ts_declaredWithInitializerSymbolList;
        [ThreadStatic, DescriptionAttribute] static List<IMemberReferenceOperation>? ts_initializerRefOperatorList;
        [ThreadStatic, DescriptionAttribute] static List<IMemberReferenceOperation>? ts_crossRefOperatorList;
        [ThreadStatic, DescriptionAttribute] static HashSet<IMemberReferenceOperation>? ts_crossRefReportedSet;
        [ThreadStatic, DescriptionAttribute] static List<FieldDeclarationSyntax>? ts_crossFDSyntaxList;
        [ThreadStatic, DescriptionAttribute] static Dictionary<string, SemanticModel>? ts_filePathToModel;
#pragma warning restore RS1008


        private static void AnalyzeStaticFields(SemanticModelAnalysisContext context)
        {
            // *MUST* set equality comparer for HashSet<ISymbol>
            var declaringOrderCheckSymbolSet = (ts_declaringOrderCheckSymbolSet ??= new(SymbolEqualityComparer.Default));
            var declaredSymbolSet = (ts_declaredSymbolSet ??= new(SymbolEqualityComparer.Default));
            var declaredWithInitializerSymbolList = (ts_declaredWithInitializerSymbolList ??= new(capacity: DEFAULT_LIST_CAPACITY));
            var initializerRefOperatorList = (ts_initializerRefOperatorList ??= new(capacity: DEFAULT_LIST_CAPACITY));
            var crossRefOperatorList = (ts_crossRefOperatorList ??= new(capacity: DEFAULT_LIST_CAPACITY));
            var crossRefReportedSet = (ts_crossRefReportedSet ??= new());
            var crossFDSyntaxList = (ts_crossFDSyntaxList ??= new(capacity: DEFAULT_LIST_CAPACITY));

            declaringOrderCheckSymbolSet.Clear();
            declaredSymbolSet.Clear();
            declaredWithInitializerSymbolList.Clear();
            initializerRefOperatorList.Clear();
            crossRefOperatorList.Clear();
            crossRefReportedSet.Clear();
            crossFDSyntaxList.Clear();

            // it seems that model cannot be reusable. all reports are gone after opening other file in VisualStudio
            var filePathToModel = (ts_filePathToModel ??= new());
            filePathToModel.Clear();
            filePathToModel[context.SemanticModel.SyntaxTree.FilePath] = context.SemanticModel;

            var token = context.CancellationToken;

            var root = context.SemanticModel.SyntaxTree.GetRoot(token);
            foreach (var memberDeclStx in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (memberDeclStx is not FieldDeclarationSyntax and not PropertyDeclarationSyntax)
                    continue;

                ClearAndCollectFieldInfo(
                    memberDeclStx, context.SemanticModel, declaredSymbolSet, declaredWithInitializerSymbolList, initializerRefOperatorList, token);

                for (int i = 0; i < initializerRefOperatorList.Count; i++)
                {
                    var refOp = initializerRefOperatorList[i];

                    /*  declaration order  ================================================================ */

                    var refOpMemberContainingTypeDeclares = refOp.Member.ContainingType.DeclaringSyntaxReferences;

                    // reading field in same type
                    if (SymbolEqualityComparer.Default.Equals(refOp.Member.ContainingType, declaredWithInitializerSymbolList[i].ContainingType))
                    {
                        bool isPartial = false;
                        if (refOpMemberContainingTypeDeclares.Length > 1)
                        {
                            var a = refOp.Member.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath;
                            var b = declaredWithInitializerSymbolList[i].DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath;

                            if (a != null && b != null && a != b)
                            {
                                isPartial = true;

                                context.ReportDiagnostic(
                                    Diagnostic.Create(Rule_AnotherFile, refOp.Syntax.GetLocation(),
                                    refOp.Member.Name));
                            }
                        }

                        if (!isPartial)
                        {
                            if (!declaringOrderCheckSymbolSet.Contains(refOp.Member))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(Rule_WrongInit, refOp.Syntax.GetLocation(),
                                    refOp.Member.Name));

                                foreach (var loc in refOp.Member.Locations)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(Rule_LateDeclare, loc, declaredWithInitializerSymbolList[i].Name));
                                }
                            }
                        }
                    }

                    /*  cross referencing  ================================================================ */

                    else
                    {
                        if (crossRefReportedSet.Contains(refOp))
                            continue;

                        crossFDSyntaxList.Clear();
                        foreach (var dsr in refOpMemberContainingTypeDeclares)
                        {
                            var s = dsr.GetSyntax(token);
                            crossFDSyntaxList.AddRange(s.DescendantNodes().OfType<FieldDeclarationSyntax>());
                        }

                        foreach (var crossField in crossFDSyntaxList)
                        {
                            if (!filePathToModel.TryGetValue(crossField.SyntaxTree.FilePath, out var crossModel))
                            {
                                crossModel = context.SemanticModel.Compilation.GetSemanticModel(crossField.SyntaxTree);
                                filePathToModel[crossField.SyntaxTree.FilePath] = crossModel;
                            }

                            ClearAndCollectFieldInfo(crossField, crossModel, null, null, crossRefOperatorList, token);

                            for (int c = 0; c < crossRefOperatorList.Count; c++)
                            {
                                if (!SymbolEqualityComparer.Default.Equals(crossRefOperatorList[c].Member.ContainingType, declaredWithInitializerSymbolList[i].ContainingType))
                                    continue;

                                context.ReportDiagnostic(
                                    Diagnostic.Create(Rule_CrossRef, refOp.Syntax.GetLocation(),
                                    refOp.Member.ContainingType.Name, declaredWithInitializerSymbolList[i].ContainingType.Name));

                                crossRefReportedSet.Add(refOp);

                                break;
                            }
                        }
                    }
                }

                declaringOrderCheckSymbolSet.UnionWith(declaredSymbolSet);
            }
        }


        private static void ClearAndCollectFieldInfo(MemberDeclarationSyntax memberStx,
                                                     SemanticModel model,
                                                     HashSet<ISymbol>? declaredSymbolSet,
                                                     List<ISymbol>? declaredWithInitializerSymbolList,
                                                     List<IMemberReferenceOperation> initializerRefOperatorList,
                                                     CancellationToken token
            )
        {
            declaredWithInitializerSymbolList?.Clear();
            initializerRefOperatorList.Clear();

            // GetOperation must run on EqualsValueClauseSyntax, otherwise returns null
            foreach (var equalsStx in memberStx.DescendantNodes().OfType<EqualsValueClauseSyntax>())
            {
                if (model.GetOperation(equalsStx, token) is not ISymbolInitializerOperation { IsImplicit: false } initOp)
                    continue;

                //lambda??
                if (initOp.Children.FirstOrDefault() is IDelegateCreationOperation)
                    continue;

                ISymbol? declaredSymbol = null;
                if (initOp is IFieldInitializerOperation fieldInitOp)
                {
                    var fieldSymbol = fieldInitOp.InitializedFields.FirstOrDefault();  // check first one --> static int FIRST, SECOND = 10;
                    if (fieldSymbol == null || fieldSymbol.IsConst || !fieldSymbol.IsStatic || fieldSymbol.IsImplicitlyDeclared)
                        continue;

                    declaredSymbolSet?.UnionWith(fieldInitOp.InitializedFields);  // for declaring order check
                    declaredSymbol = fieldSymbol;
                }
                else if (initOp is IPropertyInitializerOperation propInitOp)
                {
                    var propSymbol = propInitOp.InitializedProperties.FirstOrDefault();
                    if (propSymbol == null || !propSymbol.IsStatic || propSymbol.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    declaredSymbolSet?.UnionWith(propInitOp.InitializedProperties);  // for declaring order check
                    declaredSymbol = propSymbol;
                }


                if (declaredSymbol == null)
                {
                    continue;
                }

                foreach (var refOp in initOp.Descendants().OfType<IMemberReferenceOperation>())
                {
                    if (!refOp.Member.IsStatic || refOp.Member.IsImplicitlyDeclared)
                        continue;

                    //const??
                    if (refOp.Member is IFieldSymbol { IsConst: true })
                        continue;

                    ////method??
                    //if (refOp.Member is IMethodSymbol)
                    //    continue;

                    //nameof/typeof??
                    if (refOp.Parent is INameOfOperation)// or ITypeOfOperation)
                        continue;

                    declaredWithInitializerSymbolList?.Add(declaredSymbol);  // allow duplicate entry to simplify logic
                    initializerRefOperatorList.Add(refOp);
                }
            }
        }

    }
}
