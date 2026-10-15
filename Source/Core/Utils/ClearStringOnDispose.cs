using System;
using System.Text;
using JetBrains.Annotations;

namespace CoreLib;

/// <summary>
/// Clears the string builder when this object goes out of scope.
/// </summary>
[PublicAPI]
public readonly struct ClearStringOnDispose(StringBuilder stringBuilder) : IDisposable
{
  void IDisposable.Dispose()
  {
    stringBuilder?.Clear();
  }
}