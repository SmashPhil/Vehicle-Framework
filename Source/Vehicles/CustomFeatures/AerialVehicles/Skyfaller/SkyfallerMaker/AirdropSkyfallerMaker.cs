using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles;

[PublicAPI]
public struct AirdropProperties
{
	public required float angle;

	public bool packIntoContainer = false;

	public AirdropProperties()
	{
	}

	public static AirdropProperties Default => new() { angle = Rand.Range(-30, 30) };
}

[PublicAPI]
public static class AirdropSkyfallerMaker
{
	public static Skyfaller MakeAirdrop(IAirDroppable airDroppable, float angle)
	{
		Skyfaller skyfaller = (Skyfaller)ThingMaker.MakeThing(airDroppable.SkyfallerDef);
		skyfaller.innerContainer.TryAddOrTransfer(airDroppable.Thing);
		skyfaller.angle = angle;
		return skyfaller;
	}

	public static AirdropSkyfaller MakeAirdrop(AirdropDef airdropDef, [NotNull] Thing thing,
		in AirdropProperties props)
	{
		return MakeAirdrop(airdropDef, [thing], in props);
	}

	public static AirdropSkyfaller MakeAirdrop(AirdropDef airdropDef, [NotNull] List<Thing> contents,
		in AirdropProperties props)
	{
		AirdropSkyfaller skyfaller = (AirdropSkyfaller)ThingMaker.MakeThing(airdropDef);

		if (contents.Count > 0 && !props.packIntoContainer)
		{
			Thing thing = contents[0];
			if (thing.Spawned)
			{
				thing.DeSpawn();
			}
			skyfaller.innerContainer.TryAddOrTransfer(thing);
		}
		else
		{
			Airdrop airdrop = null;
			if (props.packIntoContainer)
			{
				airdrop = (Airdrop)ThingMaker.MakeThing(ThingDefOf_Vehicles.Airdrop);
			}

			foreach (Thing thing in contents)
			{
				TryPackInto(thing, props.packIntoContainer ? airdrop.innerContainer : skyfaller.innerContainer);
			}

			if (props.packIntoContainer)
			{
				skyfaller.innerContainer.TryAdd(airdrop);
			}
		}
		return skyfaller;
	}

	private static bool TryPackInto(Thing thing, ThingOwner container)
	{
		if (thing != null && !container.TryAddOrTransfer(thing))
		{
			Log.Error($"Could not add {thing} to Airdrop.");
			thing.Destroy();
			return false;
		}
		return true;
	}
}