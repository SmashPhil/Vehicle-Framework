using JetBrains.Annotations;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public struct Heuristic
  {
    public int normal;
    public int prefer;
    public int avoid;

    public bool IsValid => normal > 0 && prefer > 0 && avoid > 0;

    public static Heuristic Default => new()
    {
      normal = 10,
      avoid = 250,
      prefer = 1
    };
  }

  [PublicAPI]
  public enum HeuristicWeight
  {
    Normal,
    Prefer,
    Avoid
  }
}