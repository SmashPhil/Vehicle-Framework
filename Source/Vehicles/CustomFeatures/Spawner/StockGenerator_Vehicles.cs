using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles;

[PublicAPI]
public class StockGenerator_Vehicles : StockGenerator
{
	public VehicleCategory category;
	public HashSet<VehicleDef> excludedDefs;
	public float chance = 1;

	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		if (chance < 1 && !Rand.Chance(chance))
			yield break;

		List<VehicleDef> vehicleDefs =
			DefDatabase<VehicleDef>.AllDefsListForReading.Where(HandlesThingDef).ToList();
		if (vehicleDefs.Count == 0)
			yield break;

		int count = countRange.RandomInRange;
		for (int i = 0; i < count; i++)
		{
			float priceRange = totalPriceRange.RandomInRange;
			VehicleDef vehicleDef = vehicleDefs.Where(def => def.GetStatValueAbstract(StatDefOf.MarketValue) <= priceRange)
			 .RandomElementWithFallback();
			vehicleDef ??= vehicleDefs.RandomElement();

			yield return VehicleSpawner.GenerateVehicle(vehicleDef, faction);
		}
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		if (thingDef is not VehicleDef vehicleDef)
			return false;
		if (!excludedDefs.NullOrEmpty() && excludedDefs.Contains(vehicleDef))
			return false;

		return (vehicleDef.vehicleCategory & category) == category;
	}
}