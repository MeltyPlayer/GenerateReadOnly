using readOnly.generator;

namespace build;

[GenerateReadOnly]
public partial class ReadOnlyReturnValue {
  [Const]
  public IFooBar Foo(int bar) => default!;
}