//#define LAZY_REGIONS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using Verse;
using static SmashTools.Debug;

namespace Vehicles
{
	public class DeferredRegionGenerator
	{
		private readonly VehicleMapping mapping;

		private readonly HashSet<VehicleDef> activelyUsedVehicles = new HashSet<VehicleDef>();

		public DeferredRegionGenerator(VehicleMapping mapping)
		{
			this.mapping = mapping;
		}

#if LAZY_REGIONS
		[Conditional("FALSE")]
#endif
		internal void GenerateAllRegions()
		{
			foreach (VehicleDef ownerDef in VehicleHarmony.gridOwners.Owners)
			{
				RequestRegionSet(ownerDef, false);
			}
		}

		[Conditional("LAZY_REGIONS")]
		public void GenerateRegionsFor(VehicleDef vehicleDef, bool urgently, Action postGenerationAction = null)
		{
			RequestRegionSet(vehicleDef, urgently, postGenerationAction);
		}

		[Conditional("LAZY_REGIONS")]
		public void DoPass()
		{
			activelyUsedVehicles.Clear();
			if (!Find.Maps.NullOrEmpty())
			{
				foreach (Map map in Find.Maps)
				{
					foreach (Pawn pawn in map.mapPawns.AllPawns)
					{
						if (pawn is VehiclePawn vehicle)
						{
							VehicleDef ownerDef = VehicleHarmony.gridOwners.GetOwner(vehicle.VehicleDef);
							activelyUsedVehicles.Add(ownerDef);
						}
					}
				}
			}
			foreach (Pawn pawn in Find.World.worldPawns.AllPawnsAlive)
			{
				if (pawn is VehiclePawn vehicle)
				{
					VehicleDef ownerDef = VehicleHarmony.gridOwners.GetOwner(vehicle.VehicleDef);
					activelyUsedVehicles.Add(ownerDef);
				}
			}
			foreach (VehicleDef ownerDef in VehicleHarmony.gridOwners.Owners)
			{
				if (!activelyUsedVehicles.Contains(ownerDef))
				{
					ReleaseRegionSet(ownerDef);
				}
			}
		}

		private void RequestRegionSet(VehicleDef vehicleDef, bool urgent, Action postGenerationAction = null)
		{
			VehicleDef ownerDef = VehicleHarmony.gridOwners.GetOwner(vehicleDef);
			VehicleMapping.VehiclePathData pathData = mapping[ownerDef];
			// Region grid has already been initialized
			if (!pathData.Suspended)
			{
				postGenerationAction?.Invoke();
				return;
			}

#if DEBUG
			Messages.Message($"Building regions for {ownerDef}", MessageTypeDefOf.SilentInput, historical: false);
#endif
			// If multithreading is disabled, all rebuild requests are urgent
			if (!urgent && VehicleMod.settings.debug.debugUseMultithreading)
			{
				var longOperation = AsyncPool<AsyncLongOperationAction>.Get();
				longOperation.Set(GenerateRegions);
				mapping.dedicatedThread.Queue(longOperation);
			}
			else
			{
				GenerateRegions();
			}

			void GenerateRegions()
			{
				if (!pathData.Suspended) return;

				pathData.VehicleRegionAndRoomUpdater.Init();
				pathData.VehicleRegionAndRoomUpdater.RebuildAllVehicleRegions();
				// post-generation event should be invoked on the main thread
				if (postGenerationAction != null)
				{
					CoroutineManager.QueueInvoke(postGenerationAction);
				}
#if DEBUG
				Messages.Message($"Completed Region Rebuild", MessageTypeDefOf.SilentInput, historical: false);
#endif
			}
		}

		private void ReleaseRegionSet(VehicleDef ownerDef)
		{
			VehicleMapping.VehiclePathData pathData = mapping[ownerDef];
			if (!pathData.Suspended)
			{
				pathData.VehicleRegionAndRoomUpdater.Release();

#if DEBUG
				Messages.Message($"Released Regions for {ownerDef}", MessageTypeDefOf.SilentInput, historical: false);
#endif
			}
		}
	}
}
