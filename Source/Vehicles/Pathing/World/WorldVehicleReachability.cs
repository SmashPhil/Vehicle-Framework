using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Reachability grid
	/// </summary>
	public class WorldVehicleReachability : WorldComponent
	{
		private readonly Dictionary<VehicleDef, int[]> reachabilityGrid;

		private int nextFieldID;
		private int impassableFieldID;
		private int minValidFieldID;

		public WorldVehicleReachability(World world) : base(world)
		{
			this.world = world;
			reachabilityGrid = new Dictionary<VehicleDef, int[]>();
			nextFieldID = 1;
			InvalidateAllFields();
			InitReachabilityGrid();
			Instance = this;
		}

		/// <summary>
		/// Singleton getter
		/// </summary>
		public static WorldVehicleReachability Instance { get; private set; }

		/// <summary>
		/// Clear reachability cache
		/// </summary>
		public void ClearCache()
		{
			InvalidateAllFields();
		}

		/// <summary>
		/// Validate all VehicleDefs in reachability cache
		/// </summary>
		private void InitReachabilityGrid()
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				reachabilityGrid.Add(vehicleDef, new int[Find.WorldGrid.TilesCount]);
			}
		}

		/// <summary>
		/// <paramref name="caravan"/> can reach <paramref name="destTile"/>
		/// </summary>
		/// <param name="c"></param>
		/// <param name="destTile"></param>
		public bool CanReach(VehicleCaravan caravan, int destTile)
		{
			int startTile = caravan.Tile;
			List<VehicleDef> vehicleDefs = caravan.UniqueVehicleDefsInCaravan().ToList();
			return vehicleDefs.All(v => CanReach(v, startTile, destTile));
		}

		/// <summary>
		/// <paramref name="vehicleDef"/> can reach <paramref name="destTile"/> from <paramref name="startTile"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="startTile"></param>
		/// <param name="destTile"></param>
		public bool CanReach(VehicleDef vehicleDef, int startTile, int destTile)
		{
			if (startTile < 0 || startTile >= Find.WorldGrid.TilesCount || destTile < 0 || destTile >= Find.WorldGrid.TilesCount)
			{
				return false;
			}
			if (reachabilityGrid[vehicleDef][startTile] == impassableFieldID || reachabilityGrid[vehicleDef][destTile] == impassableFieldID)
			{
				return false;
			}
			if (IsValidField(reachabilityGrid[vehicleDef][startTile])  || IsValidField(reachabilityGrid[vehicleDef][destTile]))
			{
				return reachabilityGrid[vehicleDef][startTile] == reachabilityGrid[vehicleDef][destTile];
			}
			FloodFillAt(startTile, vehicleDef);
			return reachabilityGrid[vehicleDef][startTile] != impassableFieldID && reachabilityGrid[vehicleDef][startTile] == reachabilityGrid[vehicleDef][destTile];
		}

		/// <summary>
		/// Invalidate all field IDs
		/// </summary>
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

		/// <summary>
		/// <paramref name="fieldID"/> is valid
		/// </summary>
		/// <param name="fieldID"></param>
		private bool IsValidField(int fieldID)
		{
			return fieldID >= minValidFieldID;
		}

		/// <summary>
		/// FloodFill reachability cache at <paramref name="tile"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDef"></param>
		private void FloodFillAt(int tile, VehicleDef vehicleDef)
		{
			if (!reachabilityGrid.ContainsKey(vehicleDef))
			{
				reachabilityGrid.Add(vehicleDef, new int[Find.WorldGrid.TilesCount]);
			}

			if (!WorldVehiclePathGrid.Instance.Passable(tile, vehicleDef))
			{
				reachabilityGrid[vehicleDef][tile] = impassableFieldID;
				return;
			}

			Find.WorldFloodFiller.FloodFill(tile, (int x) => WorldVehiclePathGrid.Instance.Passable(x, vehicleDef), delegate (int x)
			{
				reachabilityGrid[vehicleDef][x] = nextFieldID;
			}, int.MaxValue, null);
			nextFieldID++;
		}
	}
}
