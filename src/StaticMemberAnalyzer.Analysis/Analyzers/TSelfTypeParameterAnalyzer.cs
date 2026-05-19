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
using System.Collections.Immutable;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TSelfTypeParameterAnalyzer : DiagnosticAnalyzer
    {
        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_TSelfInvariant = "SMA0010";
        private static readonly DiagnosticDescriptor Rule_TSelfInvariant = new(
            RuleId_TSelfInvariant,
            new LocalizableResourceString(nameof(Resources.SMA0010_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0010_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0010_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_TSelfCovariant = "SMA0011";
        private static readonly DiagnosticDescriptor Rule_TSelfCovariant = new(
            RuleId_TSelfCovariant,
            new LocalizableResourceString(nameof(Resources.SMA0011_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0011_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0011_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_TSelfContravariant = "SMA0012";
        private static readonly DiagnosticDescriptor Rule_TSelfContravariant = new(
            RuleId_TSelfContravariant,
            new LocalizableResourceString(nameof(Resources.SMA0012_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0012_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0012_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_TSelfPointingOther = "SMA0015";
        private static readonly DiagnosticDescriptor Rule_TSelfPointingOther = new(
            RuleId_TSelfPointingOther,
            new LocalizableResourceString(nameof(Resources.SMA0015_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0015_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0015_Description), Resources.ResourceManager, typeof(Resources)));

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_TSelfInvariant,
            Rule_TSelfCovariant,
            Rule_TSelfContravariant,

            Rule_TSelfPointingOther
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            context.RegisterSyntaxNodeAction(AnalyzeTSelf, ImmutableArray.Create(
                SyntaxKind.ClassDeclaration,
                // TODO --> SyntaxKind.RecordDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.InterfaceDeclaration
                ));

            context.RegisterSyntaxNodeAction(AnalyzeTypeConstraint, SyntaxKind.TypeParameterConstraintClause);
        }


        /*  TSelf  ================================================================ */

        const string TSELF_NAME = "TSelf";

        private void AnalyzeTSelf(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not TypeDeclarationSyntax targetTypeDeclStx)
                return;

            var baseTypeList = targetTypeDeclStx.DescendantNodes().OfType_FirstOrDefault<BaseListSyntax>();
            if (baseTypeList == null)
                return;

            AnalyzeTSelf_Impl(context, targetTypeDeclStx, baseTypeList);
        }


        private void AnalyzeTSelf_Impl(SyntaxNodeAnalysisContext context,
                                       TypeDeclarationSyntax targetTypeDeclStx,
                                       BaseListSyntax baseTypeList
            )
        {
            ITypeSymbol? targetTypeSymbol = null;
            ITypeSymbol? targetBaseSymbol = null;
            var compilation = context.Compilation;
            SemanticModel? baseTypeModel;
            bool isCovariant = false;
            bool isContravariant = false;
            foreach (var baseType in baseTypeList.Types)
            {
                var genName = baseType.DescendantNodes().OfType_FirstOrDefault<GenericNameSyntax>();
                if (genName == null)
                    continue;

                var typeArgList = genName.TypeArgumentList;
                if (typeArgList == null)
                    continue;

                // find base declaration
                baseTypeModel = compilation.GetSemanticModel(baseType.Type.SyntaxTree);
                var baseSymbol = baseTypeModel.GetTypeInfo(baseType.Type).ConvertedType;
                if (baseSymbol == null)
                {
                    Core.ReportDebugMessage(context.ReportDiagnostic,
                        title: "cannot get baseType symbol", message: null, targetTypeDeclStx.GetLocation());

                    continue;
                }

                int tselfTypeArgPosition = -1;
                foreach (var baseDeclareRef in baseSymbol.DeclaringSyntaxReferences)
                {
                    if (baseDeclareRef.GetSyntax() is not TypeDeclarationSyntax baseDeclare)
                        continue;

                    var baseTypeParamList = baseDeclare.DescendantNodes().OfType_FirstOrDefault<TypeParameterListSyntax>();
                    if (baseTypeParamList == null)
                        continue;

                    for (int i = 0; i < baseTypeParamList.Parameters.Count; i++)
                    {
                        var param = baseTypeParamList.Parameters[i];
                        if (param.Identifier.Text == TSELF_NAME)
                        {
                            tselfTypeArgPosition = i;

                            isCovariant = param.VarianceKeyword.IsKind(SyntaxKind.OutKeyword);
                            isContravariant = param.VarianceKeyword.IsKind(SyntaxKind.InKeyword);

                            goto EXIT_FOREACH;
                        }
                    }
                }

            EXIT_FOREACH:
                ;

                if (tselfTypeArgPosition < 0)
                    continue;

                var foundTypeArgNode = typeArgList.DescendantNodes().ElementAtOrDefault(tselfTypeArgPosition);
                if (foundTypeArgNode == null)
                    continue;


                var foundTypeArgSymbol = baseTypeModel.GetTypeInfo(foundTypeArgNode).ConvertedType;
                if (foundTypeArgSymbol == null)
                {
                    Core.ReportDebugMessage(context.ReportDiagnostic, title: "[NOT FOUND] TypeArg Symbol", message: null, foundTypeArgNode.GetLocation());
                    continue;
                }

                if (targetTypeSymbol == null)
                {
                    var model = compilation.GetSemanticModel(targetTypeDeclStx.SyntaxTree);
                    targetTypeSymbol = model.GetDeclaredSymbol(targetTypeDeclStx);

                    if (targetTypeSymbol == null)
                    {
                        Core.ReportDebugMessage(context.ReportDiagnostic, title: "[NOT FOUND] Target Symbol", message: null, targetTypeDeclStx.Identifier.GetLocation());
                        continue;
                    }
                }

                //go!!
                if (!SymbolEqualityComparer.Default.Equals(foundTypeArgSymbol, targetTypeSymbol))
                {
                    // allow TSelf chaining
                    if (foundTypeArgSymbol.Kind == SymbolKind.TypeParameter && foundTypeArgSymbol.Name == TSELF_NAME)
                    {
                        //ignore!!
                    }

                    //covariant!!
                    else if (isCovariant)
                    {
                        bool isOmittableBaseType = foundTypeArgSymbol.SpecialType == SpecialType.System_Object;
                        if (!isOmittableBaseType)
                        {
                            if (targetBaseSymbol == null)
                            {
                                var targetBaseType = baseTypeList.Types.FirstOrDefault()?.Type;
                                if (targetBaseType != null)
                                {
                                    targetBaseSymbol = baseTypeModel.GetTypeInfo(targetBaseType).ConvertedType;
                                }
                            }

                            var candidateSymbol = targetBaseSymbol;
                            while (candidateSymbol != null)
                            {
                                if (SymbolEqualityComparer.Default.Equals(candidateSymbol, foundTypeArgSymbol))
                                {
                                    isOmittableBaseType = true;
                                    break;
                                }

                                candidateSymbol = candidateSymbol.BaseType;
                            }
                        }

                        if (!isOmittableBaseType)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                Rule_TSelfCovariant, foundTypeArgNode.GetLocation(), targetTypeSymbol.ToString()));
                        }
                    }

                    //contravariant!!
                    else if (isContravariant)
                    {
                        bool isFound = false;

                        var contraSymbol = compilation.GetSemanticModel(foundTypeArgNode.SyntaxTree).GetTypeInfo(foundTypeArgNode).ConvertedType;
                        while (contraSymbol != null)
                        {
                            if (SymbolEqualityComparer.Default.Equals(contraSymbol, targetTypeSymbol))
                            {
                                isFound = true;
                                break;
                            }

                            contraSymbol = contraSymbol.BaseType;
                        }

                        if (!isFound)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                Rule_TSelfContravariant, foundTypeArgNode.GetLocation(), targetTypeSymbol.ToString()));
                        }
                    }

                    //invariant!!
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_TSelfInvariant, foundTypeArgNode.GetLocation(), targetTypeSymbol.ToString()));
                    }
                }
            }
        }


        /*  type constraint  ================================================================ */

        private void AnalyzeTypeConstraint(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not TypeParameterConstraintClauseSyntax typeConstStx)
                return;

            if (typeConstStx.Name.Span.Length != TSELF_NAME.Length ||
                typeConstStx.Name.ToString() != TSELF_NAME)
                return;

            if (typeConstStx.Parent is not ClassDeclarationSyntax classDeclStx)
                return;


            AnalyzeTypeConstraint_Impl(context, typeConstStx, classDeclStx);
        }


        private void AnalyzeTypeConstraint_Impl(SyntaxNodeAnalysisContext context,
                                                TypeParameterConstraintClauseSyntax typeConstStx,
                                                ClassDeclarationSyntax classDeclStx
            )
        {
            var model = context.Compilation.GetSemanticModel(typeConstStx.SyntaxTree);
            var expectedSymbol = model.GetDeclaredSymbol(classDeclStx);

            bool found = false;
            foreach (var typeConst in typeConstStx.DescendantNodes().OfType<TypeConstraintSyntax>())
            {
                var symbol = model.GetSymbolInfo(typeConst.Type).Symbol;
                if (symbol == null)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(symbol, expectedSymbol))
                    found = true;
            }

            if (!found)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule_TSelfPointingOther,
                    typeConstStx.GetLocation(), expectedSymbol));
            }
        }

    }
}
