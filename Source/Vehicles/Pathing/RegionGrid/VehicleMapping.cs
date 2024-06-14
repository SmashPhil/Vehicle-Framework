using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Verse;
using UnityEngine;
using HarmonyLib;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	/// <summary>
	/// MapComponent container for all pathing related sub-components for vehicles
	/// </summary>
	[StaticConstructorOnStartup]
	public sealed class VehicleMapping : MapComponent
	{
		private const int EventMapId = 0;
		private const int TempIncidentMapId = 1;

		private VehiclePathData[] vehicleData;
		private int[] piggyToOwner;
		private List<VehicleDef> owners = new List<VehicleDef>();

		private VehicleDef buildingFor;
		private int vehicleRegionGridIndexChecking = 0;

		internal DedicatedThread dedicatedThread;

		private bool initialized;

		public VehicleMapping(Map map) : base(map)
		{
		}

		/// <summary>
		/// VehicleDefs which have created and 'own' a set of regions
		/// </summary>
		public List<VehicleDef> Owners => owners;

		public DedicatedThread Thread => dedicatedThread;

		/// <summary>
		/// Check if <see cref="dedicatedThread"/> is initialized and running.
		/// </summary>
		public bool ThreadAlive
		{
			get
			{
				return dedicatedThread != null && dedicatedThread.thread.IsAlive;
			}
		}

		/// <summary>
		/// Check if <see cref="dedicatedThread"/> is alive and not in long operation.
		/// </summary>
		/// <remarks>Verify this is true before queueing up a method, otherwise you may just be sending it to the void where it will never be executed ever.</remarks>
		public bool ThreadAvailable
		{
			get
			{
				return ThreadAlive && !ThreadBusy;
			}
		}

		/// <summary>
		/// DedicatedThread is either processing a long operation in its queue or the queue has grown large enough to warrant waiting.
		/// </summary>
		public bool ThreadBusy
		{
			get
			{
				return dedicatedThread.InLongOperation || dedicatedThread.QueueCount > 10000;
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
				if (buildingFor == vehicleDef)
				{
					Log.Error($"Trying to pull VehiclePathData by indexing when it's currently in the middle of generation. Recursion is not supported here.");
					return null;
				}
#endif
				if (!initialized)
				{
					ConstructComponents();
				}
				VehiclePathData pathData = vehicleData[vehicleDef.DefIndex];
				if (!pathData.IsValid)
				{
					Log.Error($"Unable to retrieve path data on {map} for {vehicleDef.defName}. Was this VehicleDef created postload? Self-correcting...");
					Log.Error($"StackTrace: {StackTraceUtility.ExtractStackTrace()}");
					if (!UnityData.IsInMainThread)
					{
						Log.Error($"Unable to generate path data outside of the main thread. May encounter thread safety issues.");
						return null;
						
					}
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
		public override void FinalizeInit()
		{
			base.FinalizeInit();

			if (!initialized)
			{
				ConstructComponents();
			}

			Ext_Map.StashLongEventText();
			LongEventHandler.SetCurrentEventText("Generating Vehicle PathGrids");

			DeepProfiler.Start("VF_GeneratePathGrids".Translate());
			foreach (VehiclePathData vehiclePathData in vehicleData)
			{
				//Needs to check validity, non-pathing vehicles are still indexed since sequential vehicles will have higher index numbers
				if (vehiclePathData.IsValid)
				{
					vehiclePathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
				}
			}
			DeepProfiler.End();

			DeepProfiler.Start("VF_GenerateRegions".Translate());
			foreach (VehicleDef vehicleDef in owners)
			{
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionAndRoomUpdater.Enabled = true;
				vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
			}
			DeepProfiler.End();

			DeepProfiler.Start("Fetching DedicatedThread");
			dedicatedThread = GetDedicatedThread(map); //Init dedicated thread after map generation to avoid duplicate pathgrid and region recalcs
			DeepProfiler.End();

			Ext_Map.RevertLongEventText();
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

			initialized = true;

			GenerateAllPathData();

			PathingHelper.DisableAllRegionUpdaters(map);
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
			ReleaseThread();
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
				vehicleRegionGridIndexChecking++;
				if (vehicleRegionGridIndexChecking >= owners.Count)
				{
					vehicleRegionGridIndexChecking = 0;
				}
			}
		}

		private void GenerateAllPathData()
		{
			Ext_Map.StashLongEventText();
			LongEventHandler.SetCurrentEventText("VF_GeneratingPathData".Translate());
			DeepProfiler.Start("Generating VehiclePathData");
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading) //Even shuttles need path data for landing
			{
				GeneratePathData(vehicleDef);
			}
			DeepProfiler.End();

			Ext_Map.RevertLongEventText();
		}

		/// <summary>
		/// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		private VehiclePathData GeneratePathData(VehicleDef vehicleDef, bool compress = true)
		{
			VehiclePathData vehiclePathData = new VehiclePathData(vehicleDef);
			vehicleData[vehicleDef.DefIndex] = vehiclePathData;
			bool newOwner = false;

			buildingFor = vehicleDef;
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
					vehiclePathData.ReachabilityData = new VehicleReachabilitySettings(this, vehicleDef, vehiclePathData);
					AddOwner(vehicleDef);
					newOwner = true;
				}
			}
			buildingFor = null;

			vehiclePathData.VehiclePathGrid.PostInit();
			vehiclePathData.VehiclePathFinder.PostInit();
			if (newOwner)
			{
				vehiclePathData.ReachabilityData.PostInit();
			}

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
					return true;
				}
			}
			ownerId = -1;
			return false;
		}

		/// <summary>
		/// Container for all path related subcomponents specific to a <see cref="VehicleDef"/>.
		/// </summary>
		/// <remarks>Stores data strictly for deviations from vanilla regarding impassable values</remarks>
		public class VehiclePathData
		{
			private readonly HashSet<ThingDef> impassableThingDefs;
			private readonly HashSet<TerrainDef> impassableTerrain;
			private readonly int size;
			private readonly bool defaultTerrainImpassable;

			public VehiclePathData(VehicleDef vehicleDef)
			{
				Owner = vehicleDef;
				size = Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z);
				defaultTerrainImpassable = vehicleDef.properties.defaultTerrainImpassable;
				impassableThingDefs = vehicleDef.properties.customThingCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();
				impassableTerrain = vehicleDef.properties.customTerrainCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();

				VehiclePathGrid = null;
				VehiclePathFinder = null;
				ReachabilityData = null;
			}

			public bool IsValid => Owner != null;

			public bool UsesRegions => Owner.vehicleMovementPermissions > VehiclePermissions.NotAllowed;

			public VehicleDef Owner { get; }

			internal VehicleReachabilitySettings ReachabilityData { get; set; }

			public VehiclePathGrid VehiclePathGrid { get; set; }

			public VehiclePathFinder VehiclePathFinder { get; set; }

			public VehicleReachability VehicleReachability => ReachabilityData.reachability;

			public VehicleRegionGrid VehicleRegionGrid => ReachabilityData.regionGrid;

			public VehicleRegionMaker VehicleRegionMaker => ReachabilityData.regionMaker;

			public VehicleRegionLinkDatabase VehicleRegionLinkDatabase => ReachabilityData.regionLinkDatabase;

			public VehicleRegionAndRoomUpdater VehicleRegionAndRoomUpdater => ReachabilityData.regionAndRoomUpdater;

			public VehicleRegionDirtyer VehicleRegionDirtyer => ReachabilityData.regionDirtyer;

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

		public class VehicleReachabilitySettings
		{
			public readonly VehicleRegionGrid regionGrid;
			public readonly VehicleRegionMaker regionMaker;
			public readonly VehicleRegionLinkDatabase regionLinkDatabase;
			public readonly VehicleRegionAndRoomUpdater regionAndRoomUpdater;
			public readonly VehicleRegionDirtyer regionDirtyer;
			public readonly VehicleReachability reachability;

			public VehicleReachabilitySettings(VehicleMapping vehicleMapping, VehicleDef vehicleDef, VehiclePathData pathData)
			{
				regionGrid = new VehicleRegionGrid(vehicleMapping, vehicleDef);
				regionMaker = new VehicleRegionMaker(vehicleMapping, vehicleDef);
				regionLinkDatabase = new VehicleRegionLinkDatabase(vehicleMapping, vehicleDef);
				regionAndRoomUpdater = new VehicleRegionAndRoomUpdater(vehicleMapping, vehicleDef);
				regionDirtyer = new VehicleRegionDirtyer(vehicleMapping, vehicleDef);
				reachability = new VehicleReachability(vehicleMapping, vehicleDef, pathData.VehiclePathGrid, regionGrid);
			}

			public void PostInit()
			{
				regionGrid.PostInit();
				regionMaker.PostInit();
				regionLinkDatabase.PostInit();
				regionAndRoomUpdater.PostInit();
				regionDirtyer.PostInit();
				reachability.PostInit();
			}
		}
	}
}
