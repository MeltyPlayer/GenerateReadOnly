# GenerateReadOnly

![GitHub](https://img.shields.io/github/license/MeltyPlayer/GenerateReadOnly)
[![Nuget](https://img.shields.io/nuget/v/GenerateReadOnly)](https://www.nuget.org/packages/GenerateReadOnly)
![Nuget](https://img.shields.io/nuget/dt/GenerateReadOnly)
![Unit tests](https://github.com/MeltyPlayer/GenerateReadOnly/actions/workflows/dotnet.yml/badge.svg)
[![Coverage Status](https://coveralls.io/repos/github/MeltyPlayer/GenerateReadOnly/badge.svg?service=github)](https://coveralls.io/github/MeltyPlayer/GenerateReadOnly)

## Overview

Roslyn generator that automatically sets up ReadOnly interfaces for annotated types.

## Background

I found myself really wanting to have IReadOnly versions of my types for type-safety, similar to what C# provides with IReadOnlyList, IReadOnlyDictionary, ReadOnlySpan, etc., and similar to what is possible in C++ with const. I started setting up these types manually, but this quickly grew out of hand and was hard to manage.

This library aims to provide this functionality without too much extra boilerplate.

## Usage

### Simple cases, purely read only

To use this generator, simply annotate a type with `readOnly.GenerateReadOnlyAttribute` and mark the type as partial, like so:

**User code:**
```cs
using readOnly;

[GenerateReadOnly]
public partial class Foo {
  public int A { get; set; }

  // This will be ignored
  public int B(int c) {
    ...
  }
}
```

This will generate a read only version of the type that your type will implement automatically. This new type exposes only getters for each property by default:

**Generated code:**
```cs
public partial interface IReadOnlyFoo {
  int A { get; }
}
```

### Const methods

If you have certain methods that don't change state, you can mark them as const similar to C++ by annotating them with `readOnly.ConstAttribute`:

**User code:**
```cs
using readOnly;

[GenerateReadOnly]
public partial class Foo {
  [Const]
  public int B(int c) {
    ...
  }
}
```

Now, the generated read only interface will also include this const method:

**Generated code:**
```cs
public partial interface IReadOnlyFoo {
  int B(int c);
}
```

*Note: This generator does not verify that const methods are pure.*

### Interplay with other readonly types

If your type refers to another type with its own IReadOnly interface, it will automatically use that instead in the generated type:

**User code:**
```cs
using readOnly;

[GenerateReadOnly]
public partial class Foo;

[GenerateReadOnly]
public partial class Bar {
  public Foo Value { get; set; }
}
```

**Generated code:**
```cs
public partial interface IReadOnlyFoo;

public partial interface IReadOnlyBar {
  IReadOnlyFoo Value { get; }
}
```

*This works internally by generating overrides for these read only parent properties/methods that cast the mutable version to the read only version.*

This also supports recursion:

**User code:**
```cs
using readOnly;

[GenerateReadOnly]
public partial class Node {
  public Node Child { get; set; }
}
```

**Generated code:**
```cs
public partial interface IReadOnlyNode {
  IReadOnlyNode Child { get; }
}
```

#### Forcing mutability

If you don't want a type to be swapped out for its IReadOnly counterpart, you can force it to be kept by annotating the type with `readOnly.KeepMutableTypeAttribute`:

**User code:**
```cs
using readOnly;

[GenerateReadOnly]
public partial class Foo;

[GenerateReadOnly]
public partial class Bar {
  [Const]
  [KeepMutableType]
  public Foo Value { get; set; }

  [Const]
  [KeepMutableType]
  public Foo KeepInReturn() {
    ...
  }

  [Const]
  public void KeepInParam([KeepMutableType] Foo value) {
    ...
  }
}
```

**Generated code:**
```cs
public partial interface IReadOnlyFoo;

public partial interface IReadOnlyBar {
  Foo Value { get; }

  Foo KeepInReturn();

  void KeepInParam(Foo value);
}
```

### Inheritance

If a type with a read only interface inherits from another class with a read only interface, the read only interfaces will also have the same inheritance:

**User code:**
```cs
using readOnly;

[GenerateReadOnly]
public partial class Parent;

[GenerateReadOnly]
public partial class Child : Parent;
```

**Generated code:**
```cs
public partial interface IReadOnlyParent;

public partial interface IReadOnlyChild : IReadOnlyParent;
```
