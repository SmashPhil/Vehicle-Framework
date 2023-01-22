using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	/// <summary>
	/// MapComponent container for all pathing related sub-components for vehicles
	/// </summary>
	public sealed class VehicleMapping : MapComponent
	{
		private readonly Dictionary<VehicleDef, VehiclePathData> vehicleData = new Dictionary<VehicleDef, VehiclePathData>();
		private readonly List<VehicleDef> pathDataOwners = new List<VehicleDef>();

		public VehicleMapping(Map map) : base(map)
		{
			vehicleData ??= new Dictionary<VehicleDef, VehiclePathData>();
			ConstructComponents();
		}

		public List<VehicleDef> Owners => pathDataOwners;

		/// <summary>
		/// Retrieve all <see cref="VehiclePathData"/> for this map
		/// </summary>
		public IEnumerable<VehiclePathData> AllPathData
		{
			get
			{
				foreach (VehicleDef pathDataOwner in pathDataOwners)
				{
					yield return vehicleData[pathDataOwner];
				}
			}
		}

		/// <summary>
		/// Get <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		public VehiclePathData this[VehicleDef vehicleDef]
		{
			get
			{
				if (vehicleData.TryGetValue(vehicleDef, out VehiclePathData pathData))
				{
					return pathData;
				}
				Log.Error($"Unable to retrieve path data on {map} for {vehicleDef.defName}. Was this VehicleDef created postload? Self-correcting...");
				Log.Error($"StackTrace: {StackTraceUtility.ExtractStackTrace()}");
				GeneratePathData(vehicleDef);
				return vehicleData[vehicleDef];
			}
		}

		/// <summary>
		/// Finalize initialization for map component
		/// </summary>
		public void RebuildVehiclePathData()
		{
			foreach (VehiclePathData data in vehicleData.Values)
			{
				data.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
				data.VehicleRegionAndRoomUpdater.Enabled = true;
				data.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
			}
		}

		/// <summary>
		/// Construct and cache <see cref="VehiclePathData"/> for each moveable <see cref="VehicleDef"/> 
		/// </summary>
		public void ConstructComponents()
		{
			vehicleData.Clear();
			pathDataOwners.Clear();
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				GeneratePathData(vehicleDef);
			}
		}

		/// <summary>
		/// Update vehicle regions sequentially
		/// </summary>
		/// <remarks>
		/// Only one VehicleDef's RegionGrid updates per frame so performance doesn't scale with vehicle count
		/// </remarks>
		public override void MapComponentUpdate()
		{
			if (pathDataOwners.Count > 0 && VehicleRegionGrid.vehicleRegionGridIndexChecking <= pathDataOwners.Count)
			{
				VehicleDef vehicleDef = pathDataOwners[VehicleRegionGrid.vehicleRegionGridIndexChecking];
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionGrid.UpdateClean();
				vehiclePathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
			}
		}

		/// <summary>
		/// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		private void GeneratePathData(VehicleDef vehicleDef, bool compress = true)
		{
			VehiclePathData vehiclePathData = new VehiclePathData(vehicleDef);

			vehiclePathData.VehiclePathGrid = new VehiclePathGrid(this, vehicleDef);
			vehiclePathData.VehiclePathFinder = new VehiclePathFinder(this, vehicleDef);

			VehiclePathData matchingReachability = vehicleData.Values.FirstOrDefault(otherPathData => vehiclePathData.MatchesReachability(otherPathData));
			if (compress && matchingReachability.IsValid)
			{
				//Piggy back off vehicles with similar width + impassability
				vehiclePathData.ReachabilityData = matchingReachability.ReachabilityData;
			}
			else
			{
				vehiclePathData.ReachabilityData = new VehicleReachabilitySettings(this, vehicleDef);
				pathDataOwners.Add(vehicleDef);
			}

			vehicleData[vehicleDef] = vehiclePathData;
		}

		/// <summary>
		/// Container for all path related sub-components specific to a <see cref="VehicleDef"/>.
		/// </summary>
		/// <remarks>Stores data strictly for deviations from vanilla regarding impassable values</remarks>
		public struct VehiclePathData
		{
			private readonly VehicleDef vehicleDef;

			private readonly HashSet<ThingDef> impassableThingDefs;
			private readonly HashSet<TerrainDef> impassableTerrain;
			private readonly int border;
			private readonly bool defaultTerrainImpassable;

			public VehiclePathData(VehicleDef vehicleDef)
			{
				this.vehicleDef = vehicleDef;
				border = Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z) / 2;
				defaultTerrainImpassable = vehicleDef.properties.defaultTerrainImpassable;
				impassableThingDefs = vehicleDef.properties.customThingCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();
				impassableTerrain = vehicleDef.properties.customTerrainCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();

				VehiclePathGrid = null;
				VehiclePathFinder = null;
				ReachabilityData = null;
			}

			public bool IsValid => vehicleDef != null;

			public VehicleDef Owner => vehicleDef;

			internal VehicleReachabilitySettings ReachabilityData { get; set; }

			public VehiclePathGrid VehiclePathGrid { get; set; }

			public VehiclePathFinder VehiclePathFinder { get; set; }

			public VehicleReachability VehicleReachability => ReachabilityData.vehicleReachability;

			public VehicleRegionGrid VehicleRegionGrid => ReachabilityData.vehicleRegionGrid;

			public VehicleRegionMaker VehicleRegionMaker => ReachabilityData.vehicleRegionMaker;

			public VehicleRegionLinkDatabase VehicleRegionLinkDatabase => ReachabilityData.vehicleRegionLinkDatabase;

			public VehicleRegionAndRoomUpdater VehicleRegionAndRoomUpdater => ReachabilityData.vehicleRegionAndRoomUpdater;

			public VehicleRegionDirtyer VehicleRegionDirtyer => ReachabilityData.vehicleRegionDirtyer;

			public bool MatchesReachability(VehiclePathData other)
			{
				return border == other.border && defaultTerrainImpassable == other.defaultTerrainImpassable &&
					impassableThingDefs.SetEquals(other.impassableThingDefs) && impassableTerrain.SetEquals(other.impassableTerrain);
			}
		}

		//Strictly for readability
		public class VehicleReachabilitySettings
		{
			public VehicleReachability vehicleReachability;
			public VehicleRegionGrid vehicleRegionGrid;
			public VehicleRegionMaker vehicleRegionMaker;
			public VehicleRegionLinkDatabase vehicleRegionLinkDatabase;
			public VehicleRegionAndRoomUpdater vehicleRegionAndRoomUpdater;
			public VehicleRegionDirtyer vehicleRegionDirtyer;

			public VehicleReachabilitySettings(VehicleMapping vehicleMapping, VehicleDef vehicleDef)
			{
				vehicleReachability = new VehicleReachability(vehicleMapping, vehicleDef);
				vehicleRegionGrid = new VehicleRegionGrid(vehicleMapping, vehicleDef);
				vehicleRegionMaker = new VehicleRegionMaker(vehicleMapping, vehicleDef);
				vehicleRegionLinkDatabase = new VehicleRegionLinkDatabase();
				vehicleRegionAndRoomUpdater = new VehicleRegionAndRoomUpdater(vehicleMapping, vehicleDef);
				vehicleRegionDirtyer = new VehicleRegionDirtyer(vehicleMapping, vehicleDef);
			}
		}
	}
}
