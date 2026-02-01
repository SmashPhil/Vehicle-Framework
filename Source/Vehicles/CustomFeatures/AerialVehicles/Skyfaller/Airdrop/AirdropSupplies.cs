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

  public int DropRadii => 9;

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

	bool IAirDroppable.TryDropAt(Map map, IntVec3 center, float angle)
	{
    if (!DropCellFinder.TryFindDropSpotNear(center, map, out IntVec3 position, allowFogged: false,
          canRoofPunch: true, allowIndoors: false, maxRadius: DropRadii, mustBeReachableFromCenter: false))
    {
      return false;
    }

    if (!position.IsValid || !position.InBounds(map))
      return false;

    Skyfaller skyfaller = AirdropSkyfallerMaker.MakeAirdrop(this, angle);
    return GenSpawn.Spawn(skyfaller, position, map) != null;
  }

	void IAirDroppable.OnFailureToDrop(Map map, IntVec3 simPos)
	{
		IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);
		if (dropCell.InBounds(map))
		{
			Skyfaller skyfaller = AirdropSkyfallerMaker.MakeAirdrop(this, Rand.Range(-15, 15));
			GenSpawn.Spawn(skyfaller, dropCell, map);
		}
	}
}