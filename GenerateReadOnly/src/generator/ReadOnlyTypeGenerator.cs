using System;
using System.Collections.Generic;
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
          var interfaceName = typeSymbol.GetConstInterfaceName();
          var constMembers = typeSymbol.GetConstMembers().ToArray();

          var levelsAndMembers
              = new List<(string interfaceName, IMethodSymbol[])>();
          levelsAndMembers.Add((interfaceName, constMembers));

          /*var parentConstTypes =
              GetDirectBaseTypeAndInterfaces_(typeSymbol)
                  .Where(i => i.HasAttribute<GenerateReadOnlyAttribute>())
                  .Select(i => typeSymbol
                              .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                                  i,
                                  semanticModel,
                                  syntax))
                  .ToArray();*/

          // Class
          {
            var blockPrefix =
                typeSymbol.GetQualifiersAndNameAndGenericParametersFor() +
                " : " +
                typeSymbol.GetNameAndGenericParametersFor(interfaceName);

            if (constMembers.Length == 0) {
              sw.Write(blockPrefix).WriteLine(";");
            } else {
              sw.EnterBlock(blockPrefix);

              foreach (var (currentInterfaceName, currentConstMembers) in
                       levelsAndMembers) {
                WriteMembers_(sw,
                              typeSymbol,
                              currentConstMembers,
                              semanticModel,
                              syntax,
                              currentInterfaceName);
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

            var blockPrefix = interfaceName;
            blockPrefix
                += typeSymbol
                    .GetGenericParametersWithVarianceForReadOnlyVersion(
                        constMembers);
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

            if (constMembers.Length == 0) {
              sw.Write(blockPrefix).WriteLine(";");
            } else {
              sw.EnterBlock(blockPrefix);
              WriteMembers_(sw,
                            typeSymbol,
                            constMembers,
                            semanticModel,
                            syntax);
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

  private static void WriteMembers_(
      ISourceWriter sw,
      INamedTypeSymbol typeSymbol,
      IReadOnlyList<IMethodSymbol> constMembers,
      SemanticModel semanticModel,
      TypeDeclarationSyntax syntax,
      string? interfaceName = null) {
    foreach (var memberSymbol in constMembers) {
      var memberTypeSymbol = memberSymbol.ReturnType;

      if (interfaceName == null) {
        sw.Write(SymbolTypeUtil.AccessibilityToModifier(
                     typeSymbol.DeclaredAccessibility))
          .Write(" ");
      }

      IPropertySymbol? associatedPropertySymbol
          = memberSymbol.AssociatedSymbol as IPropertySymbol;
      sw.Write(
            typeSymbol
                .GetQualifiedNameAndGenericsOrReadOnlyFromCurrentSymbol(
                    memberTypeSymbol,
                    semanticModel,
                    syntax,
                    (ISymbol?) associatedPropertySymbol ?? memberSymbol))
        .Write(" ");

      if (interfaceName != null) {
        sw.Write(interfaceName)
          .Write(typeSymbol.GetGenericParameters())
          .Write(".");
      }

      // Property
      if (memberSymbol.IsPropertyGetter(out var propertyAccessName)) {
        var isIndexer
            = memberSymbol.IsIndexer(out var indexerParameterSymbols);

        if (!isIndexer) {
          propertyAccessName = propertyAccessName.EscapeKeyword();
          sw.Write(memberSymbol.Name.Substring(4).EscapeKeyword());
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
                       associatedPropertySymbol,
                       memberSymbol.ReturnType,
                       semanticModel,
                       syntax))
            .Write(propertyAccessName);

          if (isIndexer) {
            sw.Write("[");
            for (var i = 0; i < memberSymbol.Parameters.Length; ++i) {
              if (i > 0) {
                sw.Write(", ");
              }

              var parameterSymbol = memberSymbol.Parameters[i];
              sw.Write(parameterSymbol.Name.EscapeKeyword());
            }

            sw.Write("]");
          }

          sw.WriteLine(";");
        }
      }
      // Method
      else {
        var accessName = memberSymbol.Name.EscapeKeyword();
        sw.Write(accessName);
        sw.Write(memberSymbol.TypeParameters
                             .GetGenericParameters());
        sw.Write("(");

        for (var i = 0; i < memberSymbol.Parameters.Length; ++i) {
          if (i > 0) {
            sw.Write(", ");
          }

          var parameterSymbol = memberSymbol.Parameters[i];
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
                       memberSymbol.TypeParameters,
                       semanticModel,
                       syntax));
        }

        if (interfaceName == null) {
          sw.WriteLine(";");
        } else {
          sw.Write(" => ")
            .Write(typeSymbol.GetCStyleCastToReadOnlyIfNeeded(
                       memberSymbol,
                       memberSymbol.ReturnType,
                       semanticModel,
                       syntax))
            .Write(accessName)
            .Write(memberSymbol.TypeParameters.GetGenericParameters())
            .Write("(");
          for (var i = 0; i < memberSymbol.Parameters.Length; ++i) {
            if (i > 0) {
              sw.Write(", ");
            }

            var parameterSymbol = memberSymbol.Parameters[i];

            var refKindString = parameterSymbol.RefKind.GetRefKindString();
            if (refKindString.Length > 0) {
              sw.Write(refKindString).Write(" ");
            }

            sw.Write(parameterSymbol.Name.EscapeKeyword());
          }

          sw.WriteLine(");");
        }
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