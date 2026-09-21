using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace readOnly.data;

/// <summary>
///   An implementation for a dictionary of lists. Each value added for a key
///   will be stored in that key's corresponding list.
/// </summary>
internal sealed class ListDictionary<TKey, TValue>(
    ConcurrentDictionary<TKey, List<TValue>> impl) {
  public ListDictionary() : this(new ConcurrentDictionary<TKey, List<TValue>>()) { }

  public int TotalCount => impl.Values.Select(list => list.Count).Sum();

  public void Clear() => impl.Clear();
  public void ClearList(TKey key) => impl.TryRemove(key, out _);

  public bool HasList(TKey key) => impl.ContainsKey(key);

  public void Add(TKey key, TValue value)
    => impl.GetOrAdd(key, _ => []).Add(value);

  public void AddRange(TKey key, IEnumerable<TValue> values)
    => impl.GetOrAdd(key, _ => []).AddRange(values);

  public List<TValue> this[TKey key] => impl[key];

  public IEnumerable<TKey> Keys => impl.Keys;
  public IEnumerable<TValue> Values => impl.Values.SelectMany(v => v);
}