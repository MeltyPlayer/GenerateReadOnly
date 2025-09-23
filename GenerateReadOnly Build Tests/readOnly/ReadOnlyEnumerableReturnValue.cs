using System.Collections.Generic;

using schema.generator;


namespace readOnly;

[GenerateReadOnly]
public partial class ReadOnlyEnumerableReturnValue {
  [Const]
  public IEnumerable<IFooBar> Foo(int bar) => default!;
}