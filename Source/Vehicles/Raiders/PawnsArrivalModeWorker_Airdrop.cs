using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Raiders;

[UsedImplicitly]
internal class PawnsArrivalModeWorker_Airdrop : PawnsArrivalModeWorker
{
	private const int DefaultDelayTicks = 6 * GenTicks.TicksPerRealSecond;
	private const int DefaultTicksToFlyOver = 12 * GenTicks.TicksPerRealSecond;

	public override bool CanUseOnMap(Map map)
	{
		return map.CanAirdropInMap() && base.CanUseOnMap(map);
	}

	public override void Arrive(List<Pawn> pawns, IncidentParms parms)
	{
		Map map = parms.target as Map;
		Assert.IsNotNull(map);
		DropZone dropZone = DropZoneFinder.GetDropZone(map, parms.spawnRotation, pawns.Count);
		Assert.AreEqual(pawns.Count, dropZone.dropPoints.Count);

		DropShip.Properties props = new()
		{
			lifetime = DefaultTicksToFlyOver,
			delayDropByTicks = DefaultDelayTicks,
			ticksBetweenDrops = 10
		};
		DropShip carrier = new(map, dropZone, props)
		{
			FlyoverSoundDef = SoundDefOf_Vehicles.AerialVehicle_Paratroopers_FlyOver,
			Faction = parms.faction
		};
		foreach (Pawn pawn in pawns)
		{
			carrier.Add(new Paratrooper(pawn));
		}
		map.GetCachedMapComponent<AirdropManager>().Spawn(carrier);
	}

	public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
	{
		parms.spawnRotation = Rot4.Random; // TODO - make weighted for coastlines and mountain edges
		return true;
	}
}