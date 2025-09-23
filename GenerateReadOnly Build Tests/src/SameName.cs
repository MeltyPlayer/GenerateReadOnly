using readOnly;

namespace build;

[GenerateReadOnly]
public partial interface ISameName;

[GenerateReadOnly]
public partial interface ISameName<T> : ISameName;