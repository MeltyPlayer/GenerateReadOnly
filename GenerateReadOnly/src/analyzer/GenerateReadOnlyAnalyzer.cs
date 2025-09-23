using System;
using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using readOnly.generator;
using readOnly.util.symbols;
using readOnly.util.syntax;

namespace readOnly.analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GenerateReadOnlyAnalyzer : DiagnosticAnalyzer {
  public override ImmutableArray<DiagnosticDescriptor>
      SupportedDiagnostics { get; } =
    ImmutableArray.Create(
        Rules.TypeMustBePartial,
        Rules.ParentTypeMustBePartial,
        Rules.Exception
    );

  public override void Initialize(AnalysisContext context) {
    context.RegisterSyntaxNodeAction(
        syntaxNodeContext => {
          var syntax = syntaxNodeContext.Node as ClassDeclarationSyntax;

          var symbol =
              syntaxNodeContext.SemanticModel.GetDeclaredSymbol(syntax!);
          if (symbol is not INamedTypeSymbol namedTypeSymbol) {
            return;
          }

          CheckType(syntaxNodeContext, syntax!, namedTypeSymbol);
        },
        SyntaxKind.ClassDeclaration);

    context.RegisterSyntaxNodeAction(
        syntaxNodeContext => {
          var syntax = syntaxNodeContext.Node as StructDeclarationSyntax;

          var symbol =
              syntaxNodeContext.SemanticModel.GetDeclaredSymbol(syntax!);
          if (symbol is not INamedTypeSymbol namedTypeSymbol) {
            return;
          }

          CheckType(syntaxNodeContext, syntax!, namedTypeSymbol);
        },
        SyntaxKind.StructDeclaration);
  }

  public static void CheckType(
      SyntaxNodeAnalysisContext context,
      TypeDeclarationSyntax syntax,
      INamedTypeSymbol symbol) {
    try {
      if (!symbol.HasAttribute<GenerateReadOnlyAttribute>()) {
        return;
      }

      if (!syntax.IsPartial()) {
        Rules.ReportDiagnostic(
            context,
            symbol,
            Rules.TypeMustBePartial);
        return;
      }

      var containingType = symbol.ContainingType;
      while (containingType != null) {
        var typeDeclarationSyntax =
            containingType.DeclaringSyntaxReferences[0].GetSyntax() as
                TypeDeclarationSyntax;

        if (!typeDeclarationSyntax!.IsPartial()) {
          Rules.ReportDiagnostic(
              context,
              symbol,
              Rules.ParentTypeMustBePartial);
        }

        containingType = containingType.ContainingType;
      }

      ReadOnlyTypeGenerator.GenerateSourceForNamedType(
          symbol,
          context.SemanticModel,
          syntax);
    } catch (Exception exception) {
      if (Debugger.IsAttached) {
        throw;
      }

      Rules.ReportExceptionDiagnostic(context, symbol, exception);
    }
  }
}