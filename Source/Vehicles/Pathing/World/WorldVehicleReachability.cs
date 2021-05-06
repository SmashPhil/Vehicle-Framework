using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class WorldVehicleReachability : WorldComponent
	{
		private Dictionary<ThingDef, int[]> fields;

		private int nextFieldID;

		private int impassableFieldID;

		private int minValidFieldID;

		public WorldVehicleReachability(World world) : base(world)
		{
			this.world = world;
			fields = new Dictionary<ThingDef, int[]>();
			nextFieldID = 1;
			InvalidateAllFields();
			ValidateVehicleDefs();
		}

		public void ClearCache()
		{
			InvalidateAllFields();
		}

		public bool CanReach(Caravan c, int destTile)
		{
			int startTile = c.Tile;
			List<VehicleDef> vehicleDefs = c.UniqueVehicleDefsInCaravan().ToList();
			return CanReach(vehicleDefs, startTile, destTile);
		}

		public bool CanReach(List<VehicleDef> vehicleDefs, int startTile, int destTile)
		{
			if (startTile < 0 || startTile >= Find.WorldGrid.TilesCount || destTile < 0 || destTile >= Find.WorldGrid.TilesCount)
			{
				return false;
			}
			if (vehicleDefs.All(v => fields[v][startTile] == impassableFieldID) || vehicleDefs.All(v => fields[v][destTile] == impassableFieldID))
			{
				return false;
			}
			if (vehicleDefs.All(v => IsValidField(fields[v][startTile]))  || vehicleDefs.All(v => IsValidField(fields[v][destTile])))
			{
				return vehicleDefs.All(v => fields[v][startTile] == fields[v][destTile]);
			}
			vehicleDefs.ForEach(v => FloodFillAt(startTile, v));
			return vehicleDefs.All(v => fields[v][startTile] != impassableFieldID && fields[v][startTile] == fields[v][destTile]);
		}

		private void InvalidateAllFields()
		{
			if (nextFieldID == int.MaxValue)
			{
				nextFieldID = 1;
			}
			minValidFieldID = nextFieldID;
			impassableFieldID = nextFieldID;
			nextFieldID++;
		}

		private void ValidateVehicleDefs()
		{
			foreach(VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				fields.Add(vehicleDef, new int[Find.WorldGrid.TilesCount]);
				if (vehicleDef.properties.customBiomeCosts is null)
				{
					vehicleDef.properties.customBiomeCosts = new Dictionary<BiomeDef, float>();
				}
				if(vehicleDef.vehicleType == VehicleType.Sea)
				{
					if (!vehicleDef.properties.customBiomeCosts.ContainsKey(BiomeDefOf.Ocean))
					{
						vehicleDef.properties.customBiomeCosts.Add(BiomeDefOf.Ocean, 1);
					}
					if (!vehicleDef.properties.customBiomeCosts.ContainsKey(BiomeDefOf.Lake))
					{
						vehicleDef.properties.customBiomeCosts.Add(BiomeDefOf.Lake, 1);
					}
				}
			}
		}

		private bool IsValidField(int fieldID)
		{
			return fieldID >= minValidFieldID;
		}

		private void FloodFillAt(int tile, VehicleDef vehicleDef)
		{
			if(!fields.ContainsKey(vehicleDef))
			{
				fields.Add(vehicleDef, new int[Find.WorldGrid.TilesCount]);
			}

			if(!Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(tile, vehicleDef))
			{
				fields[vehicleDef][tile] = impassableFieldID;
				return;
			}

			Find.WorldFloodFiller.FloodFill(tile, (int x) => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(x, vehicleDef), delegate (int x)
			{
				fields[vehicleDef][x] = nextFieldID;
			}, int.MaxValue, null);
			nextFieldID++;
		}
	}
}
