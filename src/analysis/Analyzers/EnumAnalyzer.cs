// Licensed under the MIT License
// https://github.com/sator-imaging/StaticMemberAnalyzer

//#define STMG_ENABLE_KOTLIN_ENUM

#define STMG_DEBUG_MESSAGE
#if DEBUG == false
#undef STMG_DEBUG_MESSAGE
#endif

#if STMG_ENABLE_KOTLIN_ENUM
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SatorImaging.StaticMemberAnalyzer.Analysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EnumAnalyzer : DiagnosticAnalyzer
    {
        private const string SuppressionComment = "// Allow enum conversion";
        private const string UnknownEnumTypeName = "<Unknown Enum>";

        #region     /* =      DESCRIPTOR      = */

        public const string RuleId_CastToEnum = "SMA0020";
        private static readonly DiagnosticDescriptor Rule_CastToEnum = new(
            RuleId_CastToEnum,
            new LocalizableResourceString(nameof(Resources.SMA0020_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0020_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0020_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_CastFromEnum = "SMA0021";
        private static readonly DiagnosticDescriptor Rule_CastFromEnum = new(
            RuleId_CastFromEnum,
            new LocalizableResourceString(nameof(Resources.SMA0021_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0021_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0021_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_CastToGenericEnum = "SMA0022";
        private static readonly DiagnosticDescriptor Rule_CastToGenericEnum = new(
            RuleId_CastToGenericEnum,
            new LocalizableResourceString(nameof(Resources.SMA0022_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0022_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0022_Description), Resources.ResourceManager, typeof(Resources)));

        public const string RuleId_CastFromGenericEnum = "SMA0023";
        private static readonly DiagnosticDescriptor Rule_CastFromGenericEnum = new(
            RuleId_CastFromGenericEnum,
            new LocalizableResourceString(nameof(Resources.SMA0023_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0023_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0023_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_EnumToString = "SMA0024";
        private static readonly DiagnosticDescriptor Rule_EnumToString = new(
            RuleId_EnumToString,
            new LocalizableResourceString(nameof(Resources.SMA0024_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0024_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0024_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_EnumMethod = "SMA0025";
        private static readonly DiagnosticDescriptor Rule_EnumMethod = new(
            RuleId_EnumMethod,
            new LocalizableResourceString(nameof(Resources.SMA0025_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0025_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0025_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_EnumObfuscation = "SMA0026";
        private static readonly DiagnosticDescriptor Rule_EnumObfuscation = new(
            RuleId_EnumObfuscation,
            new LocalizableResourceString(nameof(Resources.SMA0026_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0026_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0026_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_UnusualEnum = "SMA0027";
        private static readonly DiagnosticDescriptor Rule_UnusualEnum = new(
            RuleId_UnusualEnum,
            new LocalizableResourceString(nameof(Resources.SMA0027_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0027_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0027_Description), Resources.ResourceManager, typeof(Resources)));


        public const string RuleId_EnumLike = "SMA0028";
#if STMG_ENABLE_KOTLIN_ENUM
        private static readonly DiagnosticDescriptor Rule_EnumLike = new(
            RuleId_EnumLike,
            new LocalizableResourceString(nameof(Resources.SMA0028_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.SMA0028_MessageFormat), Resources.ResourceManager, typeof(Resources)),
            Core.Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.SMA0028_Description), Resources.ResourceManager, typeof(Resources)));
#endif

        #endregion


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
#if STMG_DEBUG_MESSAGE
            Core.Rule_DebugError,
            Core.Rule_DebugWarn,
#endif
            Rule_CastToEnum,
            Rule_CastFromEnum,
            Rule_CastToGenericEnum,
            Rule_CastFromGenericEnum,
            Rule_EnumToString,
            Rule_EnumMethod,
            Rule_EnumObfuscation,
            Rule_UnusualEnum
#if STMG_ENABLE_KOTLIN_ENUM
            , Rule_EnumLike
#endif
            );


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();


            //https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(AnalyzeCast, OperationKind.Conversion);
            context.RegisterOperationAction(AnalyzeInterpolatedString, OperationKind.Interpolation);

            context.RegisterOperationAction(AnalyzeObjectCreation, ImmutableArray.Create(
                OperationKind.ObjectCreation,
                OperationKind.AnonymousObjectCreation,
                OperationKind.DefaultValue,
                OperationKind.TypeParameterObjectCreation
                ));

#if STMG_ENABLE_KOTLIN_ENUM
            context.RegisterSyntaxNodeAction(AnalyzeEnumLikePattern, SyntaxKind.ClassDeclaration);
#endif

            context.RegisterSymbolAction(AnalyzeEnumDeclaration, SymbolKind.NamedType);
        }


        /*  enum methods  ================================================================ */

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation op)
            {
                return;
            }

            var receiverType = op.TargetMethod.ReceiverType;
            if (receiverType?.SpecialType == SpecialType.System_Enum)
            {
                if (Core.IsSuppressedByComment(op, SuppressionComment))
                {
                    return;
                }

                //string??
                if (op.TargetMethod.ReturnType.SpecialType == SpecialType.System_String)
                {
                    var (enumType, _) = GetEnumInfo(op.Instance?.Type ?? receiverType);

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumToString, GetReportLocation(op), enumType?.ToDiagnosticMessageName() ?? UnknownEnumTypeName));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumMethod, GetReportLocation(op)));
                }
            }
        }


        /*  creation  ================================================================ */

        private static void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var op = context.Operation;

            var (enumType, _) = GetEnumInfo(op.Type);
            if (enumType == null)
            {
                return;
            }

            if (op is IDefaultValueOperation)
            {
                // method has default value of generic type arg T which has T : Enum constraint
                // --> Method<TEnum>(TEnum value = default)
                if (op.IsImplicit && op.Parent is IArgumentOperation && op.Parent?.Parent is IInvocationOperation)
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule_CastToEnum, GetReportLocation(op), enumType.ToDiagnosticMessageName()));
        }


        /*  interpolated string ($"")  ================================================================ */

        private static void AnalyzeInterpolatedString(OperationAnalysisContext context)
        {
            // NOTE: no conversion operation is reported by roslyn but cast happens internally
            //       --> $"value: {enumValue}"

            // NOTE: okay process only first child, expression inside interpolation will be processed by other analyzer
            //       --> $"value: {"" + enumVal + 0}"  // <-- checked by other analyzer code path
            if (context.Operation.Children.FirstOrDefault() is not IOperation op)
            {
                return;
            }

            var (enumType, _) = GetEnumInfo(op.Type);
            if (enumType != null)
            {
                // context.Operation: {foo} -> Parent: $"...{foo}..."
                if (Core.IsSuppressedByComment(context.Operation.Parent, SuppressionComment))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule_EnumToString, GetReportLocation(context.Operation), enumType.ToDiagnosticMessageName()));
            }
        }


        /*  cast  ================================================================ */

        private static void AnalyzeCast(OperationAnalysisContext context)
        {
            if (context.Operation is not IConversionOperation op)
            {
                return;
            }

            if (op.IsImplicit)
            {
                // NOTE: throw exception inside switch expression will perform implicit cast operation from
                //       exception instance to switch expression result type
                //       --> var val = enumValue switch { ..., _ => throw new Exception(); }
                if (op.Operand is IThrowOperation)
                {
                    return;
                }

                // NOTE: if enum method parameter has default value and it's omit on invocation
                //       implicit cast will happen internally (cannot use: IOmittedArgumentOperation)
                if (op.Parent is IArgumentOperation //castOp.Syntax
                                                    //is InvocationExpressionSyntax
                                                    //or AttributeSyntax
                                                    //or ObjectCreationExpressionSyntax
                                                    //or AnonymousObjectCreationExpressionSyntax
                )
                {
                    return;
                }
            }

            if (Core.IsSuppressedByComment(op, SuppressionComment))
            {
                return;
            }

            AnalyzeCast_Impl(context, op);
        }


        private static void AnalyzeCast_Impl(OperationAnalysisContext context, IConversionOperation castOp)
        {
            // implicit cast to string??
            // --> "value: " + enumValue;
            if (castOp.Parent is IBinaryOperation binaryOp
             && binaryOp.LeftOperand.Type.SpecialType == SpecialType.System_String
             && binaryOp.RightOperand.Type.SpecialType == SpecialType.System_Object
            )
            {
                var (enumType, _) = GetEnumInfo(castOp.Operand.Type);
                if (enumType != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumToString, GetReportLocation(binaryOp), enumType.ToDiagnosticMessageName()));

                    return;
                }
            }

            // MUST check castOp.Type first!!
            AnalyzeCast_FromToEnum(context, castOp, castOp.Type, Rule_CastToEnum, Rule_CastToGenericEnum);
            AnalyzeCast_FromToEnum(context, castOp, castOp.Operand.Type, Rule_CastFromEnum, Rule_CastFromGenericEnum);
        }


        private static void AnalyzeCast_FromToEnum(OperationAnalysisContext context,
                                                   IConversionOperation castOp,
                                                   ITypeSymbol? symbol,
                                                   DiagnosticDescriptor concreteDescriptor,
                                                   DiagnosticDescriptor genericDescriptor
            )
        {
            var (enumType, isGeneric) = GetEnumInfo(symbol);
            if (enumType == null)
            {
                return;
            }

            if (!isGeneric)
            {
                // check if cast is from enum to nullable enum or vice versa
                var (targetType, _) = GetEnumInfo(castOp.Type);
                var (sourceType, _) = GetEnumInfo(castOp.Operand.Type);

                if (SymbolEqualityComparer.Default.Equals(targetType, sourceType))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    concreteDescriptor, GetReportLocation(castOp), enumType.ToDiagnosticMessageName()));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    genericDescriptor, GetReportLocation(castOp), enumType.ToDiagnosticMessageName()));
            }
        }


        /*  enum-like pattern  ================================================================ */
#if STMG_ENABLE_KOTLIN_ENUM

#pragma warning disable RS1008
        [ThreadStatic] static List<IFieldSymbol>? ts_enumLikePatternFieldSymbolList;
        [ThreadStatic] static List<IFieldSymbol>? ts_enumLikePatternEntriesSymbolList;
        [ThreadStatic] static List<IMethodSymbol>? ts_enumLikePatternEqualsSymbolList;
#pragma warning restore RS1008

        private static void AnalyzeEnumLikePattern(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ClassDeclarationSyntax clsDeclStx)
            {
                return;
            }

            var model = context.SemanticModel;
            if (model.GetDeclaredSymbol(clsDeclStx) is not INamedTypeSymbol fieldContainerSymbol)
            {
                return;
            }

            if (fieldContainerSymbol.IsAbstract
             || fieldContainerSymbol.IsStatic
             || fieldContainerSymbol.IsImplicitlyDeclared
             )
            {
                return;
            }

            const string NAME_ENTRIES = "Entries";
            const string NAME_EQUALS = "Equals";
            const int LIST_CAPACITY = 8;

            var enumFieldList = (ts_enumLikePatternFieldSymbolList ??= new(capacity: LIST_CAPACITY));
            enumFieldList.Clear();

            var enumEntriesList = (ts_enumLikePatternEntriesSymbolList ??= new(capacity: LIST_CAPACITY));
            enumEntriesList.Clear();

            var enumEqualsList = (ts_enumLikePatternEqualsSymbolList ??= new(capacity: LIST_CAPACITY));
            enumEqualsList.Clear();

            bool hasPublicEntries = false;
            foreach (var memberSymbol in fieldContainerSymbol.GetMembers())
            {
                string? symbolName = null;
                bool isPublic = (memberSymbol.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public;

                // public bool Equals?
                // public static RETVAL Entries?
                if (isPublic)
                {
                    symbolName ??= memberSymbol.ToDiagnosticMessageName();

                    if (memberSymbol.IsStatic)
                    {
                        if (symbolName == NAME_ENTRIES)
                        {
                            hasPublicEntries = true;
                        }
                    }
                }

                // check only name and return type. IEquatable<T> won't require 'override' keyword
                if (memberSymbol is IMethodSymbol methodSymbol && methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean)
                {
                    if (isPublic && symbolName == NAME_EQUALS)
                    {
                        enumEqualsList.Add(methodSymbol);
                    }
                    else
                    {
                        foreach (var symbol in methodSymbol.ExplicitInterfaceImplementations)
                        {
                            if (symbol.Name == NAME_EQUALS)
                            {
                                enumEqualsList.Add(methodSymbol);
                                break;
                            }
                        }
                    }
                }


                /* =====  collect field analysis targets  ===== */

                if (memberSymbol is not IFieldSymbol fieldSymbol)
                {
                    continue;
                }

                // check static AND readonly modifiers only here!!
                if (!fieldSymbol.IsStatic || !fieldSymbol.IsReadOnly)
                {
                    continue;
                }

                if (fieldSymbol.IsImplicitlyDeclared)
                {
                    continue;
                }

                // is type enum member?
                if (SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, fieldContainerSymbol))
                {
                    //public?
                    if (isPublic)
                    {
                        enumFieldList.Add(fieldSymbol);
                    }

                    continue;
                }


                //entries??
                symbolName ??= fieldSymbol.ToDiagnosticMessageName();

                if (!symbolName.StartsWith(NAME_ENTRIES, StringComparison.Ordinal/*IgnoreCase*/)
                 && !symbolName.EndsWith(NAME_ENTRIES, StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                //type??
                if (fieldSymbol.Type is IArrayTypeSymbol arr && SymbolEqualityComparer.Default.Equals(arr.ElementType, fieldContainerSymbol))
                {
                    goto ENTRIES_FOUND;
                }

                if (IsReadOnlyMemory(fieldSymbol.Type, fieldContainerSymbol))
                {
                    goto ENTRIES_FOUND;
                }

                // not found
                continue;

                //entries!!
            ENTRIES_FOUND:
                enumEntriesList.Add(fieldSymbol);
            }


            /* =      class      = */

            // only when 'Entries' found
            if (hasPublicEntries || enumEntriesList.Count > 0)
            {
                const Accessibility ACCESS_HIDDEN = Accessibility.Protected | Accessibility.Private | Accessibility.NotApplicable;

                string? fieldContainerSymbolName = null;

                var ctors = fieldContainerSymbol.InstanceConstructors;
                if ((ctors.Length == 1 && ctors[0].IsImplicitlyDeclared)
                  || ctors.Any(static x => (x.DeclaredAccessibility & ~ACCESS_HIDDEN) != 0)
                  || ctors.Length == 0
                )
                {
                    fieldContainerSymbolName ??= fieldContainerSymbol.ToDiagnosticMessageName();

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumLike, clsDeclStx.Identifier.GetLocation(), fieldContainerSymbolName,
                        "constructor is not 'private' or 'protected'"));
                }

                if (!fieldContainerSymbol.IsSealed)
                {
                    fieldContainerSymbolName ??= fieldContainerSymbol.ToDiagnosticMessageName();

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumLike, clsDeclStx.Identifier.GetLocation(), fieldContainerSymbolName,
                        "type should be 'sealed'"));
                }

                // no public 'Entries'
                if (!hasPublicEntries)
                {
                    fieldContainerSymbolName ??= fieldContainerSymbol.ToDiagnosticMessageName();

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumLike, clsDeclStx.Identifier.GetLocation(), fieldContainerSymbolName,
                        "'public static' member called '" + NAME_ENTRIES + "' is not found"));
                }

                //equals
                if (enumEqualsList.Count > 0)
                {
                    foreach (var equalsSymbol in enumEqualsList)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule_EnumLike, equalsSymbol.Locations[0], NAME_EQUALS,
                            "equality comparer should not be overridden"));
                    }

                }
            }


            /* =      entries fields      = */

            for (int i = 0; i < enumEntriesList.Count; i++)
            {
                AnalyzeEnumLikeEntriesField(context, enumEntriesList[i], enumFieldList);
            }
        }


        private static void AnalyzeEnumLikeEntriesField(SyntaxNodeAnalysisContext context,
                                                        IFieldSymbol entriesSymbol,
                                                        List<IFieldSymbol> enumFieldList
            )
        {
            var entriesContainerSymbol = entriesSymbol.ContainingType;
            string? entriesContainerSymbolName = null;

            /* =      Entries initializer      = */

            var initializerStx = entriesSymbol.DeclaringSyntaxReferences[0]?.GetSyntax()
                .DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();

            if (initializerStx == null)
            {
                foreach (var stxRef in entriesSymbol.DeclaringSyntaxReferences)
                {
                    entriesContainerSymbolName ??= entriesContainerSymbol.Name;
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumLike, stxRef.GetSyntax().GetLocation(), entriesContainerSymbolName,
                        "'Entries' doesn't have field initializer"));
                }

                return;
            }


            InitializerExpressionSyntax? initExprStx =
                initializerStx.DescendantNodes().OfType<ArrayCreationExpressionSyntax>().FirstOrDefault()?.Initializer
             ?? initializerStx.DescendantNodes().OfType<ImplicitArrayCreationExpressionSyntax>().FirstOrDefault()?.Initializer
            ;

            if (initExprStx == null)
            {
                goto REPORT_INITIALIZER;
            }
            else
            {
                var initRefSyntaxes = initExprStx.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .ToArray();

                if (initRefSyntaxes.Length != enumFieldList.Count)
                {
                    goto REPORT_INITIALIZER;
                }
                else
                {
                    var model = context.Compilation.GetSemanticModel(initExprStx.SyntaxTree);

                    var initExprSymbols = initRefSyntaxes
                        .Select(x =>
                        {
                            if (model.GetOperation(x) is IFieldReferenceOperation fieldRefOp)
                            {
                                return fieldRefOp;
                            }
                            return null;
                        })
                        .Where(static x => x != null)
                        .ToArray();

                    for (int i = 0; i < enumFieldList.Count; i++)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(initExprSymbols[i]/*checked*/!.Member, enumFieldList[i]))
                        {
                            goto REPORT_INITIALIZER;
                        }
                    }

                    return;
                }
            }


        REPORT_INITIALIZER:
            entriesContainerSymbolName ??= entriesContainerSymbol.Name;
            context.ReportDiagnostic(Diagnostic.Create(
                Rule_EnumLike, initializerStx.GetLocation(), entriesContainerSymbolName,
                "'Entries' doesn't have all of 'public static readonly' field of type '" + entriesContainerSymbolName + "' in declared order"));
        }

        private static bool IsReadOnlyMemory(ITypeSymbol memoryCandidateType,
                                             ITypeSymbol elementType
            )
        {
            if (memoryCandidateType is not INamedTypeSymbol targetType)
            {
                return false;
            }

            if (targetType.TypeArguments.Length != 1)
            {
                return false;
            }

            if (!SymbolEqualityComparer.Default.Equals(targetType.TypeArguments[0], elementType))
            {
                return false;
            }

            if (targetType.Name != nameof(ReadOnlyMemory<int>))
            {
                return false;
            }

            var targetNS = targetType.ContainingNamespace;
            if (targetNS.Name != nameof(System) || !targetNS.ContainingNamespace.IsGlobalNamespace)
            {
                return false;
            }

            return true;
        }
#endif


        /*  enum declaration  ================================================================ */

        private static void AnalyzeEnumDeclaration(SymbolAnalysisContext context)
        {
            if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Enum } namedSymbol)
            {
                return;
            }

            AnalyzeEnumDeclaration_Impl(context, namedSymbol);
        }


        private static void AnalyzeEnumDeclaration_Impl(SymbolAnalysisContext context,
                                                        INamedTypeSymbol namedSymbol
            )
        {
            var attrs = namedSymbol.GetAttributes();

            const string ATTR_OBFUSCATION_NAME_FULL = nameof(System) + "." + nameof(System.Reflection) + "." + nameof(ObfuscationAttribute);
            const string ATTR_FLAGS_NAME_FULL = nameof(System) + "." + nameof(FlagsAttribute);

            bool hasObfuscationAttr = false;
            bool hasFlagsAttr = false;
            foreach (var attr in attrs)
            {
                if (attr.AttributeClass == null)
                {
                    continue;
                }

                var attrName = attr.AttributeClass.Name;
                switch (attrName)
                {
                    case nameof(ObfuscationAttribute):
                        {
                            if (attr.AttributeClass.ToString() == ATTR_OBFUSCATION_NAME_FULL)
                            {
                                if (attr.NamedArguments.Any(static x => x.Key == nameof(ObfuscationAttribute.Exclude) && x.Value.Value is bool BOOL && BOOL)
                                 && attr.NamedArguments.Any(static x => x.Key == nameof(ObfuscationAttribute.ApplyToMembers) && x.Value.Value is bool BOOL && BOOL)
                                )
                                {
                                    hasObfuscationAttr = true;
                                }
                            }
                        }
                        break;

                    case nameof(FlagsAttribute):
                        {
                            if (attr.AttributeClass.ToString() == ATTR_FLAGS_NAME_FULL)
                            {
                                hasFlagsAttr = true;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            if (!hasObfuscationAttr)
            {
                foreach (var loc in namedSymbol.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_EnumObfuscation, loc));
                }
            }

            if (!hasFlagsAttr)
            {
                AnalyzeUnusualEnum(context, namedSymbol);
            }
        }


        private static void AnalyzeUnusualEnum(SymbolAnalysisContext context,
                                               INamedTypeSymbol namedSymbol
            )
        {
            // partial enum cannot be declared. ok to take first one
            var declareStx = namedSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declareStx == null)
            {
                return;
            }

            //underlyingType
            if (namedSymbol.EnumUnderlyingType?.SpecialType != SpecialType.System_Int32)
            {
                var baseStx = declareStx.DescendantNodes().OfType_FirstOrDefault<BaseTypeSyntax>();
                if (baseStx != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_UnusualEnum, baseStx.GetLocation()));
                }
            }

            //initializer
            foreach (var memberDeclare in declareStx.DescendantNodes().OfType<EnumMemberDeclarationSyntax>())
            {
                foreach (var childNode in memberDeclare.ChildNodes())
                {
                    if (childNode is AttributeListSyntax)
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule_UnusualEnum, childNode.GetLocation()));
                }
            }
        }


        /*  helper  ================================================================ */


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEnumDerivedType(ITypeSymbol symbol)
        {
            return symbol.TypeKind == TypeKind.Enum || symbol.SpecialType == SpecialType.System_Enum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasEnumConstraint(ITypeParameterSymbol symbol)
        {
            return symbol.ConstraintTypes.Any(static x => x.SpecialType == SpecialType.System_Enum);
        }

        private static Location GetReportLocation(IOperation op)
        {
            var targetOp = op;
            while (targetOp.Parent is IConditionalAccessOperation or IConditionalAccessInstanceOperation)
            {
                targetOp = targetOp.Parent;
            }
            return targetOp.Syntax.GetLocation();
        }

        private static (ITypeSymbol? enumType, bool isGeneric) GetEnumInfo(ITypeSymbol? symbol)
        {
            if (symbol == null)
            {
                return (null, false);
            }

            if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && symbol is INamedTypeSymbol namedType)
            {
                symbol = namedType.TypeArguments[0];
            }

            if (IsEnumDerivedType(symbol))
            {
                return (symbol, false);
            }

            if (symbol is ITypeParameterSymbol typeParam && HasEnumConstraint(typeParam))
            {
                return (symbol, true);
            }

            return (null, false);
        }

    }
}
