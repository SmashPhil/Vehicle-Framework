using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// MapComponent container for all pathing related sub-components for vehicles
	/// </summary>
	public sealed class VehicleMapping : MapComponent
	{
		private readonly Dictionary<VehicleDef, VehiclePathData> vehicleData = new Dictionary<VehicleDef, VehiclePathData>();

		public VehicleMapping(Map map) : base(map)
		{
			vehicleData ??= new Dictionary<VehicleDef, VehiclePathData>();
			ConstructComponents();
		}

		/// <summary>
		/// Retrieve all <see cref="VehiclePathData"/> for this map
		/// </summary>
		public IEnumerable<VehiclePathData> AllPathData => vehicleData.Values;

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
				Log.Error($"StackTrace: {UnityEngine.StackTraceUtility.ExtractStackTrace()}");
				return GeneratePathData(vehicleDef);
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
		/// Construct and cache <see cref="VehiclePathData"/> for each <see cref="VehicleDef"/> 
		/// </summary>
		public void ConstructComponents()
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
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
			if (VehicleHarmony.AllMoveableVehicleDefsCount > 0 && VehicleRegionGrid.vehicleRegionGridIndexChecking <= VehicleHarmony.AllMoveableVehicleDefsCount)
			{
				VehicleDef vehicleDef = VehicleHarmony.AllMoveableVehicleDefs[VehicleRegionGrid.vehicleRegionGridIndexChecking];
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionGrid.UpdateClean();
				vehiclePathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
			}
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			foreach (VehiclePathData data in AllPathData)
			{
				data.FinalizeInit();
			}
		}

		/// <summary>
		/// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		private VehiclePathData GeneratePathData(VehicleDef vehicleDef)
		{
			VehiclePathData data = new VehiclePathData()
			{
				VehiclePathGrid = new VehiclePathGrid(this, vehicleDef),
				VehiclePathFinder = new VehiclePathFinder(this, vehicleDef),
				VehicleReachability = new VehicleReachability(this, vehicleDef),
				VehicleRegionGrid = new VehicleRegionGrid(this, vehicleDef),
				VehicleRegionMaker = new VehicleRegionMaker(this, vehicleDef),
				VehicleRegionAndRoomUpdater = new VehicleRegionAndRoomUpdater(this, vehicleDef),
				VehicleRegionLinkDatabase = new VehicleRegionLinkDatabase(),
				VehicleRegionDirtyer = new VehicleRegionDirtyer(this, vehicleDef)
			};
			vehicleData.Add(vehicleDef, data);
			return data;
		}

		/// <summary>
		/// Container for all path related sub-components specific to a <see cref="VehicleDef"/>
		/// </summary>
		public struct VehiclePathData
		{
			public VehiclePathGrid VehiclePathGrid { get; set; }

			public VehiclePathFinder VehiclePathFinder { get; set; }

			public VehicleReachability VehicleReachability { get; set; }

			public VehicleRegionGrid VehicleRegionGrid { get; set; }

			public VehicleRegionMaker VehicleRegionMaker { get; set; }

			public VehicleRegionLinkDatabase VehicleRegionLinkDatabase { get; set; }

			public VehicleRegionAndRoomUpdater VehicleRegionAndRoomUpdater { get; set; }

			public VehicleRegionDirtyer VehicleRegionDirtyer { get; set; }

			public void FinalizeInit()
			{
				//TODO - cache map components in relevant classes for reachability / pathfinding
			}
		}
	}
}
