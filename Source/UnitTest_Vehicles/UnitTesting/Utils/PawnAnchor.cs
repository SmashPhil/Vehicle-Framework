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
  private static readonly Pawn pawn;

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
    Assert.IsTrue(CellFinder.TryFindRandomSpawnCellForPawnNear(Find.CurrentMap.Center,
      Find.CurrentMap, out IntVec3 spawnCell));
    GenSpawn.Spawn(pawn, spawnCell, Find.CurrentMap);
    Assert.IsTrue(pawn.Spawned);
  }
}