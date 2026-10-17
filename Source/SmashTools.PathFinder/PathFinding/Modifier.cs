using JetBrains.Annotations;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public struct Modifier
  {
    public ModifierType type;
    public ushort value;

    internal static int Apply(in Modifier modifier, int cost)
    {
      switch (modifier.type)
      {
        case ModifierType.Add:
          cost += modifier.value;
          break;
        case ModifierType.Subtract:
          cost -= modifier.value;
          break;
        case ModifierType.Multiply:
          cost *= modifier.value;
          break;
        case ModifierType.Divide:
          cost /= modifier.value;
          break;
        case ModifierType.ShiftLeft:
          cost <<= modifier.value;
          break;
        case ModifierType.ShiftRight:
          cost >>= modifier.value;
          break;
        case ModifierType.None:
        default:
          break;
      }
      return cost;
    }

    internal static int Apply(ModifierType type, ushort value, int cost)
    {
      switch (type)
      {
        case ModifierType.Add:
          cost += value;
          break;
        case ModifierType.Subtract:
          cost -= value;
          break;
        case ModifierType.Multiply:
          cost *= value;
          break;
        case ModifierType.Divide:
          cost /= value;
          break;
        case ModifierType.ShiftLeft:
          cost <<= value;
          break;
        case ModifierType.ShiftRight:
          cost >>= value;
          break;
        case ModifierType.None:
        default:
          break;
      }
      return cost;
    }
  }
}