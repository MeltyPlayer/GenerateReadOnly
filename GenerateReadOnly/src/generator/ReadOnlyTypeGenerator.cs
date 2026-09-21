using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using readOnly.util.generators;
using readOnly.util.symbols;
using readOnly.util.text;
using readOnly.util.types;


namespace readOnly.generator;

[Generator(LanguageNames.CSharp)]
public class ReadOnlyTypeGenerator
    : BNamedTypesWithAttributeGenerator<GenerateReadOnlyAttribute> {
  internal override bool FilterNamedTypesBeforeGenerating(
      TypeDeclarationSyntax syntax,
      INamedTypeSymbol symbol) => true;

  internal override IEnumerable<(string fileName, string source)>
      GenerateSourcesForNamedType(INamedTypeSymbol symbol,
                                  SemanticModel semanticModel,
                                  TypeDeclarationSyntax syntax) {
    yield return ($"{symbol.GetUniqueNameForGenerator()}_readOnly.g",
                  GenerateSourceForNamedType(
                      symbol,
                      semanticModel,
                      syntax));
  }

  private class MemberNameAndParameters : IComparable<MemberNameAndParameters> {
    public static MemberNameAndParameters From(ISymbol memberSymbol)
      => new() {
          Name = memberSymbol.Name,
          Parameters = memberSymbol switch {
              IMethodSymbol methodSymbol     => methodSymbol.Parameters,
              IPropertySymbol propertySymbol => propertySymbol.Parameters,
          },
      };

    private string Name { get; set; }
    private ImmutableArray<IParameterSymbol> Parameters { get; set; }

    public bool Equals(object? otherObj) {
      if (otherObj is not MemberNameAndParameters other) {
        return false;
      }

      return this.Name == other.Name &&
             this.Parameters.SequenceEqual(other.Parameters);
    }

    public int CompareTo(MemberNameAndParameters? other) {
      var stringCompare = this.Name.CompareTo(other.Name);
      if (stringCompare != 0) {
        return stringCompare;
      }

      var paramsLengthCompare
          = this.Parameters.Length.CompareTo(other.Parameters.Length);
      if (paramsLengthCompare != 0) {
        return paramsLengthCompare;
      }

      foreach (var (lhs, rhs) in this.Parameters.Zip(
                   other.Parameters,
                   (lhs, rhs) => (lhs, rhs))) {
        var paramEqual = lhs == rhs;
        if (!paramEqual) {
          return 1;
        }
      }

      return 0;
    }
  }

  public static string GenerateSourceForNamedType(
      INamedTypeSymbol typeSymbol,
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax) {
    var sb = new StringBuilder();
    using var sw = new SourceWriter(new StringWriter(sb));

    sw.WriteLine("#nullable enable")
      .WriteLine()
      .WriteNamespaceAndParentTypeBlocks(
          typeSymbol,
          () => {
            var myMembers = typeSymbol.ParseMembers().ToArray();
            var overlapMembers
                = new Dictionary<MemberNameAndParameters, List<(bool makeConst,
                    string
                    fullyQualifiedParentName, ISymbol propertyOrMethod)>>();

            /*var parentConstTypes =
                GetDirectBaseTypeAndInterfaces_(typeSymbol)
                    .Where(i => i.HasAttribute<GenerateReadOnlyAttribute>())
                    .Select(i => typeSymbol
                                .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                                    i,
                                    semanticModel,
                                    syntax))
                    .ToArray();*/

            var myReadOnlyInterfaceName = typeSymbol.GetConstInterfaceName();
            var myConstMembers = myMembers.WhereApplicableForConst().ToArray();
            foreach (var myConstMember in myConstMembers) {
              overlapMembers.Add(MemberNameAndParameters.From(myConstMember),
              [
                  (true, myReadOnlyInterfaceName, myConstMember)
              ]);
            }

            foreach (var parentType in GetDirectBaseTypeAndInterfaces_(
                             typeSymbol)
                         .Where(i => i
                                    .HasAttribute<
                                        GenerateReadOnlyAttribute>())) {
              var parentFullyQualifiedName = typeSymbol
                  .GetQualifiedNameAndGenericsFromCurrentSymbol(
                      parentType,
                      semanticModel,
                      syntax);
              var parentReadOnlyInterfaceFullyQualifiedName = typeSymbol
                  .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                      parentType,
                      semanticModel,
                      syntax);

              var parentMembers = parentType.ParseMembers().ToArray();
              foreach (var parentMember in parentMembers) {
                if (!overlapMembers.TryGetValue(
                        MemberNameAndParameters.From(parentMember),
                        out var overlapMember)) {
                  continue;
                }

                overlapMember.Add((false, parentFullyQualifiedName,
                                   parentMember));
                overlapMember.Add(
                    (true, parentReadOnlyInterfaceFullyQualifiedName,
                     parentMember));
              }
            }

            // Class
            {
              var blockPrefix =
                  typeSymbol.GetQualifiersAndNameAndGenericParametersFor() +
                  " : " +
                  typeSymbol.GetNameAndGenericParametersFor(
                      myReadOnlyInterfaceName);

              if (overlapMembers.Count == 0) {
                sw.Write(blockPrefix).WriteLine(";");
              } else {
                sw.EnterBlock(blockPrefix);

                foreach (var overlapMember in overlapMembers
                                              .OrderBy(kvp => kvp.Key)
                                              .Select(kvp => kvp.Value)) {
                  foreach (var (makeConst, fullyQualifiedParentName,
                               memberSymbol) in
                           overlapMember
                               .OrderBy(m => m.fullyQualifiedParentName)) {
                    WriteMember_(
                        sw,
                        typeSymbol,
                        memberSymbol,
                        makeConst,
                        semanticModel,
                        syntax,
                        fullyQualifiedParentName);
                  }
                }

                sw.ExitBlock();
              }
            }
            sw.WriteLine("");

            // Interface
            {
              sw.Write(
                  SymbolTypeUtil.AccessibilityToModifier(
                      typeSymbol.DeclaredAccessibility));
              sw.Write(" partial interface ");

              var blockPrefix = myReadOnlyInterfaceName;
              blockPrefix
                  += typeSymbol
                      .GetGenericParametersWithVarianceForReadOnlyVersion(
                          myConstMembers);
              var parentConstNames =
                  GetDirectBaseTypeAndInterfaces_(typeSymbol)
                      .Where(i => i.HasAttribute<GenerateReadOnlyAttribute>() ||
                                  IsTypeAlreadyConst_(i))
                      .Select(i => typeSymbol
                                  .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                                      i,
                                      semanticModel,
                                      syntax))
                      .ToArray();
              if (parentConstNames.Length > 0) {
                blockPrefix += " : " + string.Join(", ", parentConstNames);
              }

              blockPrefix += typeSymbol.GetTypeConstraintsOrReadonly(
                  typeSymbol.TypeParameters,
                  semanticModel,
                  syntax);

              if (myConstMembers.Length == 0) {
                sw.Write(blockPrefix).WriteLine(";");
              } else {
                sw.EnterBlock(blockPrefix);
                foreach (var constMember in
                         myConstMembers.OrderBy(m => m.Name)) {
                  WriteMember_(sw,
                               typeSymbol,
                               constMember,
                               true,
                               semanticModel,
                               syntax);
                }

                sw.ExitBlock();
              }
            }
          });

    return sb.ToString();
  }

  private static bool IsTypeAlreadyConst_(INamedTypeSymbol typeSymbol) {
    if (typeSymbol.IsType(typeof(IEnumerable<>))) {
      return true;
    }

    foreach (var parsedMember in TypeInfoParser.ParseMembers(
                 typeSymbol)) {
      var (parseStatus, memberSymbol) = parsedMember;
      if (parseStatus ==
          TypeInfoParser.ParseStatus.NOT_A_FIELD_OR_PROPERTY_OR_METHOD) {
        continue;
      }

      if (memberSymbol.DeclaredAccessibility is not (Accessibility.Public
          or Accessibility.Internal)) {
        continue;
      }

      if (memberSymbol is IFieldSymbol) {
        continue;
      }

      if (memberSymbol is IPropertySymbol { IsReadOnly: true }) {
        continue;
      }

      if (memberSymbol is IMethodSymbol &&
          (memberSymbol.Name.StartsWith("get_") ||
           memberSymbol.HasAttribute<ConstAttribute>())) {
        continue;
      }

      return false;
    }

    return GetDirectBaseTypeAndInterfaces_(typeSymbol)
        .All(IsTypeAlreadyConst_);
  }

  private static void WriteMember_(
      ISourceWriter sw,
      INamedTypeSymbol typeSymbol,
      ISymbol memberSymbol,
      bool makeConst,
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax,
      string? interfaceName = null) {
    var returnType = memberSymbol switch {
        IMethodSymbol methodSymbol     => methodSymbol.ReturnType,
        IPropertySymbol propertySymbol => propertySymbol.Type,
    };

    if (interfaceName == null) {
      sw.Write(SymbolTypeUtil.AccessibilityToModifier(
                   typeSymbol.DeclaredAccessibility))
        .Write(" ");
    }

    sw.Write(
          typeSymbol
              .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                  returnType,
                  semanticModel,
                  syntax,
                  memberSymbol))
      .Write(" ");

    if (interfaceName != null) {
      sw.Write(interfaceName)
        .Write(typeSymbol.GetGenericParameters())
        .Write(".");
    }

    // Property
    switch (memberSymbol) {
      case IPropertySymbol propertySymbol: {
        var isIndexer = propertySymbol.IsIndexer;
        var indexerParameterSymbols = propertySymbol.Parameters;

        var propertyAccessName = propertySymbol.Name;
        if (!isIndexer) {
          propertyAccessName = propertyAccessName.EscapeKeyword();
          sw.Write(propertyAccessName);
        } else {
          propertyAccessName = "this";
          sw.Write("this[");
          for (var i = 0; i < indexerParameterSymbols.Length; ++i) {
            if (i > 0) {
              sw.Write(", ");
            }

            var parameterSymbol = indexerParameterSymbols[i];
            sw.Write(
                  typeSymbol.GetQualifiedNameAndGenericsFromCurrentSymbol(
                      parameterSymbol.Type,
                      semanticModel,
                      syntax,
                      parameterSymbol))
              .Write(" ")
              .Write(parameterSymbol.Name.EscapeKeyword());
          }

          sw.Write("]");
        }

        if (interfaceName == null) {
          sw.WriteLine(" { get; }");
        } else {
          sw.Write(" => ")
            .Write(typeSymbol.GetCStyleCastToReadOnlyIfNeeded(
                       memberSymbol,
                       returnType,
                       semanticModel,
                       syntax))
            .Write(propertyAccessName);

          if (isIndexer) {
            sw.Write("[");
            for (var i = 0; i < indexerParameterSymbols.Length; ++i) {
              if (i > 0) {
                sw.Write(", ");
              }

              var parameterSymbol = indexerParameterSymbols[i];
              sw.Write(parameterSymbol.Name.EscapeKeyword());
            }

            sw.Write("]");
          }

          sw.WriteLine(";");
        }

        break;
      }
      case IMethodSymbol methodSymbol: {
        var accessName = memberSymbol.Name.EscapeKeyword();
        sw.Write(accessName);
        sw.Write(methodSymbol.TypeParameters.GetGenericParameters());
        sw.Write("(");

        for (var i = 0; i < methodSymbol.Parameters.Length; ++i) {
          if (i > 0) {
            sw.Write(", ");
          }

          var parameterSymbol = methodSymbol.Parameters[i];
          if (parameterSymbol.IsParams) {
            sw.Write("params ");
          }

          var refKindString = parameterSymbol.RefKind.GetRefKindString();
          if (refKindString.Length > 0) {
            sw.Write(refKindString).Write(" ");
          }

          sw.Write(
                typeSymbol.GetQualifiedNameAndGenericsFromCurrentSymbol(
                    parameterSymbol.Type,
                    semanticModel,
                    syntax,
                    parameterSymbol))
            .Write(" ")
            .Write(parameterSymbol.Name.EscapeKeyword());


          if (interfaceName == null &&
              parameterSymbol.HasExplicitDefaultValue) {
            var defaultValueType = parameterSymbol.Type.UnwrapNullable();

            sw.Write(" = ");

            var explicitDefaultValue = parameterSymbol.ExplicitDefaultValue;
            if (defaultValueType.IsEnum() &&
                explicitDefaultValue != null) {
              sw.Write(
                  $"({typeSymbol.GetQualifiedNameFromCurrentSymbol(defaultValueType)}) {explicitDefaultValue}");
            } else {
              switch (explicitDefaultValue) {
                case null:
                  sw.Write("null");
                  break;
                case char:
                  sw.Write($"'{explicitDefaultValue}'");
                  break;
                case string:
                  sw.Write($"\"{explicitDefaultValue}\"");
                  break;
                case bool boolValue:
                  sw.Write(boolValue ? "true" : "false");
                  break;
                default:
                  sw.Write(explicitDefaultValue.ToString());
                  break;
              }
            }
          }
        }

        sw.Write(")");

        if (interfaceName == null) {
          sw.Write(typeSymbol.GetTypeConstraintsOrReadonly(
                       methodSymbol.TypeParameters,
                       semanticModel,
                       syntax));
        }

        if (interfaceName == null) {
          sw.WriteLine(";");
        } else {
          sw.Write(" => ")
            .Write(typeSymbol.GetCStyleCastToReadOnlyIfNeeded(
                       memberSymbol,
                       methodSymbol.ReturnType,
                       semanticModel,
                       syntax))
            .Write(accessName)
            .Write(methodSymbol.TypeParameters.GetGenericParameters())
            .Write("(");
          for (var i = 0; i < methodSymbol.Parameters.Length; ++i) {
            if (i > 0) {
              sw.Write(", ");
            }

            var parameterSymbol = methodSymbol.Parameters[i];

            var refKindString = parameterSymbol.RefKind.GetRefKindString();
            if (refKindString.Length > 0) {
              sw.Write(refKindString).Write(" ");
            }

            sw.Write(parameterSymbol.Name.EscapeKeyword());
          }

          sw.WriteLine(");");
        }

        break;
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol>
      GetDirectBaseTypeAndInterfaces_(
          INamedTypeSymbol symbol) {
    var baseType = symbol.BaseType;
    if (baseType != null &&
        !baseType.IsType<object>() &&
        !baseType.IsType<ValueType>()) {
      yield return baseType;
    }

    var parentInterfaces
        = symbol.Interfaces
                .Where(i => !(i.IsType(typeof(IEquatable<>)) &&
                              i.TypeArguments[0]
                               .GetFullyQualifiedNamespace() ==
                              symbol.GetFullyQualifiedNamespace() &&
                              i.TypeArguments[0].Name == symbol.Name));

    foreach (var iface in parentInterfaces) {
      yield return iface;
    }
  }
}

internal class GeneratorUtilContext(
    IReadOnlyDictionary<(string name, int arity), IEnumerable<string>?>
        knownNamespaces) {
  public IReadOnlyDictionary<(string name, int arity), IEnumerable<string>?>
      KnownNamespaces { get; } = knownNamespaces;
}