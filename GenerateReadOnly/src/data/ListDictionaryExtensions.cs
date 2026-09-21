using System.Collections.Generic;
using System.Linq;

namespace readOnly.data;

internal static class ListDictionaryExtensions {
  public static bool TryGetList<TKey, TValue>(
      this ListDictionary<TKey, TValue> impl,
      TKey key,
      out List<TValue> list) {
    if (impl.HasList(key)) {
      list = impl[key];
      return true;
    }

    list = null;
    return false;
  }

  public static IEnumerable<(TKey key, List<TValue> value)> GetPairs<
      TKey, TValue>(this ListDictionary<TKey, TValue> impl)
    => impl.Keys.Select(key => (key, impl[key]));
}