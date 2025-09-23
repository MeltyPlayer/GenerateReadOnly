using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using schema.util.asserts;
using schema.util.symbols;


namespace schema.util.types;

public enum SchemaTypeKind {
  BOOL,
  INTEGER,
  FLOAT,
  CHAR,
  STRING,
  ENUM,
  CONTAINER,
  GENERIC,
  SEQUENCE,
}

public interface ITypeInfo;

public interface IPrimitiveTypeInfo : ITypeInfo;

public interface IBoolTypeInfo : IPrimitiveTypeInfo;

public interface INumberTypeInfo : IPrimitiveTypeInfo;

public interface IIntegerTypeInfo : INumberTypeInfo;

public interface IEnumTypeInfo : IPrimitiveTypeInfo;

public interface ICharTypeInfo : IPrimitiveTypeInfo;

public interface IStringTypeInfo : ITypeInfo;

public interface IContainerTypeInfo : ITypeInfo;

public interface IGenericTypeInfo : ITypeInfo;

public interface ISequenceTypeInfo : ITypeInfo;

public class TypeInfoParser {
  public enum ParseStatus {
    SUCCESS,
    NOT_A_FIELD_OR_PROPERTY_OR_METHOD,
    NOT_IMPLEMENTED,
  }

  public IEnumerable<(ParseStatus, ISymbol, ITypeSymbol, ITypeInfo?)>
      ParseMembers(
          INamedTypeSymbol containerSymbol) {
    foreach (var memberSymbol in containerSymbol.GetInstanceMembers()) {
      // Tries to parse the type to get info about it
      var parseStatus = this.ParseMember(
          memberSymbol,
          out var memberTypeSymbol,
          out var memberTypeInfo);
      yield return (parseStatus, memberSymbol, memberTypeSymbol,
                    memberTypeInfo);
    }
  }

  public ParseStatus ParseMember(ISymbol memberSymbol,
                                 out ITypeSymbol? memberTypeSymbol,
                                 out ITypeInfo? memberTypeInfo) {
    memberTypeSymbol = null;
    memberTypeInfo = null;

    if (memberSymbol is IMethodSymbol) {
      return ParseStatus.SUCCESS;
    }

    if (!GetTypeOfMember_(
            memberSymbol,
            out memberTypeSymbol,
            out var isReadonly)) {
      return ParseStatus.NOT_A_FIELD_OR_PROPERTY_OR_METHOD;
    }

    // Primary constructor params.
    var memberName = memberSymbol.Name;
    if (memberName.Contains('<') || memberName.Contains('>')) {
      return ParseStatus.NOT_A_FIELD_OR_PROPERTY_OR_METHOD;
    }

    return this.ParseTypeSymbol(
        memberTypeSymbol,
        isReadonly,
        out memberTypeInfo);
  }

  public ParseStatus ParseTypeSymbol(
      ITypeSymbol typeSymbol,
      bool isReadonly,
      out ITypeInfo typeInfo) {
    this.ParseNullable_(ref typeSymbol);

    if (typeSymbol.IsPrimitive(out var primitiveType)) {
      switch (primitiveType) {
        case SchemaPrimitiveType.BOOLEAN: {
          typeInfo = new BoolTypeInfo();
          return ParseStatus.SUCCESS;
        }
        case SchemaPrimitiveType.BYTE:
        case SchemaPrimitiveType.SBYTE:
        case SchemaPrimitiveType.INT16:
        case SchemaPrimitiveType.UINT16:
        case SchemaPrimitiveType.INT32:
        case SchemaPrimitiveType.UINT32:
        case SchemaPrimitiveType.INT64:
        case SchemaPrimitiveType.UINT64: {
          typeInfo = new IntegerTypeInfo();
          return ParseStatus.SUCCESS;
        }
        case SchemaPrimitiveType.SN8:
        case SchemaPrimitiveType.UN8:
        case SchemaPrimitiveType.SN16:
        case SchemaPrimitiveType.UN16:
        case SchemaPrimitiveType.SINGLE:
        case SchemaPrimitiveType.DOUBLE: {
          typeInfo = new FloatTypeInfo();
          return ParseStatus.SUCCESS;
        }
        case SchemaPrimitiveType.CHAR: {
          typeInfo = new CharTypeInfo();
          return ParseStatus.SUCCESS;
        }
        case SchemaPrimitiveType.ENUM: {
          typeInfo = new EnumTypeInfo();
          return ParseStatus.SUCCESS;
        }
        default: throw new ArgumentOutOfRangeException();
      }
    }

    if (typeSymbol.IsString()) {
      typeInfo = new StringTypeInfo();
      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsSequence(out var elementTypeV2, out var sequenceType)) {
      var elementParseStatus = this.ParseTypeSymbol(
          elementTypeV2,
          sequenceType.IsReadOnly(),
          out _);
      if (elementParseStatus != ParseStatus.SUCCESS) {
        typeInfo = default;
        return elementParseStatus;
      }

      typeInfo = new SequenceTypeInfo();
      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsClass() ||
        typeSymbol.IsInterface() ||
        typeSymbol.IsStruct() ||
        typeSymbol is IErrorTypeSymbol) {
      typeInfo = new ContainerTypeInfo();
      return ParseStatus.SUCCESS;
    }

    if (typeSymbol.IsGenericTypeParameter(out _)) {
      typeInfo = new GenericTypeInfo();
      return ParseStatus.SUCCESS;
    }

    typeInfo = default;
    return ParseStatus.NOT_IMPLEMENTED;
  }

  public ITypeInfo AssertParseType(ITypeSymbol typeSymbol) {
    var parseStatus
        = this.ParseTypeSymbol(typeSymbol, true, out var typeInfo);
    if (parseStatus != ParseStatus.SUCCESS) {
      throw new NotImplementedException();
    }

    return typeInfo;
  }

  private bool GetTypeOfMember_(
      ISymbol memberSymbol,
      out ITypeSymbol memberTypeSymbol,
      out bool isMemberReadonly) {
    switch (memberSymbol) {
      case IPropertySymbol propertySymbol: {
        isMemberReadonly = propertySymbol.SetMethod == null;
        memberTypeSymbol = propertySymbol.Type;
        return true;
      }
      case IFieldSymbol fieldSymbol: {
        isMemberReadonly = fieldSymbol.IsReadOnly;
        memberTypeSymbol = fieldSymbol.Type;
        return true;
      }
      default: {
        isMemberReadonly = false;
        memberTypeSymbol = default;
        return false;
      }
    }
  }

  private void ParseNullable_(ref ITypeSymbol typeSymbol) {
    if (!typeSymbol.IsType(typeof(Nullable<>))) {
      return;
    }

    Asserts.True(typeSymbol.IsGeneric(out _, out var genericArguments));
    typeSymbol = genericArguments.ToArray()[0];
  }

  private record BoolTypeInfo : IBoolTypeInfo;
  private class FloatTypeInfo : INumberTypeInfo;
  private record IntegerTypeInfo : IIntegerTypeInfo;
  private record CharTypeInfo : ICharTypeInfo;
  private record StringTypeInfo : IStringTypeInfo;
  private class EnumTypeInfo : IEnumTypeInfo;
  private class ContainerTypeInfo : IContainerTypeInfo;
  private class GenericTypeInfo : IGenericTypeInfo;
  private class SequenceTypeInfo : ISequenceTypeInfo;
}