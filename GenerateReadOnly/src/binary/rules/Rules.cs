using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using schema.readOnly;


namespace schema.binary;

public static partial class Rules {
  private static int diagnosticId_ = 0;

  private static string GetNextDiagnosticId_() {
    var id = Rules.diagnosticId_++;
    return "SCH" + id.ToString("D3");
  }

  private static DiagnosticDescriptor CreateDiagnosticDescriptor_(
      string title,
      string messageFormat)
    => new(Rules.GetNextDiagnosticId_(),
           title,
           messageFormat,
           "BinarySchemaAnalyzer",
           DiagnosticSeverity.Error,
           true);


  public static DiagnosticDescriptor TypeMustBePartial { get; }
    = Rules.CreateDiagnosticDescriptor_(
        "GenerateReadOnly type must be partial",
        $"Type '{{0}}' was annotated with {nameof(GenerateReadOnlyAttribute)}, so it must be partial to accept automatically generated read/write code.");

  public static DiagnosticDescriptor ContainerTypeMustBePartial {
    get;
  } = Rules.CreateDiagnosticDescriptor_(
      "Container of GenerateReadOnly type must be partial",
      $"Type '{{0}}' contains a type annotated with {nameof(GenerateReadOnlyAttribute)}, so it must be partial to accept automatically generated read/write code.");

  public static DiagnosticDescriptor Exception { get; }
    = Rules.CreateDiagnosticDescriptor_(
        "Exception",
        "Ran into an exception while generating source ({0}),{1}");

  public static DiagnosticDescriptor SymbolException { get; }
    = Rules.CreateDiagnosticDescriptor_(
        "Exception",
        "Ran into an exception while parsing ({0}),{1}");


  public static Diagnostic CreateDiagnostic(
      ISymbol symbol,
      DiagnosticDescriptor descriptor)
    => Diagnostic.Create(
        descriptor,
        symbol.Locations.First(),
        symbol.Name);

  public static void ReportDiagnostic(
      SyntaxNodeAnalysisContext? context,
      ISymbol symbol,
      DiagnosticDescriptor descriptor)
    => context?.ReportDiagnostic(
        Rules.CreateDiagnostic(symbol, descriptor));

  public static Diagnostic CreateExceptionDiagnostic(
      ISymbol symbol,
      Exception exception)
    => Diagnostic.Create(
        Rules.SymbolException,
        symbol.Locations.First(),
        exception.Message,
        exception.StackTrace.Replace("\r\n", "").Replace("\n", ""));

  public static Diagnostic CreateExceptionDiagnostic(
      Exception exception)
    => Diagnostic.Create(
        Rules.Exception,
        null,
        exception.Message,
        exception.StackTrace.Replace("\r\n", "").Replace("\n", ""));


  public static void ReportExceptionDiagnostic(
      SyntaxNodeAnalysisContext? context,
      ISymbol symbol,
      Exception exception)
    => context?.ReportDiagnostic(
        Rules.CreateExceptionDiagnostic(symbol, exception));
}