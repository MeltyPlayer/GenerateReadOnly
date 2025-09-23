using System.Collections.Generic;

using readOnly.generator;


namespace build;

[GenerateReadOnly]
public partial interface IDoesNotAddInVariance<T> {
  [Const]
  public bool TryGetSet(out ISet<T> list);
}

[GenerateReadOnly]
public partial interface IOkayToAddVariance<T> {
  [Const]
  public void PassGetSet(ISet<T> list);
}

[GenerateReadOnly]
public partial interface IFinCollection<out T>;

[GenerateReadOnly]
public partial interface ISubTypeDictionary<TKey, TValue>
    : IFinCollection<(TKey Key, TValue Value)> {
  [Const]
  TValueSub Get<TValueSub>(TKey key) where TValueSub : TValue;
}