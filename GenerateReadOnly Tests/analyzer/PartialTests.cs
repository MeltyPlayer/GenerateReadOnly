using NUnit.Framework;

using readOnly.generator;

namespace readOnly.analyzer;

internal class PartialTests {
  [Test]
  public void TestReportsDiagnosticForTypeWithoutPartial() {
    ReadOnlyGeneratorTestUtil.AssertDiagnostics(
        """
        using readOnly;

        namespace foo.bar;

        [GenerateReadOnly]
        public class Foo;
        """,
        Rules.TypeMustBePartial);
  }

  [Test]
  public void TestReportsDiagnosticForParentWithoutPartial() {
    ReadOnlyGeneratorTestUtil.AssertDiagnostics(
        """
        using readOnly;

        namespace foo.bar;

        public class Parent {
          [GenerateReadOnly]
          public partial class Child;
        }
        """,
        Rules.ParentTypeMustBePartial);
  }
}