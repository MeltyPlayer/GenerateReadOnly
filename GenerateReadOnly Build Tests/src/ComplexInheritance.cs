using readOnly;


namespace build;

[GenerateReadOnly]
public partial interface ITopType<TSelf> where TSelf : ITopType<TSelf> {
  TSelf Data { get; set; }
}

[GenerateReadOnly]
public partial interface ITopType : ITopType<ITopType> {
  new ITopType Data { get; set; }

  ITopType ITopType<ITopType>.Data {
    get => this.Data;
    set => this.Data = value;
  }

  IReadOnlyTopType IReadOnlyTopType<IReadOnlyTopType>.Data => this.Data;
}

[GenerateReadOnly]
public partial interface IChildType : ITopType, ITopType<IChildType> {
  new IChildType Data { get; set; }

  IChildType ITopType<IChildType>.Data {
    get => this.Data;
    set => this.Data = value;
  }

  IReadOnlyChildType IReadOnlyTopType<IReadOnlyChildType>.Data => this.Data;
}

public partial class ChildTypeImpl : IChildType {
  public IChildType Data { get; set; }
}