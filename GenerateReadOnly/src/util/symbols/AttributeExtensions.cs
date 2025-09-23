using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace readOnly.util.symbols;

public static class AttributeExtensions {
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal static IEnumerable<AttributeData>
      GetAttributeData<TAttribute>(this ISymbol symbol) {
    var attributeType = typeof(TAttribute);
    return symbol
           .GetAttributes()
           .Where(attributeData
                      => attributeData.AttributeClass?.IsType(
                             attributeType) ??
                         false);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal static bool HasAttribute<TAttribute>(this ISymbol symbol)
      where TAttribute : Attribute
    => symbol.GetAttributeData<TAttribute>().Any();
}