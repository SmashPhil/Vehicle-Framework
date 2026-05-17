using System.Text;
using JetBrains.Annotations;
using RimWorld.Planet;

namespace Vehicles.World;

[PublicAPI]
public struct TicksPerMoveData
{
  public PlanetTile tile;
  public PlanetTile nextTile;
  public int ticksPerMove;
  public int? ticksAbs;

  public StringBuilder explanation;
  public string caravanTicksPerMoveExplanation;
}