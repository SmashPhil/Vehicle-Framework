using System.Diagnostics;
using CoreLib.Performance;
using RimWorld;
using Verse;

namespace Vehicles;

public class Paratrooper : IAirDroppable
{
  public Pawn pawn;

  Thing IAirDroppable.Thing => pawn;

  ThingDef IAirDroppable.SkyfallerDef => SkyfallerDefOf.AirdropParatrooper;

  public int DropRadii => 12;

  public Paratrooper(Pawn pawn)
  {
    this.pawn = pawn;
  }

  void IExposable.ExposeData()
  {
    Scribe_Deep.Look(ref pawn, nameof(pawn));
  }

  bool IAirDroppable.TryDropAt(Map map, IntVec3 center, float angle)
  {
    int radius = DropRadii;
    if (!DropCellFinder.TryFindDropSpotNear(center, map, out IntVec3 position, allowFogged: false,
          canRoofPunch: CanRoofPunch(map), allowIndoors: true, maxRadius: radius, mustBeReachableFromCenter: true))
    {
      return false;
    }

    if (!position.IsValid || !position.InBounds(map))
      return false;

    DebugFlash(map, position);
    Skyfaller skyfaller = AirdropSkyfallerMaker.MakeAirdrop(this, angle);
    return GenSpawn.Spawn(skyfaller, position, map) != null;
  }

  [Conditional("DEBUG")]
  private void DebugFlash(Map map, IntVec3 pos)
  {
    if (DebugSettings.ShowDevGizmos)
    {
      foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, DropRadii, true))
      {
        map.debugDrawer.FlashCell(cell, duration: 180);
      }
    }
  }

  void IAirDroppable.OnFailureToDrop(Map map, IntVec3 pos)
  {
    const int TimeToEnter = 5000; // ms

    new Debouncer(delegate
      {
        if (map.Disposed)
          return;

        bool found = CellFinder.TryFindRandomEdgeCellWith(validator: delegate(IntVec3 cell)
        {
          if (!cell.InBounds(map))
            return false;
          if (!cell.Walkable(map) || cell.Fogged(map))
            return false;

          return map.reachability.CanReachFactionBase(cell, map.ParentFaction ?? Faction.OfPlayer);
        }, map, roadChance: 0, out IntVec3 newPos);
        if (found && newPos.IsValid && newPos.InBounds(map))
        {
          GenSpawn.Spawn(pawn, newPos, map);
        }
        else
        {
          pawn.DestroyOrPassToWorld();
        }
      }, TimeToEnter).Invoke();
  }

  private bool CanRoofPunch(Map map)
  {
    Faction faction = map.ParentFaction;
    if (faction == null)
      return true;

    return pawn.Faction.HostileTo(faction);
  }
}