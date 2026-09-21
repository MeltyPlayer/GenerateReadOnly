using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using readOnly.util.symbols;
using readOnly.util.types;


namespace readOnly.generator;

internal static class ReadOnlyTypeGeneratorUtil {
  public const string PREFIX = "IReadOnly";

  public static string
      GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
          this ITypeSymbol sourceSymbol,
          ITypeSymbol referencedSymbol,
          SemanticModel semanticModel,
          TypeDeclarationSyntax sourceDeclarationSyntax,
          ISymbol? memberSymbol = null)
    => sourceSymbol.GetQualifiedNameFromCurrentSymbol(
        referencedSymbol,
        memberSymbol,
        ConvertName_,
        r => GetNamespaceOfType(r, semanticModel, sourceDeclarationSyntax));

  public static string GetQualifiedNameAndGenericsFromCurrentSymbol(
      this ITypeSymbol sourceSymbol,
      ITypeSymbol referencedSymbol,
      SemanticModel semanticModel,
      TypeDeclarationSyntax sourceDeclarationSyntax,
      ISymbol? memberSymbol = null)
    => sourceSymbol.GetQualifiedNameFromCurrentSymbol(
        referencedSymbol,
        memberSymbol,
        null,
        r => GetNamespaceOfType(r, semanticModel, sourceDeclarationSyntax));

  public static string GetTypeConstraintsOrReadonly(
      this ITypeSymbol sourceSymbol,
      IReadOnlyList<ITypeParameterSymbol> typeParameters,
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax,
      GeneratorUtilContext? context = null) {
    var sb = new StringBuilder();

    foreach (var typeParameter in typeParameters) {
      var typeConstraintNames
          = sourceSymbol
            .GetTypeConstraintNames_(typeParameter,
                                     semanticModel,
                                     syntax,
                                     context)
            .ToArray();
      if (typeConstraintNames.Length == 0) {
        continue;
      }

      sb.Append(" where ")
        .Append(typeParameter.Name.EscapeKeyword())
        .Append(" : ");

      for (var i = 0; i < typeConstraintNames.Length; ++i) {
        if (i > 0) {
          sb.Append(", ");
        }

        sb.Append(typeConstraintNames[i]);
      }
    }

    return sb.ToString();
  }

  private static IEnumerable<string> GetTypeConstraintNames_(
      this ITypeSymbol sourceSymbol,
      ITypeParameterSymbol typeParameter,
      SemanticModel semanticModel,
      TypeDeclarationSyntax sourceDeclarationSyntax,
      GeneratorUtilContext? context = null) {
    if (typeParameter.HasNotNullConstraint) {
      yield return "notnull";
    }

    if (typeParameter.HasConstructorConstraint) {
      yield return "new()";
    }

    if (typeParameter.HasUnmanagedTypeConstraint) {
      yield return "unmanaged";
    }

    if (typeParameter.HasReferenceTypeConstraint) {
      yield return typeParameter
                       .ReferenceTypeConstraintNullableAnnotation ==
                   NullableAnnotation.Annotated
          ? "class?"
          : "class";
    }

    if (typeParameter is {
            HasValueTypeConstraint: true, HasUnmanagedTypeConstraint: false
        }) {
      yield return "struct";
    }

    for (var i = 0; i < typeParameter.ConstraintTypes.Length; ++i) {
      var constraintType = typeParameter.ConstraintTypes[i];
      var qualifiedName = sourceSymbol.GetQualifiedNameFromCurrentSymbol(
          constraintType,
          typeParameter,
          ConvertName_,
          r => GetNamespaceOfType(r,
                                  semanticModel,
                                  sourceDeclarationSyntax,
                                  context));

      yield return typeParameter.ConstraintNullableAnnotations[i] ==
                   NullableAnnotation.Annotated
          ? $"{qualifiedName}?"
          : qualifiedName;
    }
  }

  private static string ConvertName_(ITypeSymbol typeSymbol,
                                     ISymbol? symbolForAttributeChecks) {
    var defaultName = typeSymbol.Name.EscapeKeyword();
    if ((symbolForAttributeChecks ?? typeSymbol)
        .HasAttribute<KeepMutableTypeAttribute>()) {
      return defaultName;
    }

    if (typeSymbol.HasBuiltInReadOnlyType_(out var builtInReadOnlyName,
                                           out _)) {
      return builtInReadOnlyName;
    }

    return typeSymbol.HasAttribute<GenerateReadOnlyAttribute>()
        ? typeSymbol.GetConstInterfaceName()
        : defaultName;
  }

  private static bool HasBuiltInReadOnlyType_(
      this ITypeSymbol symbol,
      out string readOnlyName,
      out bool canImplicitlyConvert) {
    if (symbol.IsType(typeof(ICollection<>))) {
      readOnlyName = typeof(IReadOnlyCollection<>).GetCorrectName();
      canImplicitlyConvert = false;
      return true;
    }

    if (symbol.IsType(typeof(IDictionary<,>)) ||
        symbol.IsType(typeof(Dictionary<,>))) {
      readOnlyName = typeof(IReadOnlyDictionary<,>).GetCorrectName();
      canImplicitlyConvert = false;
      return true;
    }

    if (symbol.IsType(typeof(IList<>)) || symbol.IsType(typeof(List<>))) {
      readOnlyName = typeof(IReadOnlyList<>).GetCorrectName();
      canImplicitlyConvert = false;
      return true;
    }

    if (symbol.IsType(typeof(Memory<>))) {
      readOnlyName = typeof(ReadOnlyMemory<>).GetCorrectName();
      canImplicitlyConvert = true;
      return true;
    }

    if (symbol.IsType(typeof(Span<>))) {
      readOnlyName = typeof(ReadOnlySpan<>).GetCorrectName();
      canImplicitlyConvert = true;
      return true;
    }

    readOnlyName = default;
    canImplicitlyConvert = false;
    return false;
  }

  public static string GetConstInterfaceName(
      this ITypeSymbol typeSymbol) {
    var baseName = typeSymbol.Name;
    if (baseName.Length >= 2) {
      if (baseName[1] is < 'a' or > 'z') {
        var firstChar = baseName[0];
        if ((firstChar == 'I' && typeSymbol.IsInterface()) ||
            (firstChar == 'B' && typeSymbol.IsAbstractClass())) {
          baseName = baseName.Substring(1);
        }
      }
    }

    return $"{PREFIX}{baseName}";
  }

  public static IEnumerable<INamedTypeSymbol> LookupTypesWithNameAndArity(
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax,
      string searchString,
      int arity)
    => semanticModel
       .LookupNamespacesAndTypes(syntax.SpanStart, null, searchString)
       .Where(symbol => symbol.HasAttribute<GenerateReadOnlyAttribute>())
       .OfType<INamedTypeSymbol>()
       .Where(symbol => symbol.Arity == arity);

  public static IEnumerable<string>? GetNamespaceOfType(
      this ITypeSymbol typeSymbol,
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax,
      GeneratorUtilContext? context = null) {
    if (!typeSymbol.Exists()) {
      var typeName = typeSymbol.Name;
      var arity = typeSymbol.GetArity();

      if (context?.KnownNamespaces.TryGetValue((typeName, arity),
                                               out var knownNamespace) ??
          false) {
        return knownNamespace;
      }

      if (typeName.StartsWith(ReadOnlyTypeGeneratorUtil.PREFIX)) {
        var nameWithoutPrefix
            = typeName.Substring(ReadOnlyTypeGeneratorUtil.PREFIX.Length);

        var typesWithName
            = LookupTypesWithNameAndArity(semanticModel,
                                          syntax,
                                          nameWithoutPrefix,
                                          arity)
                .ToArray();
        if (typesWithName.Length == 0) {
          typesWithName = LookupTypesWithNameAndArity(semanticModel,
                syntax,
                $"B{nameWithoutPrefix}",
                arity)
              .ToArray();
        }

        if (typesWithName.Length == 0) {
          typesWithName = LookupTypesWithNameAndArity(semanticModel,
                syntax,
                $"I{nameWithoutPrefix}",
                arity)
              .ToArray();
        }

        if (typesWithName.Length == 1) {
          var typeWithName = typesWithName[0];
          return typeWithName.GetContainingNamespaces();
        }
      }
    }

    return typeSymbol.GetContainingNamespaces();
  }

  public static string GetGenericParametersWithVarianceForReadOnlyVersion(
      this INamedTypeSymbol symbol,
      IReadOnlyList<IMethodSymbol> constMembers) {
    var typeParameters = symbol.TypeParameters;
    if (typeParameters.Length == 0) {
      return "";
    }

    var allParentTypes
        = symbol.GetBaseTypes().Concat(symbol.AllInterfaces).ToArray();

    var set = new TypeParameterSymbolVarianceSet(
        typeParameters,
        allParentTypes,
        constMembers);

    var sb = new StringBuilder();
    sb.Append("<");
    for (var i = 0; i < typeParameters.Length; ++i) {
      if (i > 0) {
        sb.Append(", ");
      }

      var typeParameter = typeParameters[i];

      var variance = typeParameter.Variance;
      if (variance == VarianceKind.None) {
        variance = set.AllowedVariance(typeParameter);
      }

      sb.Append(variance switch {
            VarianceKind.In   => "in ",
            VarianceKind.Out  => "out ",
            VarianceKind.None => "",
        })
        .Append(typeParameter.Name.EscapeKeyword());
    }

    sb.Append(">");
    return sb.ToString();
  }

  public static string GetCStyleCastToReadOnlyIfNeeded(
      this ITypeSymbol sourceSymbol,
      ISymbol? symbolForAttributeChecks,
      ITypeSymbol symbol,
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax) {
    // TODO: Only allow casts if generics are covariant, otherwise report
    // diagnostic error
    var sb = new StringBuilder();
    if (symbol.IsCastNeeded(symbolForAttributeChecks)) {
      sb.Append("(")
        .Append(
            sourceSymbol
                .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                    symbol,
                    semanticModel,
                    syntax))
        .Append(")(object) ");
    }

    return sb.ToString();
  }

  public static bool IsCastNeeded(this ITypeSymbol symbol,
                                  ISymbol? symbolForAttributeChecks) {
    if ((symbolForAttributeChecks ?? symbol)
        .HasAttribute<KeepMutableTypeAttribute>()) {
      return false;
    }

    if (symbol.IsGeneric(out _, out var typeArguments) &&
        typeArguments.Any(typeArgument => typeArgument
                              .HasAttribute<GenerateReadOnlyAttribute>())) {
      return true;
    }

    return symbol.HasBuiltInReadOnlyType_(out _,
                                          out var canImplicitlyConvert) &&
           !canImplicitlyConvert;
  }

  public static IEnumerable<IMethodSymbol> GetConstMembers(
      this INamedTypeSymbol typeSymbol) {
    if (!typeSymbol.HasAttribute<GenerateReadOnlyAttribute>()) {
      return [];
    }

    return TypeInfoParser
           .ParseMembers(typeSymbol)
           .Where(parsedMember => {
                    var (parseStatus, memberSymbol) = parsedMember;
                    if (parseStatus ==
                        TypeInfoParser.ParseStatus
                                      .NOT_A_FIELD_OR_PROPERTY_OR_METHOD) {
                      return false;
                    }

                    if (memberSymbol.DeclaredAccessibility is not (
                        Accessibility.Public
                        or Accessibility.Internal)) {
                      return false;
                    }

                    if (memberSymbol is IFieldSymbol) {
                      return false;
                    }

                    if (memberSymbol is IPropertySymbol) {
                      return false;
                    }

                    if (memberSymbol is IMethodSymbol &&
                        !memberSymbol.Name.StartsWith("get_") &&
                        !memberSymbol.HasAttribute<ConstAttribute>()) {
                      return false;
                    }

                    return true;
                  })
           .Select(parsedMember => (IMethodSymbol) parsedMember.Item2);
  }
}