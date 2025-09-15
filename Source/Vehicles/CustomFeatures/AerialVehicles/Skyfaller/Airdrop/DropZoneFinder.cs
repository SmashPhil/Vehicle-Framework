using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public static class DropZoneFinder
{
	private const string MountainCategory = "Mountain";

	public static DropZone GetDropZone(Map map, Rot4 fromEdge, int points)
	{
		Assert.IsTrue(map.CanAirdropInMap());
		AirdropManager cache = map.GetCachedMapComponent<AirdropManager>();
		return cache.GetDropZoneFor(fromEdge, points);
	}

	public static bool CanAirdropInMap(this Map map)
	{
		IList<TileMutatorDef> mutators = map.Tile.Tile.Mutators;
		if (mutators.NotNullAndAny(InvalidDropArea))
			return false;

		// TODO - check and cache drop zone viability
		return true;

		static bool InvalidDropArea(TileMutatorDef def)
		{
			return def.IsCave;
			//if (def.IsCave)
			//	return true;
			//return !def.categories.NullOrEmpty() && def.categories.Contains(MountainCategory);
		}
	}
}