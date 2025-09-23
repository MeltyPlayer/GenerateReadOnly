using System.Collections.Generic;

using readOnly.generator;


namespace build;

[GenerateReadOnly]
public partial class MutableEnumerableReturnValue {
  [Const]
  [KeepMutableType]
  public IEnumerable<IFooBar> Foo(int bar) => default!;
}