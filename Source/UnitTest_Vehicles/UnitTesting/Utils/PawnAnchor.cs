using System;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

/// <summary>
/// Generate and keep a single pawn alive on the map when object goes out of scope so test map
/// stays open when test runner skips a frame.
/// </summary>
internal class PawnAnchorer : IDisposable
{
  private static Pawn pawn;

  static PawnAnchorer()
  {
    pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, faction: Faction.OfPlayer);
  }

  public PawnAnchorer()
  {
    if (pawn.Spawned)
      pawn.DeSpawn();
    Assert.IsFalse(pawn.Spawned);
  }

  void IDisposable.Dispose()
  {
    if (pawn.Destroyed || pawn.Discarded)
      pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, faction: Faction.OfPlayer);
    IntVec3 spawnCell = IntVec3.Zero;
    Map map = Find.CurrentMap;
    DebugHelper.DestroyCell(spawnCell, map, TerrainDefOf.Concrete);
    GenSpawn.Spawn(pawn, spawnCell, Find.CurrentMap);
    Assert.IsTrue(pawn.Spawned);
  }
}