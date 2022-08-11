using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class Designator_DisassembleVehicle : Designator
	{
		public override int DraggableDimensions => 2;

		protected override DesignationDef Designation => DesignationDefOf_Vehicles.DisassembleVehicle;

		public Designator_DisassembleVehicle()
		{
			defaultLabel = "VF_DesignatorDisassemble".Translate();
			defaultDesc = "VF_DesignatorDisassembleDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct", true);
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
			hotKey = KeyBindingDefOf.Designator_Deconstruct;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			if (!c.InBounds(Map))
			{
				return false;
			}
			if (!DebugSettings.godMode && c.Fogged(Map))
			{
				return false;
			}
			if (TopDeconstructibleInCell(c, out AcceptanceReport result) == null)
			{
				return result;
			}
			return true;
		}

		public override void DesignateSingleCell(IntVec3 loc)
		{
			DesignateThing(TopDeconstructibleInCell(loc, out _));
		}

		private Thing TopDeconstructibleInCell(IntVec3 loc, out AcceptanceReport reportToDisplay)
		{
			reportToDisplay = AcceptanceReport.WasRejected;
			foreach (Thing thing in Map.thingGrid.ThingsAt(loc).OrderByDescending(thing => thing.def.altitudeLayer))
			{
				AcceptanceReport acceptanceReport = CanDesignateThing(thing);
				if (acceptanceReport.Accepted)
				{
					reportToDisplay = AcceptanceReport.WasAccepted;
					return thing;
				}
				if (!acceptanceReport.Reason.NullOrEmpty())
				{
					reportToDisplay = acceptanceReport;
				}
			}
			return null;
		}

		public override void DesignateThing(Thing t)
		{
			if (DebugSettings.godMode && t is VehiclePawn vehicle)
			{
				vehicle.DisembarkAll();
				t.Destroy(DestroyMode.Deconstruct);
				return;
			}
			Map.designationManager.AddDesignation(new Designation(t, Designation));
		}

		public override AcceptanceReport CanDesignateThing(Thing t)
		{
			if (!(t is VehiclePawn vehicle))
			{
				return false;
			}
			if (!vehicle.DeconstructibleBy(Faction.OfPlayer))
			{
				if (vehicle.Faction != null && vehicle.Faction != Faction.OfMechanoids)
				{
					return new AcceptanceReport("MessageMustDesignateDeconstructibleMechCluster".Translate());
				}
				return false;
			}
			if (Map.designationManager.DesignationOn(t, Designation) != null)
			{
				return false;
			}
			return true;
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}
	}
}
