using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class AerialVehicleArrivalAction_LoadMap : AerialVehicleArrivalAction
	{
		protected int tile;
		public LaunchProtocol launchProtocol;
		public AerialVehicleArrivalModeDef arrivalModeDef;

		public AerialVehicleArrivalAction_LoadMap()
		{
		}

		public AerialVehicleArrivalAction_LoadMap(VehiclePawn vehicle, LaunchProtocol launchProtocol, int tile, AerialVehicleArrivalModeDef arrivalModeDef) : base(vehicle)
		{
			this.tile = tile;
			this.launchProtocol = launchProtocol;
			this.arrivalModeDef = arrivalModeDef;
		}

		public override bool DestroyOnArrival => true;

		public override bool Arrived(int tile)
		{
			LongEventHandler.QueueLongEvent(delegate ()
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
				string label = map.Parent.Label;
				MapLoaded(map);
				ExecuteEvents();
				arrivalModeDef.Worker.VehicleArrived(vehicle, launchProtocol, map);
			}, "GeneratingMap", false, null, true);
			return true;
		}

		protected virtual void MapLoaded(Map map)
		{
		}

		protected virtual void ExecuteEvents()
		{
			vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding].ExecuteEvents();
		}

		public static FloatMenuAcceptanceReport CanLand(VehiclePawn vehicle, MapParent mapParent)
		{
			if (mapParent is null || !mapParent.Spawned)
			{
				return false;
			}
			if (!WorldVehiclePathGrid.Instance.Passable(mapParent.Tile, vehicle.VehicleDef))
			{
				return false;
			}
			if (mapParent.EnterCooldownBlocksEntering())
			{
				return FloatMenuAcceptanceReport.WithFailReasonAndMessage("EnterCooldownBlocksEntering".Translate(), "MessageEnterCooldownBlocksEntering".Translate(mapParent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true)));
			}
			return true;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(VehiclePawn vehicle, LaunchProtocol launchProtocol, MapParent mapParent)
		{
			if (vehicle.CompVehicleLauncher.ControlInFlight)
			{
				foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanLand(vehicle, mapParent), () => new AerialVehicleArrivalAction_LoadMap(vehicle, launchProtocol, mapParent.Tile, AerialVehicleArrivalModeDefOf.TargetedLanding),
				"VF_LandVehicleTargetedLanding".Translate(mapParent.Label), vehicle, mapParent.Tile, null))
				{
					yield return floatMenuOption2;
				}
			}
			foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanLand(vehicle, mapParent), () => new AerialVehicleArrivalAction_LoadMap(vehicle, launchProtocol, mapParent.Tile, AerialVehicleArrivalModeDefOf.EdgeDrop),
					"VF_LandVehicleEdge".Translate(mapParent.Label), vehicle, mapParent.Tile, null))
			{
				yield return floatMenuOption2;
			}
			foreach (FloatMenuOption floatMenuOption2 in VehicleArrivalActionUtility.GetFloatMenuOptions(() => CanLand(vehicle, mapParent), () => new AerialVehicleArrivalAction_LoadMap(vehicle, launchProtocol, mapParent.Tile, AerialVehicleArrivalModeDefOf.CenterDrop),
				"VF_LandVehicleCenter".Translate(mapParent.Label), vehicle, mapParent.Tile, null))
			{
				yield return floatMenuOption2;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref tile, nameof(tile));
			Scribe_Defs.Look(ref arrivalModeDef, "arrivalModeDef");
			Scribe_Deep.Look(ref launchProtocol, nameof(launchProtocol));
		}
	}
}
