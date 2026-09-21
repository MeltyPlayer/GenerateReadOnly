using NUnit.Framework;


namespace readOnly.generator;

internal class ComplexInheritanceTests {
  [Test]
  public void TestGeneratesAsExpected() {
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

        public partial interface ITopType : IReadOnlyTopType;

        public partial interface IReadOnlyTopType : IReadOnlyTopType<IReadOnlyTopType>;

        """,
        """
        #nullable enable

        namespace foo.bar;

        public partial interface IChildType : IReadOnlyChildType;

        public partial interface IReadOnlyChildType : IReadOnlyTopType, IReadOnlyTopType<IReadOnlyChildType>;

        """);
  }
}