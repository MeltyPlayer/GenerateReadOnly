using System;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#pragma warning disable CS8604


namespace readOnly.binary;

internal static class BinarySchemaTestUtil {
  public static CSharpCompilation Compilation =
      CSharpCompilation
          .Create("test")
          .AddReferences(
              ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
              .Split(Path.PathSeparator)
              .Select(path => MetadataReference.CreateFromFile(path)));
}