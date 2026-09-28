using JetBrains.Annotations;

namespace AnimationKit.Editor;

[PublicAPI]
internal enum ErrorType : byte
{
  NoError = 0,
  // Arguments
  IndexOfOutOfRange = 1,
  NullArgument = 2,
  // Behavior
  NotFound = 10
}
