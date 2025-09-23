using System.Collections.Generic;


namespace readOnly;

[GenerateReadOnly]
public partial class ReadOnlyEnumerableReturnValue {
  [Const]
  public IEnumerable<IFooBar> Foo(int bar) => default!;
}