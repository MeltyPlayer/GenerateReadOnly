namespace readOnly.util.types;

public enum SequenceType {
  MUTABLE_ARRAY,
  IMMUTABLE_ARRAY,
  MUTABLE_LIST,
  READ_ONLY_LIST,
  MUTABLE_SEQUENCE,
  CONST_LENGTH_MUTABLE_SEQUENCE,
  READ_ONLY_SEQUENCE,
}

public static class SequenceTypeExtensions {
  public static bool IsConstLength(this SequenceType sequenceType)
    => sequenceType switch {
        SequenceType.MUTABLE_ARRAY                 => true,
        SequenceType.IMMUTABLE_ARRAY               => true,
        SequenceType.MUTABLE_LIST                  => false,
        SequenceType.READ_ONLY_LIST                => true,
        SequenceType.MUTABLE_SEQUENCE              => false,
        SequenceType.CONST_LENGTH_MUTABLE_SEQUENCE => true,
        SequenceType.READ_ONLY_SEQUENCE            => true,
    };

  public static bool IsReadOnly(this SequenceType sequenceType)
    => sequenceType switch {
        SequenceType.MUTABLE_ARRAY                 => false,
        SequenceType.IMMUTABLE_ARRAY               => true,
        SequenceType.MUTABLE_LIST                  => false,
        SequenceType.READ_ONLY_LIST                => true,
        SequenceType.MUTABLE_SEQUENCE              => false,
        SequenceType.CONST_LENGTH_MUTABLE_SEQUENCE => false,
        SequenceType.READ_ONLY_SEQUENCE            => true,
    };

  public static bool IsArray(this SequenceType sequenceType)
    => sequenceType is SequenceType.MUTABLE_ARRAY
                       or SequenceType.IMMUTABLE_ARRAY;

  public static bool IsISequence(this SequenceType sequenceType)
    => sequenceType is SequenceType.MUTABLE_SEQUENCE
                       or SequenceType.CONST_LENGTH_MUTABLE_SEQUENCE
                       or SequenceType.READ_ONLY_SEQUENCE;
}