//#define LAZY_REGIONS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using Verse;

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

		public void GenerateRegionsFor(VehicleDef vehicleDef, bool urgently, Action postGenerationAction = null)
		{
			if (RequestRegionSet(vehicleDef, urgently, postGenerationAction) == Urgency.Urgent)
			{
				Debug.Message($"Skipped deferred generation for {vehicleDef}");
			}
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

		private Urgency RequestRegionSet(VehicleDef vehicleDef, bool urgent, Action postGenerationAction = null)
		{
			VehicleDef ownerDef = VehicleHarmony.gridOwners.GetOwner(vehicleDef);
			VehicleMapping.VehiclePathData pathData = mapping[ownerDef];
			// Region grid has already been initialized
			if (!pathData.Suspended)
			{
				postGenerationAction?.Invoke();
				return Urgency.None;
			}

			// If multithreading is disabled, all rebuild requests are urgent
			if (!urgent && VehicleMod.settings.debug.debugUseMultithreading)
			{
				var longOperation = AsyncPool<AsyncLongOperationAction>.Get();
				longOperation.Set(GenerateRegions, () => !mapping.map.Disposed);
				mapping.dedicatedThread.Queue(longOperation);
				return Urgency.Deferred;
			}
			GenerateRegions();
			return Urgency.Urgent;

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

		public enum Urgency
		{
			None,
			Urgent,
			Deferred,
		}
	}
}
