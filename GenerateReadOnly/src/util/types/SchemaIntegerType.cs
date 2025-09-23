using System;

namespace readOnly.util.types;

public enum SchemaIntegerType {
  UNDEFINED,

  BYTE,
  SBYTE,
  INT16,
  UINT16,
  INT24,
  UINT24,
  INT32,
  UINT32,
  INT64,
  UINT64
}

public static class SchemaIntegerTypeExtensions {
  public static SchemaNumberType AsNumberType(this SchemaIntegerType type)
    => type switch {
        SchemaIntegerType.SBYTE => SchemaNumberType.SBYTE,
        SchemaIntegerType.BYTE => SchemaNumberType.BYTE,
        SchemaIntegerType.INT16 => SchemaNumberType.INT16,
        SchemaIntegerType.UINT16 => SchemaNumberType.UINT16,
        SchemaIntegerType.INT24 => SchemaNumberType.INT24,
        SchemaIntegerType.UINT24 => SchemaNumberType.UINT24,
        SchemaIntegerType.INT32 => SchemaNumberType.INT32,
        SchemaIntegerType.UINT32 => SchemaNumberType.UINT32,
        SchemaIntegerType.INT64 => SchemaNumberType.INT64,
        SchemaIntegerType.UINT64 => SchemaNumberType.UINT64,
        SchemaIntegerType.UNDEFINED => SchemaNumberType.UNDEFINED,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}