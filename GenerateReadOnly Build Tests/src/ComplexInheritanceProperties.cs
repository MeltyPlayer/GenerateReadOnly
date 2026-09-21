using readOnly;


namespace build.complexInheritanceProperties;

[GenerateReadOnly]
public partial interface ITopType<TSelf> where TSelf : ITopType<TSelf> {
  TSelf Data { get; set; }
}

[GenerateReadOnly]
public partial interface ITopType : ITopType<ITopType>;

[GenerateReadOnly]
public partial interface IChildType : ITopType, ITopType<IChildType>;

public partial class ChildTypeImpl : IChildType {
  public IChildType Data { get; set; }
}