using NUnit.Framework;


namespace readOnly.generator;

internal class SameNameTests {
  [Test]
  public void TestSameName() {
    ReadOnlyGeneratorTestUtil.AssertGenerated(
        """
        using readOnly.generator;

        namespace foo.bar;
        
        [GenerateReadOnly]
        public partial interface ISameName;
      
        [GenerateReadOnly]
        public partial interface ISameName<T> : ISameName;
        """,
        """
        #nullable enable

        namespace foo.bar;
        
        public partial interface ISameName : IReadOnlySameName;
        
        public partial interface IReadOnlySameName;

        """,
        """
        #nullable enable

        namespace foo.bar;
        
        public partial interface ISameName<T> : IReadOnlySameName<T>;
        
        public partial interface IReadOnlySameName<out T> : IReadOnlySameName;

        """);
  }
}