using NUnit.Framework;


namespace readOnly.generator;

internal class KeywordTests {
  [Test]
  public void TestKeywords() {
    ReadOnlyGeneratorTestUtil.AssertGenerated(
        """
        using readOnly;
        
        namespace @const;

        [GenerateReadOnly]
        public partial class @void<@double> where @double : struct {
          [Const]
          public @void @int<@short>(@void @bool) where @short : @void { }
          
          public @void @float { get; }
        }
        """,
        """
        #nullable enable

        namespace @const;

        public partial class @void<@double> : IReadOnlyvoid<@double> {
          @void IReadOnlyvoid<@double>.@float => @float;
          @void IReadOnlyvoid<@double>.@int<@short>(@void @bool) => @int<@short>(@bool);
        }

        public partial interface IReadOnlyvoid<out @double> where @double : struct {
          public @void @float { get; }
          public @void @int<@short>(@void @bool) where @short : @void;
        }

        """);
  }
}