using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Algorithms;
using SmashTools.Performance;
using Verse;
using Verse.Sound;

namespace Vehicles.World;

/// <summary>
/// Reachability grid
/// </summary>
public class WorldVehicleReachability
{
	private readonly WorldVehiclePathGrid pathGrid;

	private readonly WorldRegionGrid[] regionGrids;

	public WorldVehicleReachability(WorldVehiclePathGrid pathGrid)
	{
		this.pathGrid = pathGrid;
		regionGrids = new WorldRegionGrid[DefDatabase<VehicleDef>.DefCount];
		InitReachabilityGrid();
		pathGrid.OnReachabilityDirty += RegenerateRegionsFor;
	}

	public WorldRegionGrid GetRegionGrid(VehicleDef vehicleDef)
	{
		// TODO VF-48: Implement lazy generation for region grids
		VehicleDef ownerDef = GridOwners.World.GetOwner(vehicleDef);
		return regionGrids[ownerDef.DefIndex];
	}

	public int GetRegionId(VehicleDef vehicleDef, int tile)
	{
		VehicleDef ownerDef = GridOwners.World.GetOwner(vehicleDef);
		return regionGrids[ownerDef.DefIndex].GetRegionId(tile);
	}

	/// <summary>
	/// Validate all VehicleDefs in reachability cache
	/// </summary>
	private void InitReachabilityGrid()
	{
		foreach (VehicleDef ownerDef in GridOwners.World.AllOwners)
		{
			regionGrids[ownerDef.DefIndex] = new WorldRegionGrid(pathGrid, ownerDef);
		}
	}

	private void RegenerateAllRegions(CancellationToken token)
	{
		foreach (VehicleDef ownerDef in GridOwners.World.AllOwners)
		{
			RegenerateRegionsFor(ownerDef, token);
		}
	}

	private void RegenerateRegionsFor(VehicleDef vehicleDef, CancellationToken token)
	{
		regionGrids[GridOwners.World.GetOwner(vehicleDef).DefIndex].GenerateRegions(token);
	}

	/// <summary>
	/// <paramref name="caravan"/> can reach <paramref name="destTile"/>
	/// </summary>
	public bool CanReach(VehicleCaravan caravan, PlanetTile destTile)
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
	public bool CanReach(VehicleDef vehicleDef, PlanetTile startTile, PlanetTile destTile)
	{
		if (startTile < 0 || startTile >= Find.WorldGrid.TilesCount || destTile < 0 ||
			destTile >= Find.WorldGrid.TilesCount)
		{
			Log.Error("Trying to reach tile that is out of bounds of the world grid.");
			return false;
		}
		VehicleDef ownerDef = GridOwners.World.GetOwner(vehicleDef);
		return regionGrids[ownerDef.DefIndex].CanReach(startTile, destTile);
	}

	[DebugAction(VehicleHarmony.VehiclesLabel, name = "Flash Region Grid",
		allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void FlashRandomRegionGrid()
	{
		const int FlashTicks = 600;

		if (!GridOwners.World.AnyOwners)
		{
			SoundDefOf.ClickReject.PlayOneShotOnCamera();
			return;
		}
		VehicleDef vehicleDef = GridOwners.World.AllOwners[0];
		WorldVehicleReachability reachability = WorldVehiclePathGrid.Instance.reachability;
		for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
		{
			Find.World.debugDrawer.FlashTile(i, colorPct: ColorPct(i), text: IdStringAt(i), FlashTicks);
		}
		return;

		string IdStringAt(int t) => reachability.GetRegionId(vehicleDef, t).ToString();

		float ColorPct(int tile)
		{
			WorldRegionGrid regionGrid =
				WorldVehiclePathGrid.Instance.reachability.regionGrids[vehicleDef.DefIndex];
			int id = regionGrid.GetRegionId(tile);
			return id switch
			{
				0  => 0,
				-1 => 0.25f,
				_  => 0.75f,
			};
		}
	}

	// TODO 1.6 - Account for planet layers (map surface layer -> int[], then PlanetTile::tileId)
	public class WorldRegionGrid
	{
		private readonly WorldVehiclePathGrid pathGrid;
		private readonly VehicleDef owner;

		// -1 : Impassable tile
		//  0 : Unregistered tile
		// >0 : Tile with region
		private int[] regionIds = [];

		private int totalRegions;

		public WorldRegionGrid(WorldVehiclePathGrid pathGrid, VehicleDef vehicleDef)
		{
			this.pathGrid = pathGrid;
			this.owner = vehicleDef;
		}

		public int TotalRegions => totalRegions + 2;

		public int GetRegionId(int tile)
		{
			return regionIds[tile];
		}

		public bool CanReach(int fromTile, int toTile)
		{
			int fromId = regionIds[fromTile];
			int toId = regionIds[toTile];
			return (fromId > 0 && toId > 0) && fromId == toId;
		}

		[Profile]
		public void GenerateRegions(CancellationToken token)
		{
			BFS<PlanetTile> floodfiller = new();
			int[] tilesToId = new int[Find.WorldGrid.TilesCount];
			totalRegions = 1;

			for (int tile = 0; tile < Find.WorldGrid.TilesCount; tile++)
			{
				if (token.IsCancellationRequested)
					return;
				if (tilesToId[tile] != 0)
					continue;

				if (!pathGrid.PassableFast(tile, owner))
				{
					tilesToId[tile] = -1;
					continue;
				}

				int id = totalRegions;
				floodfiller.FloodFill(tile, Ext_World.GetTileNeighbors, null, onEntered: OnEnter,
					onSkipped: null, token, canEnter: CanEnter);

				totalRegions++;
				continue;

				void OnEnter(PlanetTile t) => tilesToId[t] = id;

				// Tile hasn't been processed and is not impassable
				bool CanEnter(PlanetTile t) => tilesToId[t] == 0 && pathGrid.PassableFast(t, owner);
			}
			// This is the only case where the id array will be getting written to so we can just use
			// an atomic reference swap and maintain thread safety lock-free.
			this.regionIds = tilesToId;
		}
	}
}