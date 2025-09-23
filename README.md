# GenerateReadOnly

![GitHub](https://img.shields.io/github/license/MeltyPlayer/GenerateReadOnly)
[![Nuget](https://img.shields.io/nuget/v/GenerateReadOnly)](https://www.nuget.org/packages/GenerateReadOnly)
![Nuget](https://img.shields.io/nuget/dt/GenerateReadOnly)
![Unit tests](https://github.com/MeltyPlayer/GenerateReadOnly/actions/workflows/dotnet.yml/badge.svg)
[![Coverage Status](https://coveralls.io/repos/github/MeltyPlayer/GenerateReadOnly/badge.svg?service=github)](https://coveralls.io/github/MeltyPlayer/GenerateReadOnly)

## Overview

Roslyn generator that automatically implements ReadOnly interfaces for annotated types.

## Usage

### Simple cases, purely read only

To use this generator, simply annotate a type with `readOnly.GenerateReadOnlyAttribute` and mark the type as partial, like so:

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
```cs
public partial interface IReadOnlyFoo {
  int A { get; }
}
```

### Const methods

If you have certain methods that don't change state, you can mark them as const similar to C++ by annotating them with `readOnly.ConstAttribute`:

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
```cs
public partial interface IReadOnlyFoo {
  int B(int c);
}
```
