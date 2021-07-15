using Verse;
using System.Collections.Generic;

namespace Vehicles.AI
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
		public IEnumerable<VehiclePathData> AllPathData
		{
			get
			{
				foreach (VehiclePathData pathData in vehicleData.Values)
				{
					yield return pathData;
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
				Log.Error($"StackTrace: {UnityEngine.StackTraceUtility.ExtractStackTrace()}");
				return GeneratePathData(vehicleDef);
			}
		}

		/// <summary>
		/// Finalize initialization for map component
		/// </summary>
		public override void FinalizeInit()
		{
			//ConstructComponents();
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
		/// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		private VehiclePathData GeneratePathData(VehicleDef vehicleDef)
		{
			VehiclePathData data = new VehiclePathData()
			{
				VehiclePathGrid = new VehiclePathGrid(map, vehicleDef),
				VehiclePathFinder = new VehiclePathFinder(map, vehicleDef),
				VehicleReachability = new VehicleReachability(map, vehicleDef),
				VehicleRegionGrid = new VehicleRegionGrid(map, vehicleDef),
				VehicleRegionMaker = new VehicleRegionMaker(map, vehicleDef),
				VehicleRegionAndRoomUpdater = new VehicleRegionAndRoomUpdater(map, vehicleDef),
				VehicleRegionLinkDatabase = new VehicleRegionLinkDatabase(),
				VehicleRegionDirtyer = new VehicleRegionDirtyer(map, vehicleDef)
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
		}
	}
}
