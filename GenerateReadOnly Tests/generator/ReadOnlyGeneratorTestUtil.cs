using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using NUnit.Framework;

using readOnly.analyzer;
using readOnly.util.asserts;
using readOnly.util.diagnostics;

#pragma warning disable CS8604


namespace readOnly.generator;

internal static class ReadOnlyGeneratorTestUtil {
  public static CSharpCompilation Compilation =
      CSharpCompilation
          .Create("test")
          .AddReferences(
              ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
              .Split(Path.PathSeparator)
              .Select(path => MetadataReference.CreateFromFile(path)));

  public static void AssertGenerated(string src, params string[] expected) {
    var actual = ParseReadOnlyTypeCandidates(src, out var semanticModel)
        .Select(symbolAndSyntax
                    => ReadOnlyTypeGenerator
                       .GenerateSourceForNamedType(
                           symbolAndSyntax.namedTypeSymbol,
                           semanticModel,
                           symbolAndSyntax.declarationSyntax)
                       .ReplaceLineEndings());

    CollectionAssert.AreEqual(expected, actual);
  }

  public static void AssertDiagnostics(
      string src,
      params DiagnosticDescriptor[] expected) {
    var actual = new List<DiagnosticDescriptor>();

    foreach (var (namedTypeSymbol, declarationSyntax) in
             ParseReadOnlyTypeCandidates(src, out var semanticModel)) {
      var context = new SyntaxNodeAnalysisContext(
          declarationSyntax,
          semanticModel,
          new AnalyzerOptions([]),
          diagnostic => actual.Add(diagnostic.Descriptor),
          _ => true,
          CancellationToken.None);

      GenerateReadOnlyAnalyzer.CheckType(
          context,
          declarationSyntax,
          namedTypeSymbol);
    }

    CollectionAssert.AreEqual(expected, actual);
  }

  private static IEnumerable<(INamedTypeSymbol namedTypeSymbol,
          TypeDeclarationSyntax declarationSyntax)>
      ParseReadOnlyTypeCandidates(string src, out SemanticModel semanticModel) {
    var syntaxTree = CSharpSyntaxTree.ParseText(src);
    var compilation = Compilation.Clone()
                                 .AddSyntaxTrees(syntaxTree);

    semanticModel = compilation.GetSemanticModel(syntaxTree);
    var localSemanticModel = semanticModel;

    return syntaxTree
           .GetRoot()
           .DescendantTokens()
           .Where(t => t is {
               Text: "GenerateReadOnly",
               Parent.Parent: AttributeSyntax
           })
           .Select(t => t.Parent?.Parent as AttributeSyntax)
           .Select(attributeSyntax => {
                     var attributeListSyntax
                         = Asserts.AsA<AttributeListSyntax>(
                             attributeSyntax.Parent);
                     var declarationSyntax
                         = Asserts.AsA<TypeDeclarationSyntax>(
                             attributeListSyntax.Parent);

                     var symbol
                         = localSemanticModel.GetDeclaredSymbol(
                             declarationSyntax);
                     var namedTypeSymbol
                         = symbol as INamedTypeSymbol;

                     return (namedTypeSymbol, declarationSyntax);
                   });
  }
}