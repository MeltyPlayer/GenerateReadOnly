using System.Collections.Generic;


namespace readOnly;

[GenerateReadOnly]
public partial class MutableEnumerableReturnValue {
  [Const]
  [KeepMutableType]
  public IEnumerable<IFooBar> Foo(int bar) => default!;
}