using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using readOnly.util.symbols;
using readOnly.util.types;

namespace readOnly.generator;

internal static class MembersUtil {
  public static IEnumerable<ISymbol> ParseMembers(
      this INamedTypeSymbol typeSymbol) {
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

                    if (memberSymbol is IPropertySymbol) {
                      return true;
                    }

                    if (memberSymbol is IMethodSymbol {
                            AssociatedSymbol: null
                        }) {
                      return true;
                    }

                    return false;
                  })
           .Select(parsedMember => parsedMember.Item2);
  }

  public static IEnumerable<ISymbol> WhereApplicableForConst(
      this IEnumerable<ISymbol> enumerable)
    => enumerable
        .Where(e => {
                 if (e is IPropertySymbol { GetMethod: { } }) {
                   return true;
                 }

                 if (e is IMethodSymbol methodSymbol &&
                     methodSymbol.HasAttribute<ConstAttribute>()) {
                   return true;
                 }

                 return false;
               });
}