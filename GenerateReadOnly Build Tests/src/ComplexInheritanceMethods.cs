using readOnly;


namespace build.complexInheritanceMethods;

[GenerateReadOnly]
public partial interface ITopType<TSelf> where TSelf : ITopType<TSelf> {
  TSelf Foo(TSelf a);
}

[GenerateReadOnly]
public partial interface ITopType : ITopType<ITopType>;

[GenerateReadOnly]
public partial interface IChildType : ITopType, ITopType<IChildType>;

public partial class ChildTypeImpl : IChildType {
  public IChildType Foo(IChildType a) {
    return default!;
  }
}