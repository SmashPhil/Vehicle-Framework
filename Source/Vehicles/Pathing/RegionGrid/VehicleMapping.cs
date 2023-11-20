using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	/// <summary>
	/// MapComponent container for all pathing related sub-components for vehicles
	/// </summary>
	public sealed class VehicleMapping : MapComponent
	{
		private const int EventMapId = 0;
		private const int TempIncidentMapId = 1;

		private VehiclePathData[] vehicleData;
		private int[] piggyToOwner;
		private List<VehicleDef> owners = new List<VehicleDef>();

		private int buildingFor = -1;
		private int vehicleRegionGridIndexChecking = 0;

		internal DedicatedThread dedicatedThread;

		public VehicleMapping(Map map) : base(map)
		{
			ConstructComponents();
			dedicatedThread = GetDedicatedThread(map);
		}

		/// <summary>
		/// VehicleDefs which have created and 'own' a set of regions
		/// </summary>
		public List<VehicleDef> Owners => owners;

		public DedicatedThread Thread => dedicatedThread;

		/// <summary>
		/// Check to make sure dedicated thread is instantiated and running.
		/// </summary>
		/// <remarks>Verify this is true before queueing up a method, otherwise you may just be sending it to the void where it will never be executed ever.</remarks>
		public bool ThreadAvailable => dedicatedThread != null && dedicatedThread.thread.IsAlive;

		/// <summary>
		/// If dedicated thread is given long task, it should be marked as having a long operation so smaller tasks can avoid queueing up with a long wait time
		/// </summary>
		public bool ThreadBusy => !dedicatedThread.InLongOperation;

		/// <summary>
		/// Retrieve all <see cref="VehiclePathData"/> for this map
		/// </summary>
		public IEnumerable<VehiclePathData> AllPathData
		{
			get
			{
				foreach (VehiclePathData pathData in vehicleData)
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
#if DEBUG
				if (buildingFor == vehicleDef.DefIndex)
				{
					Log.Error($"Trying to pull VehiclePathData by indexing when it's currently in the middle of generation. Recursion is not supported here!");
					return VehiclePathData.Invalid;
				}
#endif
				VehiclePathData pathData = vehicleData[vehicleDef.DefIndex];
				if (!pathData.IsValid)
				{
					Log.Error($"Unable to retrieve path data on {map} for {vehicleDef.defName}. Was this VehicleDef created postload? Self-correcting...");
					Log.Error($"StackTrace: {StackTraceUtility.ExtractStackTrace()}");
					return GeneratePathData(vehicleDef);
				}
				return pathData;
			}
		}

		internal static DedicatedThread GetDedicatedThread(Map map)
		{
			if (!VehicleMod.settings.debug.debugUseMultithreading)
			{
				Log.Warning($"Loading map without DedicatedThread. This will cause performance issues. Map={map}.");
				return null;
			}
			if (map.info?.parent == null)
			{
				return null; //MapParent won't have reference resolved when loading from save, GetDedicatedThread will be called a 2nd time on PostLoadInit
			}
			DedicatedThread thread;
			if (map.IsPlayerHome)
			{
				thread = ThreadManager.CreateNew();
				Debug.Message($"<color=orange>{VehicleHarmony.LogLabel} Creating thread (id={thread?.id})</color>");
				return thread;
			}
			if (map.IsTempIncidentMap)
			{
				thread = ThreadManager.GetShared(TempIncidentMapId);
				Debug.Message($"<color=orange>{VehicleHarmony.LogLabel} Fetching thread from pool (id={thread?.id})</color>");
				return thread;
			}
			thread = ThreadManager.GetShared(EventMapId);
			Debug.Message($"<color=orange>{VehicleHarmony.LogLabel} Fetching thread from pool (id={thread?.id})</color>");
			return thread;
		}

		/// <summary>
		/// Check if <paramref name="vehicleDef"/> is an owner of a region set
		/// </summary>
		/// <param name="vehicleDef"></param>
		public bool IsOwner(VehicleDef vehicleDef)
		{
			return piggyToOwner[vehicleDef.DefIndex] == vehicleDef.DefIndex;
		}

		public VehicleDef GetOwner(VehicleDef vehicleDef)
		{
			int id = piggyToOwner[vehicleDef.DefIndex];
			return VehicleHarmony.AllMoveableVehicleDefs.FirstOrDefault(vehicleDef => vehicleDef.DefIndex == id);
		}

		public VehicleDef GetOwner(int ownerId)
		{
			return VehicleHarmony.AllMoveableVehicleDefs.FirstOrDefault(vehicleDef => vehicleDef.DefIndex == ownerId);
		}

		public List<VehicleDef> GetPiggies(VehicleDef ownerDef)
		{
			List<VehicleDef> owners = new List<VehicleDef>();
			if (!IsOwner(ownerDef))
			{
				return owners;
			}
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				if (!IsOwner(vehicleDef))
				{
					VehicleDef matchingOwnerDef = GetOwner(vehicleDef);
					if (matchingOwnerDef == ownerDef)
					{
						owners.Add(vehicleDef);
					}
				}
			}
			return owners;
		}

		/// <summary>
		/// Finalize initialization for map component
		/// </summary>
		public void RebuildVehiclePathData()
		{
			foreach (VehiclePathData vehiclePathData in AllPathData)
			{
				//Needs to check validity, non-pathing vehicles are still indexed since sequential vehicles will have higher index numbers
				if (vehiclePathData.IsValid)
				{
					vehiclePathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
					if (IsOwner(vehiclePathData.Owner) && vehiclePathData.UsesRegions)
					{
						vehiclePathData.VehicleRegionAndRoomUpdater.Enabled = true;
						vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
					}
				}
			}
		}

		/// <summary>
		/// Construct and cache <see cref="VehiclePathData"/> for each moveable <see cref="VehicleDef"/> 
		/// </summary>
		public void ConstructComponents()
		{
			int size = DefDatabase<VehicleDef>.DefCount;
			vehicleData = new VehiclePathData[size];
			piggyToOwner = new int[size].Populate(-1);

			owners.Clear();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading) //Even shuttles need path data for landing
			{
				GeneratePathData(vehicleDef);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (dedicatedThread == null)
				{
					dedicatedThread = GetDedicatedThread(map);
				}
			}
		}

		public override void MapRemoved()
		{
			bool result = ReleaseThread();
		}

		internal bool ReleaseThread()
		{
			Debug.Message($"<color=orange>Releasing thread {Thread.id}.</color>");
			return dedicatedThread.Release();
		}

		/// <summary>
		/// Update vehicle regions sequentially
		/// </summary>
		/// <remarks>
		/// Only one VehicleDef's RegionGrid updates per frame so performance doesn't scale with vehicle count
		/// </remarks>
		public override void MapComponentUpdate()
		{
			if (owners.Count > 0 && vehicleRegionGridIndexChecking < owners.Count)
			{
				VehicleDef vehicleDef = owners[vehicleRegionGridIndexChecking];
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionGrid.UpdateClean();
				vehiclePathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
			}
		}

		/// <summary>
		/// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		private VehiclePathData GeneratePathData(VehicleDef vehicleDef, bool compress = true)
		{
			VehiclePathData vehiclePathData = new VehiclePathData(vehicleDef);

			buildingFor = vehicleDef.DefIndex;
			{
				vehiclePathData.VehiclePathGrid = new VehiclePathGrid(this, vehicleDef);
				vehiclePathData.VehiclePathFinder = new VehiclePathFinder(this, vehicleDef);

				if (TryGetOwner(vehiclePathData, out int ownerId) && compress)
				{
					//Piggy back off vehicles with similar width + impassability
					piggyToOwner[vehicleDef.DefIndex] = ownerId;
					vehiclePathData.ReachabilityData = vehicleData[ownerId].ReachabilityData;
				}
				else
				{
					vehiclePathData.ReachabilityData = new VehicleReachabilitySettings(this, vehicleDef);
					AddOwner(vehicleDef);
				}
				vehicleData[vehicleDef.DefIndex] = vehiclePathData;
			}
			buildingFor = -1;
			return vehiclePathData;
		}

		private void AddOwner(VehicleDef vehicleDef)
		{
			piggyToOwner[vehicleDef.DefIndex] = vehicleDef.DefIndex;
			owners.Add(vehicleDef);
		}

		private bool TryGetOwner(VehiclePathData vehiclePathData, out int ownerId)
		{
			foreach (VehicleDef checkingOwner in owners)
			{
				ownerId = checkingOwner.DefIndex;
				if (vehiclePathData.MatchesReachability(vehicleData[ownerId]))
				{
					Log.Message($"VehicleDef: {vehiclePathData.Owner} with id = {vehiclePathData.Owner.DefIndex} piggying off {checkingOwner} with ownerId = {ownerId}");
					return true;
				}
			}
			ownerId = -1;
			return false;
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
			private readonly int size;
			private readonly bool defaultTerrainImpassable;

			public VehiclePathData(VehicleDef vehicleDef)
			{
				this.vehicleDef = vehicleDef;
				size = Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z);
				defaultTerrainImpassable = vehicleDef.properties.defaultTerrainImpassable;
				impassableThingDefs = vehicleDef.properties.customThingCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();
				impassableTerrain = vehicleDef.properties.customTerrainCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();

				VehiclePathGrid = null;
				VehiclePathFinder = null;
				ReachabilityData = null;
			}

			public static VehiclePathData Invalid => new VehiclePathData();

			public bool IsValid => vehicleDef != null;

			public bool UsesRegions => vehicleDef.vehicleMovementPermissions > VehiclePermissions.NotAllowed;

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
				if (!other.IsValid)
				{
					return false;
				}
				return UsesRegions == other.UsesRegions && size == other.size && defaultTerrainImpassable == other.defaultTerrainImpassable &&
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
