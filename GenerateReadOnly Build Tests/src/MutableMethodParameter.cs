using readOnly;

namespace build;

[GenerateReadOnly]
public partial interface MutableMethodParameter {
  [Const]
  public int Foo([KeepMutableType] IFooBar bar) => 0;
}