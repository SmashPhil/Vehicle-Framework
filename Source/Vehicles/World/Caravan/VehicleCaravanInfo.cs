using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public class VehicleCaravanInfo(
  List<Pawn> pawns,
  float massUsage,
  float massCapacity,
  PlanetTile tile)
{
  public PlanetLayer layer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
  public List<Pawn> pawns = pawns;
  public float massUsage = massUsage;
  public float massCapacity = massCapacity;
  public PlanetTile tile = tile;

  public bool caravaning;

  public VehicleCaravanInfo(List<TransferableOneWay> transferables, float massUsage,
    float massCapacity, PlanetTile tile) : this(
    TransferableUtility.GetPawnsFromTransferables(transferables), massUsage, massCapacity, tile)
  {
  }

  public VehicleCaravanInfo(Caravan caravan) : this(caravan.PawnsListForReading, caravan.MassUsage,
    caravan.MassCapacity, caravan.Tile)
  {
    caravaning = true;
  }
}