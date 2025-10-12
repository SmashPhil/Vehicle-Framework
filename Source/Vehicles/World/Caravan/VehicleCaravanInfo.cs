using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public class VehicleCaravanInfo
{
	public PlanetLayer layer = Find.WorldGrid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
	public List<Pawn> pawns;
	public List<Pawn> vehiclesAndDismountedPawns;
	public float massUsage;
	public float massCapacity;
	public PlanetTile tile;

	public bool caravaning;

	public VehicleCaravanInfo(List<Pawn> pawns, float massUsage, float massCapacity, PlanetTile tile)
	{
		this.pawns = pawns;
		this.massUsage = massUsage;
		this.massCapacity = massCapacity;
		this.tile = tile;
	}

	public VehicleCaravanInfo(List<TransferableOneWay> transferables, float massUsage,
		float massCapacity, PlanetTile tile) : this(TransferableUtility.GetPawnsFromTransferables(transferables), massUsage,
		massCapacity, tile)
	{
		vehiclesAndDismountedPawns = pawns
		 .Where(pawn => pawn is VehiclePawn || !CaravanHelper.assignedSeats.IsAssigned(pawn))
		 .ToList();
	}

	public VehicleCaravanInfo(Caravan caravan) : this(caravan.PawnsListForReading, caravan.MassUsage,
		caravan.MassCapacity, caravan.Tile)
	{
		caravaning = true;
		vehiclesAndDismountedPawns = caravan is VehicleCaravan vehicleCaravan ?
			[.. vehicleCaravan.Vehicles, ..vehicleCaravan.DismountedPawns] :
			null;
	}

	public VehicleCaravanInfo(Dialog_FormCaravan formCaravan) : this(formCaravan.transferables, formCaravan.MassUsage,
		formCaravan.MassCapacity, formCaravan.CurrentTile)
	{
	}
}