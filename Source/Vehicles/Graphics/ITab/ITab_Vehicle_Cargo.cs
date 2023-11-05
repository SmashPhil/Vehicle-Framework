using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class ITab_Vehicle_Cargo : ITab_Airdrop_Container
	{
		public ITab_Vehicle_Cargo()
		{
			size = new Vector2(460f, 450f);
			labelKey = "VF_TabCargo";
		}

		public override bool IsVisible => !Vehicle.beached;

		protected override string InventoryLabelKey => "VF_Cargo";

		protected override bool AllowDropping => true;

		private VehiclePawn Vehicle
		{
			get
			{
				if (SelPawn is VehiclePawn vehicle)
				{
					return vehicle;
				}
				throw new InvalidOperationException("Cargo tab on non-pawn ship " + SelThing);
			}
		}

		protected override void DrawAdditionalRows(ref float curY, Rect rect)
		{
			foreach (TransferableOneWay transferable in Vehicle.cargoToLoad)
			{
				if (transferable.AnyThing != null && transferable.CountToTransfer > 0 && !Vehicle.inventory.innerContainer.Contains(transferable.AnyThing))
				{
					DrawThingRow(ref curY, rect.width, transferable.AnyThing, transferable.CountToTransfer, false, true);
				}
			}
		}

		protected override void DrawHeader(ref float curY, float width)
		{
			Rect rect = new Rect(0f, curY, width, StandardLineHeight);
			float cannonsNum = 0f;
			if (Vehicle.TryGetComp<CompVehicleTurrets>() != null)
			{
				foreach (VehicleTurret turret in Vehicle.CompVehicleTurrets.turrets)
				{
					cannonsNum += turret.loadedAmmo is null ? 0f : turret.loadedAmmo.BaseMass * turret.shellCount;
				}
			}
			float mass = MassUtility.GearAndInventoryMass(Vehicle) + cannonsNum;
			float capacity = Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			Widgets.Label(rect, "MassCarried".Translate(mass.ToString("0.##"), capacity.ToString("0.##")));

			Rect rectDropAll = new Rect(rect.xMax - ThingDropButtonSize, curY, ThingDropButtonSize, ThingDropButtonSize);
			if (AllowDropping && Inventory.Any)
			{
				TooltipHandler.TipRegion(rectDropAll, "EjectAll".Translate());
				if (Widgets.ButtonImageFitted(rectDropAll, VehicleTex.Drop))
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					InterfaceDropAll();
				}
			}
			curY += StandardLineHeight;
		}

		protected override bool InterfaceDrop(Thing thing)
		{
			bool result = base.InterfaceDrop(thing);
			if (result)
			{
				Vehicle.EventRegistry[VehicleEventDefOf.CargoRemoved].ExecuteEvents();
			}
			return result;
		}

		protected override bool InterfaceDropAll()
		{
			bool result = base.InterfaceDropAll();
			if (result)
			{
				Vehicle.EventRegistry[VehicleEventDefOf.CargoRemoved].ExecuteEvents();
			}
			return result;
		}
	}
}
