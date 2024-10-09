using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Debugging;
using SmashTools.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
		internal DeferredRegionGenerator deferredRegionGenerator;

		private int deferredRegionsCalculatedDayOfYear;
		
		public VehicleMapping(Map map) : base(map)
		{
		}

		public bool ComponentsInitialized { get; private set; } = false;

		private int DayOfYearAt0Long => GenDate.DayOfYear(GenTicks.TicksAbs, 0f);

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
			if (UnitTestManager.RunningUnitTests)
			{
				// Don't automatically initialize thread while running unit tests. DedicatedThread will be 
				// part of the testing and the state must remain consistent while transitioning between scenes.
				// Regions and path grids will also need to remain synchronous during testing.
				Debug.Message($"Skipping DedicatedThread. Running UnitTests.");
				return;
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
			deferredRegionGenerator = new DeferredRegionGenerator(this);
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

			GeneratePathGrids();

			if (!ThreadAlive)
			{
				// Init dedicated thread after map generation to avoid duplicate pathgrid and region recalcs
				InitThread(map);
			}
			if (deferredRegionGenerator != null)
			{
				deferredRegionGenerator.GenerateAllRegions();
			}
			else
			{
				GenerateRegionsAsync();
			}

			Ext_Map.RevertLongEventText();
		}

		private void GeneratePathGrids()
		{
			if (vehicleData.NullOrEmpty())
			{
				return;
			}
			for (int i = 0; i < vehicleData.Length; i++)
			{
				VehiclePathData vehiclePathData = vehicleData[i];
				LongEventHandler.SetCurrentEventText($"{"VF_GeneratingPathGrids".Translate()} {i}/{vehicleData.Length}");
				//Needs to check validity, non-pathing vehicles are still indexed since sequential vehicles will have higher index numbers
				if (vehiclePathData.IsValid)
				{
					vehiclePathData.VehiclePathGrid.RecalculateAllPerceivedPathCosts();
				}
			}
		}

		private void GenerateRegions()
		{
			if (VehicleHarmony.gridOwners.Owners.NullOrEmpty())
			{
				return;
			}

			for (int i = 0; i < VehicleHarmony.gridOwners.Owners.Count; i++)
			{
				VehicleDef vehicleDef = VehicleHarmony.gridOwners.Owners[i];
				LongEventHandler.SetCurrentEventText($"{"VF_GeneratingPathGrids".Translate()} {i}/{VehicleHarmony.gridOwners.Owners.Count}");

				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionAndRoomUpdater.Init();
				vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
			}
		}

		private void GenerateRegionsAsync()
		{
			if (VehicleHarmony.gridOwners.Owners.NullOrEmpty())
			{
				return;
			}
			if (VehicleHarmony.gridOwners.Owners.Count < 3)
			{
				GenerateRegions();
				return;
			}
			DeepProfiler.Start("Vehicle Regions");
			Parallel.ForEach(VehicleHarmony.gridOwners.Owners, delegate (VehicleDef vehicleDef)
			{
				LongEventHandler.SetCurrentEventText("VF_GeneratingRegions".Translate());
				VehiclePathData vehiclePathData = this[vehicleDef];
				vehiclePathData.VehicleRegionAndRoomUpdater.Init();
				vehiclePathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
			});
			DeepProfiler.End();
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

		public void VehicleSpawned(VehiclePawn vehicle)
		{
			if (deferredRegionGenerator != null)
			{
				deferredRegionGenerator?.GenerateRegionsFor(vehicle.VehicleDef, 
					vehicle.Faction != null && vehicle.Faction.HostileTo(Faction.OfPlayer));
			}
		}

		public override void MapRemoved()
		{
			ReleaseThread();
		}

		internal bool ReleaseThread()
		{
			if (dedicatedThread == null) return false;

			Debug.Message($"<color=orange>Releasing thread {Thread.id}.</color>");
			bool released = dedicatedThread.Release();
			dedicatedThread.update -= UpdateRegions;
			dedicatedThread = null;
			return released;
		}

		public override void MapComponentTick()
		{
			base.MapComponentTick();
			if (ThreadAlive && deferredRegionGenerator != null)
			{
				int dayOfYear = DayOfYearAt0Long;
				if (deferredRegionsCalculatedDayOfYear != dayOfYear)
				{
					deferredRegionGenerator.DoPass();
					deferredRegionsCalculatedDayOfYear = dayOfYear;
				}
			}
		}

		public override void MapComponentUpdate()
		{
			if (!ThreadAlive)
			{
				UpdateRegions();
			}
#if !RELEASE
			FlashGridType flashGridType = VehicleMod.settings.debug.debugDrawFlashGrid;
			if (flashGridType != FlashGridType.None)
			{
				if (Find.CurrentMap != null && !WorldRendererUtility.WorldRenderedNow)
				{
					switch (flashGridType)
					{
						case FlashGridType.CoverGrid:
							FlashCoverGrid();
							break;
						case FlashGridType.GasGrid:
							FlashGasGrid();
							break;
						case FlashGridType.PositionManager:
							FlashClaimants();
							break;
						case FlashGridType.ThingGrid:
							FlashThingGrid();
							break;
						default:
							Log.ErrorOnce($"Not Implemented: {flashGridType}", flashGridType.GetHashCode());
							break;
					}
				}
			}
#endif
		}

		private void FlashCoverGrid()
		{
			if (!Find.TickManager.Paused)
			{
				foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
				{
					float cover = CoverUtility.TotalSurroundingCoverScore(cell, map);
					map.debugDrawer.FlashCell(cell, cover / 8, cover.ToString("F2"), duration: 1);
				}
			}
		}

		private void FlashGasGrid()
		{
			if (!Find.TickManager.Paused)
			{
				foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
				{
					if (!map.gasGrid.GasCanMoveTo(cell)) continue;

					float gas = map.gasGrid.DensityPercentAt(cell, GasType.BlindSmoke);
					map.debugDrawer.FlashCell(cell, gas / 8, gas.ToString("F2"), duration: 1);
				}
			}
		}

		private void FlashClaimants()
		{
			if (!Find.TickManager.Paused)
			{
				var manager = map.GetCachedMapComponent<VehiclePositionManager>();
				foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
				{
					if (!manager.PositionClaimed(cell)) continue;

					map.debugDrawer.FlashCell(cell, 1, duration: 1);
				}
			}
		}

		private void FlashThingGrid()
		{
			if (!Find.TickManager.Paused)
			{
				foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
				{
					Thing thing = map.thingGrid.ThingAt(cell, ThingCategory.Pawn);
					if (thing is not VehiclePawn) continue;

					map.debugDrawer.FlashCell(cell, 1, duration: 1);
				}
			}
		}

		private void UpdateRegions()
		{
			if (VehicleHarmony.gridOwners.Owners.Count > 0 && vehicleRegionGridIndexChecking < VehicleHarmony.gridOwners.Owners.Count)
			{
				VehicleDef vehicleDef = VehicleHarmony.gridOwners.Owners[vehicleRegionGridIndexChecking];
				VehiclePathData pathData = this[vehicleDef];
				if (!pathData.Suspended)
				{
					pathData.VehicleRegionGrid.UpdateClean();
					pathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
				}
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

			// Default true, suspended indicates region grid is currently disabled.
			public bool Suspended { get; internal set; } = true;

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
