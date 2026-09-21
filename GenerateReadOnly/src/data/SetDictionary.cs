using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace readOnly.data;

/// <summary>
///   An implementation for a dictionary of sets. Each value added for a key
///   will be stored in that key's corresponding set.
/// </summary>
public sealed class SetDictionary<TKey, TValue>(
    ConcurrentDictionary<TKey, ISet<TValue>> impl) {
  public SetDictionary() : this(
      new ConcurrentDictionary<TKey, ISet<TValue>>()) { }

  public void Clear() => impl.Clear();

  public int Count => impl.Values.Select(list => list.Count).Sum();

  public void Add(TKey key, TValue value)
    => impl.GetOrAdd(key, _ => new HashSet<TValue>()).Add(value);

  public ISet<TValue> this[TKey key] => impl[key];

  public IEnumerable<(TKey key, ISet<TValue> value)> GetPairs()
    => impl.Keys.Select(key => (key, impl[key]));
}