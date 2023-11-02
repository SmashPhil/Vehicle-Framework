using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public static class AirdropSkyfallerMaker
	{
		public static AirdropSkyfaller MakeAirdrop(AirdropDef airdropDef, bool packIntoContainer = true, params Thing[] contents)
		{
			try
			{
				AirdropSkyfaller skyfaller = (AirdropSkyfaller)ThingMaker.MakeThing(airdropDef);
				
				if (contents.Length == 1 && !packIntoContainer)
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
					if (packIntoContainer)
					{
						airdrop = (Airdrop)ThingMaker.MakeThing(ThingDefOf_Vehicles.Airdrop);
					}

					foreach (Thing thing in contents)
					{
						if (packIntoContainer)
						{
							TryPackInto(thing, airdrop.innerContainer);
						}
						else
						{
							TryPackInto(thing, skyfaller.innerContainer);
						}
					}

					if (packIntoContainer)
					{
						skyfaller.innerContainer.TryAdd(airdrop);
					}
				}
				
				return skyfaller;
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to generate AirdropSkyfaller. Exception=\"{ex}\"");
			}
			return null;
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
}
