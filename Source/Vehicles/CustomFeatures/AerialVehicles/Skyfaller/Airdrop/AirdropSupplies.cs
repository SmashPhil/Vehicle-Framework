using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles;

[PublicAPI]
public class AirdropSupplies : IAirDroppable
{
	private Airdrop airdrop = (Airdrop)ThingMaker.MakeThing(ThingDefOf_Vehicles.Airdrop);

	Thing IAirDroppable.Thing => airdrop;

	ThingDef IAirDroppable.SkyfallerDef => SkyfallerDefOf.AirdropPackage;

	public AirdropSupplies()
	{
	}

	public AirdropSupplies(IEnumerable<Thing> things)
	{
		foreach (Thing thing in things)
		{
			Pack(thing);
		}
	}

	public void Pack(Thing thing)
	{
		airdrop.innerContainer.TryAddOrTransfer(thing);
	}

	void IExposable.ExposeData()
	{
		Scribe_Deep.Look(ref airdrop, nameof(airdrop));
	}

	void IAirDroppable.OnDropped(Map map, IntVec3 pos)
	{
	}

	void IAirDroppable.OnFailureToDrop(Map map, IntVec3 simPos)
	{
		IntVec3 dropCell = DropCellFinder.TradeDropSpot(map);
		if (dropCell.InBounds(map))
		{
			Skyfaller skyfaller = AirdropSkyfallerMaker.MakeAirdrop(this, Rand.Range(-15, 15));
			GenSpawn.Spawn(skyfaller, dropCell, map);
		}
	}
}