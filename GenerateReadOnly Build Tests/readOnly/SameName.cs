using schema.generator;

namespace readOnly;

[GenerateReadOnly]
public partial interface ISameName;

[GenerateReadOnly]
public partial interface ISameName<T> : ISameName;