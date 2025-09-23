using System.Collections.Generic;

using readOnly.generator;


namespace build;

[GenerateReadOnly]
public partial class ReadOnlyEnumerableReturnValue {
  [Const]
  public IEnumerable<IFooBar> Foo(int bar) => default!;
}