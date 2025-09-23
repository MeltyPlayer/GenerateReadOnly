using System;

namespace readOnly;

[AttributeUsage(AttributeTargets.Class |
                AttributeTargets.Interface |
                AttributeTargets.Struct)]
public class GenerateReadOnlyAttribute : Attribute;