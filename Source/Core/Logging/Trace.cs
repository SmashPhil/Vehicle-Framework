using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Logger = CoreLib.Logger;

// TODO 1.8 - Change to CoreLib
namespace SmashTools;

/// <summary>
/// Log wrapper class for extracting stack trace info before sending to log
/// </summary>
[PublicAPI]
[TypeForwardedFrom(assemblyFullName: "SmashTools, Version=1.6.0.0, Culture=neutral, PublicKeyToken=null")]
public static class Trace
{
  [Conditional("TRACE")]
  public static void IsTrue(bool condition, string message = null, [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0)
  {
    if (condition)
      return;

    Fail(message, filePath, lineNumber);
  }

  [Conditional("TRACE")]
  public static void IsFalse(bool condition, string message = null, [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0)
  {
    if (!condition)
      return;

    Fail(message, filePath, lineNumber);
  }

  [Conditional("TRACE")]
  public static void IsNull<T>(T obj, string message = null, [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0) where T : class
  {
    if (obj == null)
      return;

    Fail(message, filePath, lineNumber);
  }

  [Conditional("TRACE")]
  public static void IsNotNull<T>(T obj, string message = null, [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0) where T : class
  {
    if (obj != null)
      return;

    Fail(message, filePath, lineNumber);
  }

  [Conditional("TRACE")]
  public static void Fail(string message = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
  {
    Logger.Error($"{message ?? "Assertion Failed"} ({filePath} ln. {lineNumber})");
  }
}