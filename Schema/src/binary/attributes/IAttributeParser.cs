using schema.util.symbols;
using schema.util.types;


namespace schema.binary.attributes;

internal interface IAttributeParser {
  void ParseIntoMemberType(IBetterSymbol memberBetterSymbol,
                           ITypeInfo memberTypeInfo,
                           IMemberType memberType);
}