using SmashTools;
using SmashTools.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

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

		private VehicleDef buildingFor;
		private int vehicleRegionGridIndexChecking = 0;

		internal DedicatedThread dedicatedThread;

		public VehicleMapping(Map map) : base(map)
		{
		}

		public bool ComponentsInitialized { get; private set; } = false;

		public bool RegionsInitialized { get; private set; } = false;

		/// <summary>
		/// VehicleDefs which have created and 'own' a set of regions
		/// </summary>
		[Obsolete("Use VehicleHarmony.gridOwners instead")]
		public List<VehicleDef> Owners => VehicleHarmony.gridOwners.Owners;

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
				if (!ComponentsInitialized)
				{
					ConstructComponents();
				}
				return vehicleData[vehicleDef.DefIndex];
			}
		}

		internal void InitThread(Map map)
		{
			if (dedicatedThread != null)
			{
				ReleaseThread();
			}
			if (!VehicleMod.settings.debug.debugUseMultithreading)
			{
				Log.Warning($"Loading map without DedicatedThread. This will cause performance issues. Map={map}.");
				return;
			}
			if (map.info?.parent == null)
			{
				return; //MapParent won't have reference resolved when loading from save, GetDedicatedThread will be called a 2nd time on PostLoadInit
			}
			DedicatedThread thread = GetDedicatedThread(map);
			thread.update += UpdateRegions;
			dedicatedThread = thread;
		}

		private static DedicatedThread GetDedicatedThread(Map map)
		{
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
		[Obsolete("Use VehicleHarmony.gridOwners instead")]
		public bool IsOwner(VehicleDef vehicleDef)
		{
			return VehicleHarmony.gridOwners.IsOwner(vehicleDef);
		}

		[Obsolete("Use VehicleHarmony.gridOwners instead")]
		public VehicleDef GetOwner(VehicleDef vehicleDef)
		{
			return VehicleHarmony.gridOwners.GetOwner(vehicleDef);
		}

		public List<VehicleDef> GetPiggies(VehicleDef ownerDef)
		{
			List<VehicleDef> owners = new List<VehicleDef>();
			if (!VehicleHarmony.gridOwners.IsOwner(ownerDef))
			{
				return owners;
			}
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
			{
				if (!VehicleHarmony.gridOwners.IsOwner(vehicleDef))
				{
					VehicleDef matchingOwnerDef = VehicleHarmony.gridOwners.GetOwner(vehicleDef);
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

			if (!ComponentsInitialized)
			{
				ConstructComponents();
			}
			RegenerateGrids();
		}

		public void RegenerateGrids()
		{
			Ext_Map.StashLongEventText();

			StringBuilder eventTextBuilder = new StringBuilder();
			GeneratePathGrids(eventTextBuilder);
			GenerateRegionsAsync(eventTextBuilder);
			if (!ThreadAlive)
			{
				InitThread(map); //Init dedicated thread after map generation to avoid duplicate pathgrid and region recalcs
			}

			Ext_Map.RevertLongEventText();
		}

		private void GeneratePathGrids(StringBuilder eventTextBuilder)
		{
			if (vehicleData.NullOrEmpty())
			{
				return;
			}
			foreach (VehiclePathData vehiclePathData in vehicleData)
			{
				//Needs to check validity, non-pathing vehicles are still indexed since sequential vehicles will have higher index numbers
				if (vehiclePathData.IsValid)
				{
					eventTextBuilder.Clear();
					eventTextBuilder.AppendLine("VF_GeneratingPathGrids".Translate());
					eventTextBuilder.AppendLine(vehiclePathData.Owner.defName);
					LongEventHandler.SetCurrentEventText(eventTextBuilder.ToString());

					vehiclePathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
				}
			}
		}

		private void GenerateRegions(StringBuilder eventTextBuilder)
		{
			if (VehicleHarmony.gridOwners.Owners.NullOrEmpty())
			{
				return;
			}

			foreach (VehicleDef vehicleDef in VehicleHarmony.gridOwners.Owners)
			{
				eventTextBuilder.Clear();
				eventTextBuilder.AppendLine("VF_GeneratingRegions".Translate());
				eventTextBuilder.AppendLine(vehicleDef.defName);
				LongEventHandler.SetCurrentEventText(eventTextBuilder.ToString());

				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionAndRoomUpdater.Enabled = true;
				vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
			}

			RegionsInitialized = true;
		}

		private void GenerateRegionsAsync(StringBuilder eventTextBuilder)
		{
			if (VehicleHarmony.gridOwners.Owners.NullOrEmpty())
			{
				return;
			}
			if (VehicleHarmony.gridOwners.Owners.Count < 3)
			{
				GenerateRegions(eventTextBuilder);
				return;
			}

			LongEventHandler.SetCurrentEventText("VF_GeneratingRegions".Translate());

			DeepProfiler.Start("Vehicle Regions");
			Parallel.ForEach(VehicleHarmony.gridOwners.Owners, delegate (VehicleDef vehicleDef)
			{
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionAndRoomUpdater.Enabled = true;
				vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
			});
			DeepProfiler.End();

			RegionsInitialized = true;
		}

		/// <summary>
		/// Construct and cache <see cref="VehiclePathData"/> for each moveable <see cref="VehicleDef"/> 
		/// </summary>
		public void ConstructComponents()
		{
			int size = DefDatabase<VehicleDef>.DefCount;
			vehicleData = new VehiclePathData[size];

			ComponentsInitialized = true;

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
					InitThread(map);
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
			bool released = dedicatedThread.Release();
			dedicatedThread.update -= UpdateRegions;
			dedicatedThread = null;
			return released;
		}

		public override void MapComponentUpdate()
		{
			if (!ThreadAlive)
			{
				UpdateRegions();
			}
		}

		private void UpdateRegions()
		{
			if (VehicleHarmony.gridOwners.Owners.Count > 0 && vehicleRegionGridIndexChecking < VehicleHarmony.gridOwners.Owners.Count)
			{
				VehicleDef vehicleDef = VehicleHarmony.gridOwners.Owners[vehicleRegionGridIndexChecking];
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionGrid.UpdateClean();
				vehiclePathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
				vehicleRegionGridIndexChecking++;
				if (vehicleRegionGridIndexChecking >= VehicleHarmony.gridOwners.Owners.Count)
				{
					vehicleRegionGridIndexChecking = 0;
				}
			}
		}

		private void GenerateAllPathData()
		{
			Ext_Map.StashLongEventText();
			LongEventHandler.SetCurrentEventText("VF_GeneratingPathData".Translate());
			foreach (VehicleDef vehicleDef in VehicleHarmony.AllVehicleOwners)
			{
				GeneratePathData(vehicleDef);
			}
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading) //Even shuttles need path data for landing
			{
				GeneratePathData(vehicleDef);
			}
			Ext_Map.RevertLongEventText();
		}

		/// <summary>
		/// Generate new <see cref="VehiclePathData"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		private VehiclePathData GeneratePathData(VehicleDef vehicleDef)
		{
			VehiclePathData vehiclePathData = new VehiclePathData(vehicleDef);
			vehicleData[vehicleDef.DefIndex] = vehiclePathData;
			bool isOwner = VehicleHarmony.gridOwners.IsOwner(vehicleDef);

			buildingFor = vehicleDef;
			{
				vehiclePathData.VehiclePathGrid = new VehiclePathGrid(this, vehicleDef);
				vehiclePathData.VehiclePathFinder = new VehiclePathFinder(this, vehicleDef);

				if (isOwner)
				{
					vehiclePathData.ReachabilityData = new VehicleReachabilitySettings(this, vehicleDef, vehiclePathData);
				}
				else
				{
					VehicleDef ownerDef = VehicleHarmony.gridOwners.GetOwner(vehicleDef); //Will return itself if it's an owner
					vehiclePathData.ReachabilityData = vehicleData[ownerDef.DefIndex].ReachabilityData;
				}
			}
			buildingFor = null;

			vehiclePathData.VehiclePathGrid.PostInit();
			vehiclePathData.VehiclePathFinder.PostInit();
			if (isOwner)
			{
				vehiclePathData.ReachabilityData.PostInit();
			}

			return vehiclePathData;
		}

		/// <summary>
		/// Container for all path related subcomponents specific to a <see cref="VehicleDef"/>.
		/// </summary>
		/// <remarks>Stores data strictly for deviations from vanilla regarding impassable values</remarks>
		public class VehiclePathData
		{
			public VehiclePathData(VehicleDef vehicleDef)
			{
				Owner = vehicleDef;

				VehiclePathGrid = null;
				VehiclePathFinder = null;
				ReachabilityData = null;
			}

			public bool IsValid => Owner != null;

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
