using System;

namespace readOnly;

[AttributeUsage(AttributeTargets.GenericParameter |
                AttributeTargets.Parameter |
                AttributeTargets.Property |
                AttributeTargets.Method)]
public class KeepMutableTypeAttribute : Attribute;