using System;
using JetBrains.Annotations;

namespace CoreLib;

[PublicAPI]
public static class TypeUtils
{
  /// <summary>
  /// Returns <see langword="true"/> if the specified <paramref name="type"/> is a built-in numeric type.
  /// </summary>
  /// <param name="type">The type to test. </param>
  /// <returns>
  /// <see langword="true"/> for <see cref="byte"/>, <see cref="sbyte"/>, <see cref="ushort"/>,
  /// <see cref="uint"/>, <see cref="ulong"/>, <see cref="short"/>, <see cref="int"/>,
  /// <see cref="long"/>, <see cref="decimal"/>, <see cref="double"/>, <see cref="float"/>; otherwise <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This does not treat <see cref="char"/> or <see cref="bool"/> as numeric.
  /// Nullable numeric types (e.g., <see cref="Nullable{T}"/> where <c>T</c> is numeric) return <see langword="false"/>.
  /// </remarks>
  /// <exception cref="ArgumentNullException">If the type argument is null</exception>
  public static bool IsNumericType([NotNull] this Type type)
  {
    if (type == null)
      throw new ArgumentNullException(nameof(type));

    switch (Type.GetTypeCode(type))
    {
      case TypeCode.Byte:
      case TypeCode.SByte:
      case TypeCode.UInt16:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.Int64:
      case TypeCode.Decimal:
      case TypeCode.Double:
      case TypeCode.Single:
        return true;
    }
    return false;
  }
}
