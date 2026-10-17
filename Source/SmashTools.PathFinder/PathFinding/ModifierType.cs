using JetBrains.Annotations;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public enum ModifierType
  {
    None,
    Add,
    Subtract,
    Multiply,
    Divide,
    ShiftLeft,
    ShiftRight
  }
}