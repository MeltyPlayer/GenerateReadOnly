using System.Collections.Generic;

using schema.generator;


namespace readOnly;

[GenerateReadOnly]
public partial class MutableEnumerableReturnValue {
  [Const]
  [KeepMutableType]
  public IEnumerable<IFooBar> Foo(int bar) => default!;
}