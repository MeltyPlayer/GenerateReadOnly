using readOnly.generator;

namespace build;

[GenerateReadOnly]
public partial class MutableReturnValue {
  [Const]
  [KeepMutableType]
  public IFooBar Foo(int bar) => default!;
}