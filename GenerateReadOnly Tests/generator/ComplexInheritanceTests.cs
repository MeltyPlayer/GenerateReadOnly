using NUnit.Framework;


namespace readOnly.generator;

internal class ComplexInheritanceTests {
  [Test]
  public void TestGeneratesOverlappingPropertiesAsExpected() {
    ReadOnlyGeneratorTestUtil.AssertGenerated(
        """
        using readOnly;

        namespace foo.bar;

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
        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface ITopType<TSelf> : IReadOnlyTopType<TSelf> {
          TSelf IReadOnlyTopType<TSelf>.Data => Data;
        }

        public partial interface IReadOnlyTopType<out TSelf> where TSelf : IReadOnlyTopType<TSelf> {
          public TSelf Data { get; }
        }

        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface ITopType : IReadOnlyTopType {
          ITopType Data { get; set; }
          IReadOnlyTopType IReadOnlyTopType<IReadOnlyTopType>.Data => Data;
          ITopType ITopType<ITopType>.Data {
            get => Data;
            set => Data = value;
          }
        }

        public partial interface IReadOnlyTopType : IReadOnlyTopType<IReadOnlyTopType>;

        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface IChildType : IReadOnlyChildType {
          IChildType Data { get; set; }
          IReadOnlyChildType IReadOnlyTopType<IReadOnlyChildType>.Data => Data;
          IReadOnlyTopType IReadOnlyTopType<IReadOnlyTopType>.Data => Data;
          IChildType ITopType<IChildType>.Data {
            get => Data;
            set => Data = value;
          }
          ITopType ITopType<ITopType>.Data {
            get => Data;
            set => Data = value;
          }
        }

        public partial interface IReadOnlyChildType : IReadOnlyTopType, IReadOnlyTopType<IReadOnlyChildType>;

        """);
  }

  [Test]
  public void TestGeneratesOverlappingMethodsAsExpected() {
    ReadOnlyGeneratorTestUtil.AssertGenerated(
        """
        using readOnly;

        namespace foo.bar;

        [GenerateReadOnly]
        public partial interface ITopType<TSelf> where TSelf : ITopType<TSelf> {
          TSelf Foo(TSelf a);
        }

        [GenerateReadOnly]
        public partial interface ITopType : ITopType<ITopType>;

        [GenerateReadOnly]
        public partial interface IChildType : ITopType, ITopType<IChildType>;

        public partial class ChildTypeImpl : IChildType {
          public IChildType Foo(IChildType a);
        }
        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface ITopType<TSelf> : IReadOnlyTopType<TSelf>;

        public partial interface IReadOnlyTopType<out TSelf> where TSelf : IReadOnlyTopType<TSelf>;

        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface ITopType : IReadOnlyTopType;

        public partial interface IReadOnlyTopType : IReadOnlyTopType<IReadOnlyTopType>;

        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface IChildType : IReadOnlyChildType {
          IChildType ITopType<IChildType>.Foo(IChildType a) => Foo(a);
          ITopType ITopType<ITopType>.Foo(ITopType a) => Foo(a);
        }

        public partial interface IReadOnlyChildType : IReadOnlyTopType, IReadOnlyTopType<IReadOnlyChildType>;

        """);
  }
}