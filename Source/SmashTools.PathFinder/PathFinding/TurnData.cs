
using JetBrains.Annotations;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public struct TurnData
  {
    /// <summary>
    /// Add 'turn cost' to add slight preference for smoothened paths
    /// </summary>
    public TurnCost turnCost;

    /// <summary>
    /// Add cooldown before a turn can be performed again.
    /// </summary>
    /// <remarks>
    /// Turns can still be performed before the cooldown expires, but at an increased cost.
    /// </remarks>
    public int turnCooldown;

    /// <summary>
    /// Cost multiplier per degree when turning from current heading.
    /// </summary>
    public int turnFactorPerDegree;
     
    public static TurnData Linear(int factor)
    {
      return new TurnData
      {
        turnCost = TurnCost.Linear,
        turnFactorPerDegree = factor
      };
    }

    public static TurnData Restricted(int cooldown, int factor)
    {
      return new TurnData
      {
        turnCost = TurnCost.Restricted,
        turnCooldown = cooldown,
        turnFactorPerDegree = factor
      };
    }
  }
}