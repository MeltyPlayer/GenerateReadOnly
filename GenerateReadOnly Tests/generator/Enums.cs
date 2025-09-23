namespace readOnly.generator;

internal enum SomeEnum {
  FOO = 123,
}

[GenerateReadOnly]
internal partial interface ISomeWrapper {
  [Const]
  void Foo(SomeEnum? bar = SomeEnum.FOO);
}