
using JetBrains.Annotations;

namespace SmashTools.PathFinder
{
  /// <summary>
  /// The cost model used to evaluate turn maneuvers in pathfinding.
  /// </summary>
  /// <remarks>
  /// <see cref="TurnCost.None"/> applies no turn cost while pathfinding, prioritizing the cheapest path.
  /// <para/>
  /// <see cref="TurnCost.Linear"/> applies a flat cost per degree turned.
  /// <para/>
  /// <see cref="TurnCost.Restricted"/> applies a flat cost per degree turned, with a cell-count cooldown
  /// before another turn can be made.  If a turn is performed before the cooldown expires, turn cost is
  /// increased multiplicatively.
  /// </remarks>
  [PublicAPI]
  public enum TurnCost
  {
    None,
    Linear,
    Restricted
  }
}