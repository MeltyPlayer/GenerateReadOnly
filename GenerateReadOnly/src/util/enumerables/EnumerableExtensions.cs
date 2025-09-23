using System.Collections.Generic;


namespace schema.util.enumerables;

public static class EnumerableExtensions {
  public static IEnumerable<T> Yield<T>(this T value) {
    yield return value;
  }
}