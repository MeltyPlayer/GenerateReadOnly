using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using readOnly.util.asserts;

using readOnly.util.symbols;


namespace readOnly.util.types;

public static class TypeInfoParser {
  public enum ParseStatus {
    SUCCESS,
    NOT_A_FIELD_OR_PROPERTY_OR_METHOD,
    NOT_IMPLEMENTED,
  }

  public static IEnumerable<(ParseStatus, ISymbol)> ParseMembers(
      INamedTypeSymbol containerSymbol) {
    foreach (var memberSymbol in containerSymbol.GetInstanceMembers()) {
      // Tries to parse the type to get info about it
      var parseStatus = ParseMember(memberSymbol);
      yield return (parseStatus, memberSymbol);
    }
  }

  public static ParseStatus ParseMember(ISymbol memberSymbol) {
    if (memberSymbol is IMethodSymbol) {
      return ParseStatus.SUCCESS;
    }

    if (!GetTypeOfMember_(memberSymbol, out var memberTypeSymbol)) {
      return ParseStatus.NOT_A_FIELD_OR_PROPERTY_OR_METHOD;
    }

    // Primary constructor params.
    var memberName = memberSymbol.Name;
    if (memberName.Contains('<') || memberName.Contains('>')) {
      return ParseStatus.NOT_A_FIELD_OR_PROPERTY_OR_METHOD;
    }

    return ParseTypeSymbol(memberTypeSymbol);
  }

  public static ParseStatus ParseTypeSymbol(ITypeSymbol typeSymbol) {
    ParseNullable_(ref typeSymbol);

    if (typeSymbol.IsPrimitive(out _)) {
      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsString()) {
      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsSequence(out var elementTypeV2)) {
      var elementParseStatus = ParseTypeSymbol(elementTypeV2);
      if (elementParseStatus != ParseStatus.SUCCESS) {
        return elementParseStatus;
      }

      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsClass() ||
        typeSymbol.IsInterface() ||
        typeSymbol.IsStruct() ||
        typeSymbol is IErrorTypeSymbol) {
      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsGenericTypeParameter(out _)) {
      return ParseStatus.SUCCESS;
    }

    return ParseStatus.NOT_IMPLEMENTED;
  }

  private static bool GetTypeOfMember_(
      ISymbol memberSymbol,
      out ITypeSymbol memberTypeSymbol) {
    switch (memberSymbol) {
      case IPropertySymbol propertySymbol: {
        memberTypeSymbol = propertySymbol.Type;
        return true;
      }
      case IFieldSymbol fieldSymbol: {
        memberTypeSymbol = fieldSymbol.Type;
        return true;
      }
      default: {
        memberTypeSymbol = default;
        return false;
      }
    }
  }

  private static void ParseNullable_(ref ITypeSymbol typeSymbol) {
    if (!typeSymbol.IsType(typeof(Nullable<>))) {
      return;
    }

    Asserts.True(typeSymbol.IsGeneric(out _, out var genericArguments));
    typeSymbol = genericArguments.ToArray()[0];
  }
}